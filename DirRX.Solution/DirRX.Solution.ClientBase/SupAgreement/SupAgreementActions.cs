using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.SupAgreement;
using System.Text;

namespace DirRX.Solution.Client
{
  partial class SupAgreementActions
  {
    public virtual void SendToIMS(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Проверка прав.
      var isEnabled = false;
      
      if (_obj.Department != null)
      {
        var department = Solution.Departments.As(_obj.Department);
        var responsible = Sungero.Company.Employees.Null;
        
        responsible = (department.ResponsibleForSAP != null) ? department.ResponsibleForSAP : _obj.ResponsibleEmployee;
        
        if (responsible != null)
          isEnabled = Equals(Employees.Current, responsible) || Solution.PublicFunctions.Module.Remote.IsSubsitute(responsible);
      }
      
      if (!isEnabled)
      {
        Dialogs.NotifyMessage(DirRX.Solution.Contracts.Resources.SendToIMSNotAccessRight);
        return;
      }
      
      // Подтверждение повторной отправки.
      if (_obj.Counterparties.Where(c => c.Counterparty != null && c.CSBExportDate.HasValue).Any())
      {
        var dialog = Dialogs.CreateTaskDialog(DirRX.Solution.Contracts.Resources.SendIMSDialogMessage, MessageType.Question);
        dialog.Buttons.AddYesNo();
        if (dialog.Show() != DialogButtons.Yes)
          return;
      }
      
      #region Постоянные значения для передачи в SAP.
      // Значение функциональности для передачи - "Закупка".
      const string functionalityPurchase = "MM";
      // Значение функциональности для передачи - "Сбыт".
      const string functionalitySell = "SD";
      // Значение баланосовой единицы для передачи.
      const string balanceUnit = "1111";
      // Значение типа документа для передачи - "Дополнительное соглашение".
      const string supAgreementType = "02";
      // Значение статуса документа для передачи - "Действующий".
      const string activeDocumentState = "02";
      
      // Email ответственного за интеграцию с SAP.
      var sapResponsible = ContractsCustom.PublicFunctions.Module.Remote.GetSAPResponsible();
      string sapResponsibleDefaultEmail = sapResponsible != null && !string.IsNullOrEmpty(sapResponsible.Email) ? sapResponsible.Email : string.Empty;
      #endregion
      
      // Получить договор.
      DirRX.Solution.IContract contract = null;
      var baseContract = _obj.LeadingDocument;
      if (baseContract != null)
        contract = DirRX.Solution.Contracts.As(baseContract);
      if (contract == null)
      {
        Dialogs.NotifyMessage(ContractsCustom.Resources.ContractsToSAPNoContractInSupAgreement);
        return;
      }
      
      var ksssContragentIdList = new List<int>();
      var errContragentIdLinks = new StringBuilder();
      foreach (var counterparty in _obj.Counterparties.Where(c => c.Counterparty != null).Select(c => c.Counterparty))
      {
        if (counterparty.KSSSContragentId.HasValue)
          ksssContragentIdList.Add(counterparty.KSSSContragentId.Value);
        else
          errContragentIdLinks.AppendLine(Hyperlinks.Get(counterparty));
      }
      
      if (errContragentIdLinks.Length > 0)
        Dialogs.NotifyMessage(ContractsCustom.Resources.ContractsToSAPWrongKSSSCounterpartyIdFormat(errContragentIdLinks.ToString()));
      
      if (!ksssContragentIdList.Any())
        return;
      
      // Вычислить функциональность договора, для смешанной, запрос формируется по полям "Закупка" и "Сбыт".
      bool isPurchaseFunctionality = contract.ContractFunctionality == DirRX.Solution.Contract.ContractFunctionality.Purchase;
      bool isSellFunctionality = contract.ContractFunctionality == DirRX.Solution.Contract.ContractFunctionality.Sale;
      bool isMixedFunctionality = contract.ContractFunctionality == DirRX.Solution.Contract.ContractFunctionality.Mixed;
      
      if (!isPurchaseFunctionality && !isSellFunctionality && !isMixedFunctionality)
      {
        Dialogs.NotifyMessage(ContractsCustom.Resources.ContractsToSAPNoContractFunctionality);
        return;
      }
      
      // Email сотрудника, инициировавшего выгрузку.
      string sapResponsibleEmail = null;
      sapResponsibleEmail = Employees.Current.Email;
      
      // Вычислить email ответственного за SAP из подразделения.
      if (string.IsNullOrEmpty(sapResponsibleEmail))
      {
        var contractDepartment = DirRX.Solution.Departments.As(_obj.Department);
        sapResponsibleEmail = (contractDepartment != null && contractDepartment.ResponsibleForSAP != null) ? contractDepartment.ResponsibleForSAP.Email : sapResponsibleDefaultEmail;
      }
      
      // Собрать коды вида договора и функциональности.
      var kindFunctionalityList = new List<DirRX.Solution.Structures.Module.ISAPContractKindFunctionality>();
      
      if (isPurchaseFunctionality || isSellFunctionality)
      {
        foreach (ContractsCustom.IIMSContractCode codeIMS in DirRX.Solution.Contracts.As(_obj.LeadingDocument).IMSCodeCollection.Select(c => c.IMSCode))
        {
          var kindFunctionality = DirRX.Solution.Structures.Module.SAPContractKindFunctionality.Create();
          kindFunctionality.ContractFunctionality = isPurchaseFunctionality ? functionalityPurchase : functionalitySell;
          kindFunctionality.ContractKind = codeIMS.Name;
          kindFunctionalityList.Add(kindFunctionality);
        }
      }
      
      if (isMixedFunctionality)
      {
        // Закупка.
        foreach (ContractsCustom.IIMSContractCode codeIMS in DirRX.Solution.Contracts.As(_obj.LeadingDocument).IMSCodePurchaseCollection.Select(c => c.IMSCode))
        {
          var kindFunctionality = DirRX.Solution.Structures.Module.SAPContractKindFunctionality.Create();
          kindFunctionality.ContractFunctionality = functionalityPurchase;
          kindFunctionality.ContractKind = codeIMS.Name;
          kindFunctionalityList.Add(kindFunctionality);
        }
        // Сбыт.
        foreach (ContractsCustom.IIMSContractCode codeIMS in DirRX.Solution.Contracts.As(_obj.LeadingDocument).IMSCodeSaleCollection.Select(c => c.IMSCode))
        {
          var kindFunctionality = DirRX.Solution.Structures.Module.SAPContractKindFunctionality.Create();
          kindFunctionality.ContractFunctionality = functionalitySell;
          kindFunctionality.ContractKind = codeIMS.Name;
          kindFunctionalityList.Add(kindFunctionality);
        }
      }
      
      // Многостороннее ДС.
      var isManyCounterparties = _obj.IsManyCounterparties.GetValueOrDefault();
      
      foreach (DirRX.Solution.Structures.Module.SAPContractKindFunctionality kf in kindFunctionalityList.Distinct().ToList())
      {
        var contractKind = kf.ContractKind;
        var contractFunctionality = kf.ContractFunctionality;
        
        foreach (var ksssContragentId in ksssContragentIdList)
        {
          string json = string.Empty;
          try
          {
            var sapContract = new DirRX.Solution.Structures.Module.SAPContract();
            sapContract.Id = _obj.Id.ToString();
            sapContract.KSSSContragentId = ksssContragentId.ToString();
            sapContract.ContractFunctionality = contractFunctionality;
            sapContract.BalanceUnit = balanceUnit;
            sapContract.ContractKind = contractKind;
            sapContract.DocumentDate = _obj.ActualDate.HasValue ? _obj.ActualDate.Value.ToString("dd.MM.yyyy") : string.Empty;
            sapContract.ValidFrom = _obj.ValidFrom.HasValue ? _obj.ValidFrom.Value.ToString("dd.MM.yyyy") : string.Empty;
            sapContract.ValidTill = ContractsCustom.PublicFunctions.Module.Remote.GetValidTillSAP(_obj.ValidTill, _obj.ValidFrom);
            sapContract.RegNumber = ContractsCustom.PublicFunctions.Module.Remote.GetRegistrationNumberSAP(_obj.RegistrationNumber);
            sapContract.RegNumberContract = ContractsCustom.PublicFunctions.Module.Remote.GetRegistrationNumberSAP(contract.RegistrationNumber);
            sapContract.DocumentType = supAgreementType;
            sapContract.TotalAmount = ContractsCustom.PublicFunctions.Module.Remote.GetAmountSAP(_obj.TransactionAmount);
            sapContract.Currency = ContractsCustom.PublicFunctions.Module.Remote.GetCurrencySAP(_obj.Currency);
            sapContract.LifeCycleState = activeDocumentState;
            sapContract.ResponsibleEmployee = ContractsCustom.PublicFunctions.Module.Remote.GetEmployeeSAP(_obj.ResponsibleEmployee);
            sapContract.OurSignatory = ContractsCustom.PublicFunctions.Module.Remote.GetEmployeeSAP(_obj.OurSignatory);
            sapContract.Supervisor = ContractsCustom.PublicFunctions.Module.Remote.GetEmployeeSAP(_obj.Supervisor);
            sapContract.Territory = ContractsCustom.PublicFunctions.Module.Remote.GetTerritorySAP(_obj.Territory);
            sapContract.EmployeeEmail = sapResponsibleEmail;
            sapContract.ContractName = _obj.Name;
            
            var countSignName = string.Empty;
            var counterpartySignatory = !isManyCounterparties ? _obj.CounterpartySignatory : _obj.Counterparties.FirstOrDefault(c => c.Counterparty.KSSSContragentId == ksssContragentId).Signatory;
            if (counterpartySignatory != null)
            {
              countSignName = counterpartySignatory.Name.Length > 250 ? counterpartySignatory.Name.Substring(0, 250) : counterpartySignatory.Name;
              if (!string.IsNullOrEmpty(counterpartySignatory.JobTitle))
                countSignName = string.Join(Environment.NewLine, countSignName, counterpartySignatory.JobTitle);
            }
            sapContract.CountSignName = countSignName;
            
            var ourSignName = string.Empty;
            var ourSignatory =  _obj.OurSignatory;
            if (ourSignatory != null)
            {
              ourSignName = ourSignatory.Name.Length > 250 ? ourSignatory.Name.Substring(0, 250) : ourSignatory.Name;
              if (ourSignatory.JobTitle != null)
                ourSignName = string.Join(Environment.NewLine, ourSignName, ourSignatory.JobTitle.Name);
            }
            sapContract.OurSignName = ourSignName;
            
            var deliveryMethod = !isManyCounterparties ? _obj.DeliveryMethod : _obj.Counterparties.FirstOrDefault(c => c.Counterparty.KSSSContragentId == ksssContragentId).DeliveryMethod;
            sapContract.DeliveryMethod = deliveryMethod != null ? deliveryMethod.Name : string.Empty;
            
            var contactName = string.Empty;
            var contact = !isManyCounterparties ? _obj.Contact : _obj.Counterparties.FirstOrDefault(c => c.Counterparty.KSSSContragentId == ksssContragentId).Contact;
            if (contact != null)
            {
              contactName = contact.Name.Length > 250 ? contact.Name.Substring(0, 250) : contact.Name;
              if (!string.IsNullOrEmpty(contact.JobTitle))
                contactName = string.Join(Environment.NewLine, contactName, contact.JobTitle);
              if (!string.IsNullOrEmpty(contact.Email))
                contactName = string.Join(Environment.NewLine, contactName, contact.Email);
              if (!string.IsNullOrEmpty(contact.Phone))
                contactName = string.Join(Environment.NewLine, contactName, contact.Phone);
            }
            sapContract.ContactName = contactName;
            
            json = DirRX.Solution.PublicFunctions.Module.SerializeObjectToJSONClient(sapContract);
            int status = DirRX.Solution.PublicFunctions.Module.Remote.SendSAPContractToCSB(json);
            if (status >= 200 && status <= 202)
            {
              _obj.Counterparties.FirstOrDefault(c => c.Counterparty.KSSSContragentId == ksssContragentId).CSBExportDate = Calendar.Now;
              // Обновляем только для визуального отображения.
              _obj.CSBExportDate = Calendar.Now;
              _obj.Save();
              
              Logger.DebugFormat("Документ с ИД {0} успешно передан в КСШ с кодом ответа: {1}", _obj.Id, status);
              Dialogs.NotifyMessage(string.Format("Документ с ИД {0} успешно передан в КСШ.", _obj.Id));
            }
            else
            {
              Dialogs.NotifyMessage(ContractsCustom.Resources.ContractsToSAPWrongResponseStatusCodeMessage);
              Logger.Debug(ContractsCustom.Resources.ContractsToSAPWrongResponseStatusCodeFormat(status, json));
            }
          }
          catch (Exception ex)
          {
            Dialogs.NotifyMessage(ContractsCustom.Resources.ContractsToSAPSendingErrorTextMessageFormat(ex.Message));
            Logger.Debug(ContractsCustom.Resources.ContractsToSAPSendingErrorTextFormat(ex.Message, json));
          }
        }
      }
    }

    public virtual bool CanSendToIMS(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.LifeCycleState == LifeCycleState.Active && !_obj.State.IsChanged;
    }

    public override void ImportInLastVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ImportInLastVersion(e);
    }

    public override bool CanImportInLastVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanImportInLastVersion(e) && (_obj.IsStandard != true || _obj.InternalApprovalState == InternalApprovalState.Signed);
    }

    public override void ImportInNewVersion(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ImportInNewVersion(e);
    }

    public override bool CanImportInNewVersion(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanImportInNewVersion(e) && (_obj.IsStandard != true || _obj.InternalApprovalState == InternalApprovalState.Signed);
    }

    public override void CreateFromFile(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromFile(e);
    }

    public override bool CanCreateFromFile(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCreateFromFile(e) && (_obj.IsStandard != true || _obj.InternalApprovalState == InternalApprovalState.Signed);
    }

    public override void CreateFromScanner(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.CreateFromScanner(e);
    }

    public override bool CanCreateFromScanner(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCreateFromScanner(e) && (_obj.IsStandard != true || _obj.InternalApprovalState == InternalApprovalState.Signed);
    }

    public virtual void SendWithResposible(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Contract.ExecuteSendDocWithResposibleAction(_obj);
    }

    public virtual bool CanSendWithResposible(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void MultipleCounterparties(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.IsManyCounterparties != true)
        Dialogs.NotifyMessage(DirRX.Solution.Contracts.Resources.FillCounterpartiesListOnTab);
      
      if (_obj.IsManyCounterparties == true && _obj.Counterparties.Count(c => c.Counterparty != null) > 1)
      {
        var dialog = Dialogs.CreateTaskDialog(DirRX.Solution.Contracts.Resources.ChangeManyCounterpartiesQuestion,
                                              DirRX.Solution.Contracts.Resources.ChangeManyCounterpartiesDescription, MessageType.Question);
        dialog.Buttons.AddYesNo();
        if (dialog.Show() == DialogButtons.Yes)
          _obj.IsManyCounterparties = !_obj.IsManyCounterparties.GetValueOrDefault();
      }
      else
        _obj.IsManyCounterparties = !_obj.IsManyCounterparties.GetValueOrDefault();
    }

    public virtual bool CanMultipleCounterparties(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.State.Properties.IsManyCounterparties.IsEnabled && DirRX.Solution.Contracts.As(_obj.LeadingDocument).TenderStep != DirRX.Solution.Contract.TenderStep.ApprovalProject;
    }

    public virtual void SetPostReturnStatus(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                            DirRX.ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalReturnedByPostGuid,
                                                                            DirRX.ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus,
                                                                            false);
      _obj.Save();
    }

    public virtual bool CanSetPostReturnStatus(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate();
    }






    public override void Register(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.Register(e);
      
      DateTime? activateDate = _obj.ValidFrom != null && _obj.ValidFrom > Calendar.Today ? _obj.ValidFrom : Calendar.Today;
      
      if (!Calendar.IsWorkingDay(activateDate.Value))
        activateDate = Calendar.PreviousWorkingDay(activateDate.Value);
      
      _obj.ActivateDate = activateDate.Value;
    }

    public override bool CanRegister(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanRegister(e);
    }


    public override void SendForApproval(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Проверить заполненность обязательных полей.
      bool propertiesEmpty = Functions.SupAgreement.CheckRequiredProperties(_obj, e);
      
      if (propertiesEmpty)
      {
        if (e.FormType == Sungero.Domain.Shared.FormType.Collection)
          Dialogs.ShowMessage(DirRX.Solution.Contracts.Resources.SendApprovalError, MessageType.Warning);
      }
      else
      {
        if (!_obj.HasVersions)
        {
          var dialog = Dialogs.CreateTaskDialog(DirRX.Solution.Contracts.Resources.NoVersionsInContract, MessageType.Question);
          var sendButton = dialog.Buttons.AddCustom(DirRX.Solution.Contracts.Resources.SendButton);
          dialog.Buttons.AddCancel();
          var result = dialog.Show();
          if (result == DialogButtons.Cancel)
            return;
        }
        
        if (_obj.TransactionAmount.HasValue)
        {
          // Автоматический расчет и заполнение поле Сумма.
          var contract = Contracts.As(_obj.LeadingDocument);
          double amount = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetSummInRUB(contract.TransactionAmount.Value, contract.Currency);
          // Если договор не рамочный, то общая сумма договора и сумм ДС действующих/исполненных + текущий допник.
          if (contract.IsFrameContract != true)
          {
            var totalAmount = Functions.SupAgreement.Remote.GetContractTotalAmount(_obj);
            amount = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetSummInRUB(totalAmount, _obj.Currency);
          }
          _obj.TotalAmount = amount;
          _obj.Save();
          base.SendForApproval(e);
        }
        else
          e.AddWarning(DirRX.Solution.SupAgreements.Resources.SendForApprovalCheckSumMessage);
      }
    }

    public override bool CanSendForApproval(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSendForApproval(e);
    }

    public virtual void DocumentReturned(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.ShowApprovalResultDialog(_obj);
    }

    public virtual bool CanDocumentReturned(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.ContractorOriginalSigning.HasValue;
    }

    public override void CreateFromTemplate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      FromTemplate(e);
    }

    public override bool CanCreateFromTemplate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return CanFromTemplate(e);
    }

    public virtual void FromTemplate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var dialog = Dialogs.CreateInputDialog(_obj.IsStandard == true ? DirRX.Solution.SupAgreements.Resources.CreateFromTemplate : DirRX.Solution.SupAgreements.Resources.CreateFromRecommendedForm);
      var title = _obj.IsStandard == true ? DirRX.Solution.SupAgreements.Resources.StandardForm : DirRX.Solution.SupAgreements.Resources.RecommendedForm;
      var standardFormSelect = dialog.AddSelect(title, true, _obj.StandartForm)
        .Where(s => s.FormType == null || _obj.IsStandard.Value && s.FormType == ContractsCustom.ContractSettings.FormType.Typical ||
               !_obj.IsStandard.Value && s.FormType == ContractsCustom.ContractSettings.FormType.Recommended)
        .Where(s => s.DocumentType.DocumentTypeGuid == DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.SupAgreement)
        .Where(s => Sungero.Docflow.DocumentKinds.Equals(s.DocumentKind, _obj.DocumentKind))
        .Where(s => s.Category == null || Sungero.Contracts.ContractCategories.Equals(s.Category, _obj.DocumentGroup))
        .Where(s => s.Subcategory == null || DirRX.ContractsCustom.ContractSubcategories.Equals(s.Subcategory, Contracts.As(_obj.LeadingDocument).Subcategory))
        .Where(s => !s.LukoilGroupCompany.HasValue ||
               Companies.As(_obj.Counterparty).IsLUKOILGroup == (s.LukoilGroupCompany.Value == DirRX.ContractsCustom.ContractSettings.LukoilGroupCompany.IsLukoil))
        .Where(s => s.Status == DirRX.ContractsCustom.ContractSettings.Status.Active);
      dialog.Buttons.AddOkCancel();
      if (dialog.Show() == DialogButtons.Cancel)
        return;
      
      if (!DirRX.ContractsCustom.ContractSettingses.Equals(_obj.StandartForm, standardFormSelect.Value))
        _obj.StandartForm = standardFormSelect.Value;
      
      var template = _obj.StandartForm.Template;
      if (template == null)
      {
        e.AddError(DirRX.Solution.SupAgreements.Resources.NoTemplate);
        return;
      }
      
      using (var body = template.LastVersion.Body.Read())
      {
        var newVersion = _obj.CreateVersionFrom(body, template.AssociatedApplication.Extension);
        
        var exEntity = (Sungero.Domain.Shared.IExtendedEntity)_obj;
        exEntity.Params[Sungero.Content.Shared.ElectronicDocumentUtils.FromTemplateIdKey] = template.Id;
        
        _obj.Save();
        _obj.Edit();
      }
    }

    public virtual bool CanFromTemplate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.DocumentKind != null &&
        !string.IsNullOrEmpty(_obj.Subject) &&
        _obj.Counterparty != null &&
        _obj.BusinessUnit != null &&
        _obj.Department != null &&
        _obj.Territory != null &&
        _obj.TransactionAmount != null &&
        (_obj.DocumentValidity != null || _obj.ValidTill != null);
    }

  }



  partial class SupAgreementCollectionActions
  {

    public virtual bool CanSubmitToSign(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _objs.Any() && _objs.All(c => (c.IsContractorSignsFirst == true &&
                                            c.InternalApprovalState == InternalApprovalState.Signed &&
                                            c.OriginalSigning == null &&
                                            c.ContractorOriginalSigning == ContractorOriginalSigning.Signed) ||
                                      (c.IsContractorSignsFirst == false &&
                                       c.InternalApprovalState == InternalApprovalState.Signed &&
                                       c.OriginalSigning == null));
    }

    public virtual void SubmitToSign(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var lockedDocs = Functions.Contract.Remote.SetStateOnSigning(_objs.ToList<Sungero.Contracts.IContractualDocument>());
      if (lockedDocs.Count == 0)
        Dialogs.NotifyMessage(DirRX.Solution.Contracts.Resources.SelectedContractsSetStateOnSigningSuccessfully);
      else
        DirRX.Solution.Functions.Module.ShowActionResultDialog(_objs.Count(), lockedDocs.ToList<Sungero.Contracts.IContractualDocument>(), DirRX.Solution.Contracts.Resources.SelectedContractsSetStateOnSigningPartially);

    }

    public virtual bool CanSigned(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _objs.Any() && _objs.All(c => (c.IsContractorSignsFirst == true &&
                                            c.InternalApprovalState == InternalApprovalState.Signed &&
                                            c.OriginalSigning == OriginalSigning.OnSigning &&
                                            c.ContractorOriginalSigning == ContractorOriginalSigning.Signed) ||
                                      (c.IsContractorSignsFirst == false &&
                                       c.InternalApprovalState == InternalApprovalState.Signed &&
                                       c.OriginalSigning == OriginalSigning.OnSigning));
    }

    public virtual void Signed(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // В карточке документа будет зафиксирован результат подписания оригиналов.
      var notUpdatedDocs = DirRX.Solution.Functions.Module.Remote.ChangeDocSigningOriginalState(_objs.ToList<Sungero.Contracts.IContractualDocument>(), true);
      if (notUpdatedDocs.Count == 0)
        Dialogs.NotifyMessage(DirRX.Solution.Contracts.Resources.SelectedContractsSetStateSignedSuccessfully);
      else
        DirRX.Solution.Functions.Module.ShowActionResultDialog(_objs.Count(), notUpdatedDocs, DirRX.Solution.Contracts.Resources.SelectedContractsSetStateSignedPartially);

    }

    public virtual bool CanNotSigned(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return CanSigned(e);
    }

    public virtual void NotSigned(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // В карточке документа будет зафиксирован результат подписания оригиналов.
      var notUpdatedDocs = DirRX.Solution.Functions.Module.Remote.ChangeDocSigningOriginalState(_objs.ToList<Sungero.Contracts.IContractualDocument>(), false);
      if (notUpdatedDocs.Count == 0)
        Dialogs.NotifyMessage(DirRX.Solution.Contracts.Resources.SelectedContractsSetStateNotSignedSuccessfully);
      else
        DirRX.Solution.Functions.Module.ShowActionResultDialog(_objs.Count(), notUpdatedDocs, DirRX.Solution.Contracts.Resources.SelectedContractsSetStateNotSignedPartially);

    }

    public virtual bool CanFormPackages(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _objs.Any() && (_objs.All(d => d.IsManyCounterparties != true) || _objs.Count() == 1);
    }

    public virtual void FormPackages(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.CreateDocPackages(_objs.ToList<Sungero.Contracts.IContractualDocument>());
    }

    public virtual bool CanOriginalSigned(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _objs.Any() && _objs.All(c => !c.ContractorOriginalSigning.HasValue) &&
        (_objs.All(c => c.IsManyCounterparties != true) || _objs.Count() == 1);
    }

    public virtual void OriginalSigned(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Contract.ExecuteOriginalSignedAction(_objs.ToList<Sungero.Contracts.IContractualDocument>());
    }

    public virtual bool CanSubmitToSignCopy(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _objs.Any() && _objs.All(c => !c.ScanMoveStatuses.Any(s => s.Status.Sid == ContractsCustom.PublicConstants.Module.ContractStatusGuid.ScanSendedBusinessUnitForSigningGuid.ToString()));
    }

    public virtual void SubmitToSignCopy(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var lockedContracts = Solution.Functions.Module.Remote.SetStateOnSigningCopy(_objs.ToList<Sungero.Contracts.IContractualDocument>());
      if (lockedContracts.Count == 0)
        Dialogs.NotifyMessage(DirRX.Solution.Contracts.Resources.SelectedContractsSetStateOnSigningCopySuccessfully);
      else
        DirRX.Solution.Functions.Module.ShowActionResultDialog(_objs.Count(), lockedContracts.ToList<Sungero.Contracts.IContractualDocument>(), DirRX.Solution.Contracts.Resources.SelectedContractsSetStateOnSigningCopyPartially);
      
    }

    public virtual bool CanSignedCopy(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _objs.Any() &&
        _objs.All(c => c.ScanMoveStatuses.Any(s => s.Status.Sid == ContractsCustom.PublicConstants.Module.ContractStatusGuid.ScanSendedBusinessUnitForSigningGuid.ToString()));
    }

    public virtual void SignedCopy(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var lockedContracts = Solution.Functions.Module.Remote.ChangeDocsSigningCopyState(_objs.ToList<Sungero.Contracts.IContractualDocument>(), true);
      if (lockedContracts.Count == 0)
        Dialogs.NotifyMessage(DirRX.Solution.Contracts.Resources.SelectedContractsChangeStateSignedCopySuccessfully);
      else
        DirRX.Solution.Functions.Module.ShowActionResultDialog(_objs.Count(), lockedContracts.ToList<Sungero.Contracts.IContractualDocument>(), DirRX.Solution.Contracts.Resources.SelectedContractsChangeStateSignedCopyPartially);
    }

    public virtual bool CanNotSignedCopy(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return CanSignedCopy(e);
    }

    public virtual void NotSignedCopy(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var lockedContracts = Solution.Functions.Module.Remote.ChangeDocsSigningCopyState(_objs.ToList<Sungero.Contracts.IContractualDocument>(), false);
      if (lockedContracts.Count == 0)
        Dialogs.NotifyMessage(DirRX.Solution.Contracts.Resources.SelectedContractsChangeStateSignedCopySuccessfully);
      else
        DirRX.Solution.Functions.Module.ShowActionResultDialog(_objs.Count(), lockedContracts.ToList<Sungero.Contracts.IContractualDocument>(), DirRX.Solution.Contracts.Resources.SelectedContractsChangeStateSignedCopyPartially);
    }

    public virtual bool CanCancelSignTransferCopy(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return CanSignedCopy(e);
    }

    public virtual void CancelSignTransferCopy(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var lockedContracts = Solution.Functions.Module.Remote.RemoveStateOnSigningCopy(_objs.ToList<Sungero.Contracts.IContractualDocument>());
      if (lockedContracts.Count == 0)
        Dialogs.NotifyMessage(DirRX.Solution.Contracts.Resources.SelectedContractsRemoveStateOnSigningCopySuccessfully);
      else
        DirRX.Solution.Functions.Module.ShowActionResultDialog(_objs.Count(), lockedContracts.ToList<Sungero.Contracts.IContractualDocument>(), DirRX.Solution.Contracts.Resources.SelectedContractsRemoveStateOnSigningCopyPartially);
    }

    public virtual bool CanCancelSignTransfer(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _objs.Any() && _objs.All(c => c.OriginalSigning == OriginalSigning.OnSigning);
    }

    public virtual void CancelSignTransfer(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var lockedContracts = Functions.Contract.Remote.CancelStateOnSigning(_objs.ToList<Sungero.Contracts.IContractualDocument>());
      if (lockedContracts.Count == 0)
        Dialogs.NotifyMessage(DirRX.Solution.Contracts.Resources.SelectedContractsCancelStateOnSigningSuccessfully);
      else
        DirRX.Solution.Functions.Module.ShowActionResultDialog(_objs.Count(), lockedContracts, DirRX.Solution.Contracts.Resources.SelectedContractsCancelStateOnSigningPartially);
    }
  }


}