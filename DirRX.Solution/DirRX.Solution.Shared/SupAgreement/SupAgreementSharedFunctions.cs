using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.SupAgreement;

namespace DirRX.Solution.Shared
{
  partial class SupAgreementFunctions
  {

    /// <summary>
    /// Заполнить признак "Требуется анализ на признак МСФО 16", если:
    /// Сумма и срок в документе должны быть больше чем в настройке.
    /// </summary>       
    public void UpdateAnalysisRequired()
    {
      var contract = _obj.LeadingDocument == null ? null : DirRX.Solution.Contracts.As(_obj.LeadingDocument);
      if (contract != null)
      {
        if (contract.IsAnalysisRequired == true)
        {
          _obj.IsAnalysisRequired = true;
          return;
        }
        
        if (contract.AnalysisRequiredExclude == false)
        {
          _obj.IsAnalysisRequired = false;
          return;
        }
        
        if (contract.StandartForm != null && contract.StandartForm.IsAnalysisRequired == true && _obj.InternalApprovalState == null)
        {
          // Если контрагент группы Лукойл и резидент, то проверка не требуется.
          var isNonresident = contract.IsManyCounterparties == true ?
            contract.Counterparties.All(c => c.Counterparty.IsLUKOILGroup == true && c.Counterparty.Nonresident == false) :
            contract.Counterparty != null && contract.Counterparty.IsLUKOILGroup == true && contract.Counterparty.Nonresident == false;
          
          if (isNonresident)
          {
            _obj.AnalysisRequiredExclude = true;
            _obj.IsAnalysisRequired = false;
            return;
          }
          
          // Высчитать сумму с учетом валюты.
          var currencyRub = DirRX.ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetCurrencyRUB();
          
          var settingAmount = contract.StandartForm.TransactionAmountAnalysisRequired;
          if (settingAmount != null)
            settingAmount = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetSummInRUB(contract.StandartForm.TransactionAmountAnalysisRequired.Value, contract.StandartForm.CurrencyAnalysisRequired ?? currencyRub);
          
          double documentAmount = 0.0;
          
          var supAgreements = DirRX.ContractsCustom.PublicFunctions.Module.Remote.GetSupAgreements(contract);
          
          if (contract.IsFrameContract == true)
          {
            // Для рамочных договоров сумму смотрим только по договору основанию.
            if (contract.TransactionAmount.HasValue)
              documentAmount = contract.TransactionAmount.Value;
          }
          else
          {
            // Сумма всех дополнительных соглашений в состоянии: Действующий, Недействующий. Закрыт в SAP, Недействующий. Открыт в SAP, Уничтоженный и текущий документ.
            if (supAgreements.Where(s => s.TransactionAmount.HasValue).Any())
              documentAmount = supAgreements.Where(s => s.TransactionAmount.HasValue).Sum(s => s.TransactionAmount.Value);
            
            // Добавим текущий документ если он в разработке.
            if (_obj.TransactionAmount.HasValue && _obj.LifeCycleState == LifeCycleState.Draft)
              documentAmount += _obj.TransactionAmount.Value;
            
            // Добавим сумму договора.
            if (contract.TransactionAmount.HasValue)
              documentAmount += contract.TransactionAmount.Value;
          }
          
          // Высчитать общую сумму.
          if (documentAmount > 0.0)
            documentAmount = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetSummInRUB(documentAmount, _obj.Currency ?? currencyRub);
          
          // Расчитать срок.
          var documentTerm = 0;
          if (contract.StandartForm.ContractTermAnalysisRequired.HasValue && contract.IsTermless != true)
          {
            int maxTerm = 0;
            if (contract.ValidFrom != null && contract.ValidTill != null)
              maxTerm = ContractsCustom.PublicFunctions.Module.GetDateDifferenceInMonth(contract.ValidFrom.Value, contract.ValidTill.Value);
            else if (contract.DocumentValidity.HasValue)
              maxTerm = contract.DocumentValidity.Value;
            
            // Получить наибольший срок договора + доп. соглашений.
            foreach (var supAgreement in supAgreements)
            {
              if (supAgreement.Id != _obj.Id)
              {
                int supTerm = 0;
                if (contract.ValidFrom != null && supAgreement.ValidTill != null)
                  supTerm = ContractsCustom.PublicFunctions.Module.GetDateDifferenceInMonth(contract.ValidFrom.Value, supAgreement.ValidTill.Value);
                else if (supAgreement.DocumentValidity.HasValue)
                  supTerm = supAgreement.DocumentValidity.Value;
                
                if (supTerm > maxTerm)
                  maxTerm = supTerm;
              }
            }
            
            // Добавим текущий документ.
            int currentTerm = 0;
            // Вычислить значение Действует по, если оно не заполнено или изменился срок действия или Действует с.
            if ((!_obj.ValidTill.HasValue || _obj.State.Properties.DocumentValidity.IsChanged || _obj.State.Properties.ValidFrom.IsChanged)
                && _obj.DocumentValidity.HasValue && _obj.ValidFrom.HasValue)
              _obj.ValidTill = _obj.ValidFrom.Value.AddMonths(_obj.DocumentValidity.Value).AddDays(-1);
            
            if (contract.ValidFrom != null && _obj.ValidTill != null)
              currentTerm = ContractsCustom.PublicFunctions.Module.GetDateDifferenceInMonth(contract.ValidFrom.Value, _obj.ValidTill.Value);
            else if (_obj.DocumentValidity.HasValue)
              currentTerm = _obj.DocumentValidity.Value;
            
            if (currentTerm > maxTerm)
              maxTerm = currentTerm;
              
            documentTerm = maxTerm;
          }
          
          if ((documentAmount > 0.0 && documentAmount >= settingAmount) &&
              (contract.IsTermless == true || documentTerm >= contract.StandartForm.ContractTermAnalysisRequired))
          {
            _obj.AnalysisRequiredExclude = false;
            _obj.IsAnalysisRequired = true;
          }
          else
          {
            _obj.AnalysisRequiredExclude = true;
            _obj.IsAnalysisRequired = false;
          }
        }
      }
    }
    
    /// <summary>
    /// Сменить доступность поля контрагент. Доступность зависит от статуса.
    /// </summary>
    /// <param name="isEnabled">Признак доступности поля. TRUE - поле доступно.</param>
    /// <param name="counterpartyCodeInNumber">Признак вхождения кода контрагента в формат номера. TRUE - входит.</param>
    /// <param name="enabledState">Признак доступность поля взависимости от статуса.</param>
    public override void ChangeCounterpartyPropertyAccess(bool isEnabled, bool counterpartyCodeInNumber, bool enabledState)
    {
      base.ChangeCounterpartyPropertyAccess(isEnabled, counterpartyCodeInNumber, enabledState);
      
      _obj.State.Properties.IsManyCounterparties.IsEnabled = isEnabled && !counterpartyCodeInNumber && enabledState;
    }
    
    /// <summary>
    /// Установка статуса "Ожидает отправки контрагенту".
    /// </summary>
    public void SetStatusOriginalWaitingForSending()
    {
      // Установка статуса договора "Ожидает отправки контрагенту".
      if (_obj.OriginalSigning == DirRX.Solution.SupAgreement.OriginalSigning.Signed &&
          (!string.IsNullOrEmpty(_obj.RegistrationNumber) ||
           (_obj.IsContractorSignsFirst != true && !(_obj.IsScannedImageSign == true && _obj.ContractActivate == ContractActivate.Copy))) &&
          !_obj.Tracking.Where(t => t.Action == Sungero.Docflow.OfficialDocumentTracking.Action.Sending && t.IsOriginal == true).Any())
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalWaitingForSendingGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus,
                                                                              false);
    }
    
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();
      
      // Содержание необязательно (в функции ChangeDocumentProperties не достаточно объявления необязательным).
      _obj.State.Properties.Subject.IsRequired = false;
      _obj.State.Properties.Department.IsRequired = false;
      _obj.State.Properties.ValidTill.IsRequired = false;
    }
    
    /// <summary>
    /// Перекрытие разделяемой функции FillName. Вызывать эту функцию в событии "Изменение значения свойства" в свойствах, значения которых влияют на имя документа.
    /// Вызов определен у свойств: DocumentDate
    /// </summary>
    public override void FillName()
    {
      var documentKind = _obj.DocumentKind;
      
      if (documentKind != null && !documentKind.GenerateDocumentName.Value && _obj.Name == Sungero.Docflow.OfficialDocuments.Resources.DocumentNameAutotext)
        _obj.Name = string.Empty;
      
      if (documentKind == null || !documentKind.GenerateDocumentName.Value)
        return;
      
      var name = string.Empty;
      
      // Имя в формате: <Вид документа> <рег. №> от <Дата документа> к <Вид документа основания> <Категория><рег. № документа основания> от <Дата документа основания> с <Контрагент>.
      
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += Sungero.Docflow.OfficialDocuments.Resources.Number + _obj.RegistrationNumber + " ";
        
        if (_obj.ActualDate != null)
          name += DirRX.Solution.SupAgreements.Resources.From + _obj.ActualDate.Value.ToString("dd.MM.yyyy") + " ";
        else if (_obj.RegistrationDate != null)
          name += DirRX.Solution.SupAgreements.Resources.From + _obj.RegistrationDate.Value.ToString("dd.MM.yyyy") + " ";
        
        if (_obj.LeadingDocument != null)
        {
          var contract = Contracts.As(_obj.LeadingDocument);
          name += DirRX.Solution.SupAgreements.Resources.To + contract.DocumentKind.DisplayValue + " ";
          if (contract.DocumentGroup != null)
            name += contract.DocumentGroup.DisplayValue + " ";
          if (contract.RegistrationDate.HasValue)
            name += contract.RegistrationNumber + DirRX.Solution.SupAgreements.Resources.FromPlusToSpace + contract.RegistrationDate.Value.ToString("dd.MM.yyyy");
        }

        if (_obj.Counterparty != null)
          name += DirRX.Solution.SupAgreements.Resources.CPlusTwoSpace + _obj.Counterparty.DisplayValue;
      }
      
      if (string.IsNullOrWhiteSpace(name))
        name = Sungero.Docflow.OfficialDocuments.Resources.DocumentNameAutotext;
      else if (_obj.DocumentKind != null)
        name = _obj.DocumentKind.ShortName + " " + name;
      
      _obj.Name = Sungero.Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
    }
    
    /// <summary>
    /// Сменить тип документа на недействующий.
    /// </summary>
    /// <param name="isActive">True, если документ действующий.</param>
    public override void SetObsolete(bool isActive)
    {
      _obj.LifeCycleState = LifeCycleState.Obsolete;
    }

    public override void UpdateLifeCycle(Enumeration? registrationState,
                                         Enumeration? approvalState,
                                         Enumeration? counterpartyApprovalState)
    {
      // Не проверять статусы для пустых параметров.
      if (_obj == null || _obj.DocumentKind == null)
        return;
      
      var direction = _obj.DocumentKind.DocumentFlow;
      var currentState = _obj.LifeCycleState;
      var lifeCycleMustByActive = IsLifeCycleMustBeActive(direction, approvalState, counterpartyApprovalState);
      
      // Если регистрация была отменена, а документ действующий согласно функции - ставим статус в разработке.
      if (currentState == LifeCycleState.Active &&
          registrationState == RegistrationState.NotRegistered &&
          _obj.State.Properties.RegistrationState.OriginalValue != registrationState &&
          _obj.State.Properties.RegistrationState.OriginalValue != null &&
          lifeCycleMustByActive)
        _obj.LifeCycleState = Sungero.Docflow.OfficialDocument.LifeCycleState.Draft;

      // Документ должен быть в разработке (или null) и зарегистрирован.
      if ((currentState != null && currentState != Sungero.Docflow.OfficialDocument.LifeCycleState.Draft) ||
          registrationState != Sungero.Docflow.OfficialDocument.RegistrationState.Registered)
        return;
    }
    
    /// <summary>
    /// Определить доступность полей.
    /// </summary>
    public void ChangeDocumentProperties()
    {
      var isDPOEmployeesRole = Users.Current.IncludedIn(DirRX.ContractsCustom.PublicConstants.Module.RoleGuid.DPOEmployeesRole);
      var startConditionsExists = _obj.StartConditionsExists == true;
      _obj.State.Properties.IsCorporateApprovalRequired.IsEnabled = isDPOEmployeesRole;
      _obj.State.Properties.ContractActivate.IsEnabled = isDPOEmployeesRole;
      _obj.State.Properties.StartConditionsExists.IsEnabled = isDPOEmployeesRole;
      _obj.State.Properties.StartConditions.IsEnabled = isDPOEmployeesRole && startConditionsExists;
      _obj.State.Properties.AreConditionsCompleted.IsEnabled = isDPOEmployeesRole && startConditionsExists;
      
      _obj.State.Properties.CoExecutor.IsEnabled = Users.Current.IncludedIn(DirRX.ContractsCustom.PublicConstants.Module.RoleGuid.CustomerServiceEmployeesRole);
      
      _obj.State.Properties.Currency.IsEnabled = false;
      _obj.State.Properties.LukoilApproving.IsEnabled = isDPOEmployeesRole;
      var contract = _obj.LeadingDocument == null ? null : DirRX.Solution.Contracts.As(_obj.LeadingDocument);
      _obj.State.Properties.BrandedProducts.IsVisible = contract != null && contract.DocumentGroup != null && contract.DocumentGroup.IsShowBranded.GetValueOrDefault();
      _obj.State.Properties.Subject.IsRequired = false;
      _obj.State.Properties.DestinationCountries.IsEnabled = _obj.LeadingDocument != null ?
        !(DirRX.Solution.Contracts.As(_obj.LeadingDocument).DocumentGroup != null && DirRX.Solution.Contracts.As(_obj.LeadingDocument).DocumentGroup.DestinationCountry == DirRX.Solution.ContractCategory.DestinationCountry.RF) :
        true;
      _obj.State.Properties.DestinationCountries.IsVisible = _obj.LeadingDocument != null ?
        !(DirRX.Solution.Contracts.As(_obj.LeadingDocument).DocumentGroup != null && DirRX.Solution.Contracts.As(_obj.LeadingDocument).DocumentGroup.DestinationCountry == DirRX.Solution.ContractCategory.DestinationCountry.NotRequired) :
        true;
    }
    
    /// <summary>
    /// Получить номер ведущего документа.
    /// </summary>
    /// <returns>Номер документа либо пустая строка.</returns>
    /// <remarks>Переопределяем для возможности вызова в коде.</remarks>
    public override string GetLeadDocumentNumber()
    {
      return base.GetLeadDocumentNumber();
    }
    
    /// <summary>
    /// Определить обязательность "Действует по" и "Срок действия документа".
    /// </summary>
    public void CheckValidTillState()
    {
      //_obj.State.Properties.ValidTill.IsRequired = !_obj.DocumentValidity.HasValue;
      //_obj.State.Properties.DocumentValidity.IsRequired = !_obj.ValidTill.HasValue;
    }
    
    /// <summary>
    /// Изменить отображение панели регистрации.
    /// </summary>
    /// <param name="needShow">Признак отображения.</param>
    /// <param name="repeatRegister">Признак повторной регистрации\изменения реквизитов.</param>
    public override void ChangeRegistrationPaneVisibility(bool needShow, bool repeatRegister)
    {
      base.ChangeRegistrationPaneVisibility(needShow, repeatRegister);
      _obj.State.Properties.DeliveryMethod.IsEnabled = true;
      _obj.State.Properties.DeliveryMethod.IsVisible = true;
      
      _obj.State.Properties.ApproveLabel.IsVisible = needShow && !string.IsNullOrWhiteSpace(_obj.ApproveLabel);
      _obj.State.Properties.ScanMoveLabel.IsVisible = needShow && !string.IsNullOrWhiteSpace(_obj.ScanMoveLabel);
      _obj.State.Properties.OriginalMoveLabel.IsVisible = needShow && !string.IsNullOrWhiteSpace(_obj.OriginalMoveLabel);
      
      _obj.State.Properties.InternalApprovalState.IsVisible = false;
      _obj.State.Properties.ExternalApprovalState.IsVisible = false;
      _obj.State.Properties.ExecutionState.IsVisible = false;
      _obj.State.Properties.ControlExecutionState.IsVisible = false;
      _obj.State.Properties.ExchangeState.IsVisible = false;
    }
    
    /// <summary>
    /// Подстановка настройки договора по заполненным свойствам.
    /// </summary>
    public void SelectStandartFrom()
    {
      var contract = Contracts.As(_obj.LeadingDocument);
      if (_obj.DocumentKind != null && _obj.IsStandard.HasValue && contract != null &&
          (_obj.IsManyCounterparties != true && _obj.Counterparty != null && _obj.Counterparty.IsLUKOILGroup.HasValue) ||
          (_obj.IsManyCounterparties == true && _obj.Counterparties.Any(c => c.Counterparty.IsLUKOILGroup.HasValue)))
      {
        var isLUKOILGroup = _obj.IsManyCounterparties == true ? _obj.Counterparties.All(c => c.Counterparty.IsLUKOILGroup == true) : _obj.Counterparty.IsLUKOILGroup.Value;
        _obj.StandartForm = DirRX.ContractsCustom.PublicFunctions.ContractSettings.Remote.GetContractSetting(_obj.IsStandard.Value,
                                                                                                             DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.SupAgreement,
                                                                                                             _obj.DocumentKind,
                                                                                                             _obj.DocumentGroup,
                                                                                                             contract.Subcategory,
                                                                                                             isLUKOILGroup);
      }
    }
    
    /// <summary>
    /// Заполнить поле "Согласование с ПАО "Лукойл" значением "Требуется", если:
    /// Сумма и срок в документе должны быть больше чем в настройке (если в настройке эти поля заполнены).
    /// </summary>
    public void UpdateLukoilApproving()
    {
      bool lukoilApprovingRequired = false;
      var contract = _obj.LeadingDocument == null ? null : DirRX.Solution.Contracts.As(_obj.LeadingDocument);
      if (contract != null && contract.StandartForm != null && _obj.InternalApprovalState == null)
      {
        // Высчитать сумму с учетом валюты.
        var currencyRub = DirRX.ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetCurrencyRUB();
        
        double documentAmount = 0.0;
        double documentAmountPreviousValue = 0.0;
        var validTillPreviousValue = _obj.State.Properties.ValidTill.PreviousValue;
        var validFromPreviousValue = _obj.State.Properties.ValidFrom.PreviousValue;
        var DocumentValidityPreviousValue = _obj.State.Properties.DocumentValidity.PreviousValue;
        var currencyPreviousValue = _obj.State.Properties.Currency.PreviousValue;
        
        var supAgreements = DirRX.ContractsCustom.PublicFunctions.Module.Remote.GetSupAgreements(contract);
        
        if (contract.IsFrameContract == true)
        {
          // Для рамочных договоров сумму смотрим только по договору основанию.
          if (contract.TransactionAmount.HasValue)
          {
            documentAmount = contract.TransactionAmount.Value;
            documentAmountPreviousValue = documentAmount;
          }
        }
        else
        {
          // Сумма всех дополнительных соглашений в состоянии: Действующий, Недействующий. Закрыт в SAP, Недействующий. Открыт в SAP, Уничтоженный и текущий документ.
          if (supAgreements.Where(s => s.TransactionAmount.HasValue).Any())
            documentAmount = supAgreements.Where(s => s.TransactionAmount.HasValue).Sum(s => s.TransactionAmount.Value);
          
          // Добавим текущий документ если он в разработке.
          if (_obj.TransactionAmount.HasValue && _obj.LifeCycleState == LifeCycleState.Draft)
            documentAmount += _obj.TransactionAmount.Value;
          
          // Добавим сумму договора.
          if (contract.TransactionAmount.HasValue)
            documentAmount += contract.TransactionAmount.Value;
          
          if (_obj.State.Properties.TransactionAmount.IsChanged)
          {
            documentAmountPreviousValue = documentAmount;
            if (_obj.TransactionAmount.HasValue)
              documentAmountPreviousValue -= _obj.TransactionAmount.Value;
            if (_obj.State.Properties.TransactionAmount.PreviousValue.HasValue)
              documentAmountPreviousValue += _obj.State.Properties.TransactionAmount.PreviousValue.Value;
          }
          else
            documentAmountPreviousValue = documentAmount;
        }
        
        // Высчитать общую сумму.
        if (documentAmount > 0.0)
          documentAmount = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetSummInRUB(documentAmount, _obj.Currency ?? currencyRub);
        
        if (documentAmountPreviousValue > 0.0)
          documentAmountPreviousValue = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetSummInRUB(documentAmountPreviousValue, currencyPreviousValue ?? currencyRub);
        
        // Расчитать срок.
        var documentTerm = 0.0;
        var documentTermPreviousValue = 0.0;
        if (contract.IsTermless != true)
        {
          var terms = new List<double>();
          var maxTerm = 0.0;
          
          if (contract.ValidFrom != null && contract.ValidTill != null)
            maxTerm = ContractsCustom.PublicFunctions.Module.GetDateDifferenceInYear(contract.ValidFrom.Value, contract.ValidTill.Value);
          else if (contract.DocumentValidity.HasValue)
            maxTerm = contract.DocumentValidity.Value / 12.0;
          
          if (maxTerm > 0)
            terms.Add(maxTerm);
          
          // Получить наибольший срок договора + доп. соглашений, исключая текущее.
          foreach (var supAgreement in supAgreements)
          {
            if (supAgreement.Id != _obj.Id)
            {
              var supTerm = 0.0;
              if (contract.ValidFrom != null && supAgreement.ValidTill != null)
                supTerm = ContractsCustom.PublicFunctions.Module.GetDateDifferenceInYear(contract.ValidFrom.Value, supAgreement.ValidTill.Value);
              else if (supAgreement.DocumentValidity.HasValue)
                supTerm = supAgreement.DocumentValidity.Value / 12.0;
              
              if (supTerm > 0)
                terms.Add(supTerm);
              
              if (supTerm > maxTerm)
                maxTerm = supTerm;
            }
          }
          
          // Добавим текущий документ.          
          var currentTerm = 0.0;
          // Вычислить значение Действует по, если оно не заполнено или изменился срок или Действуует с.
          if ((!_obj.ValidTill.HasValue || _obj.State.Properties.DocumentValidity.IsChanged || _obj.State.Properties.ValidFrom.IsChanged)
              && _obj.DocumentValidity.HasValue && _obj.ValidFrom.HasValue)
            _obj.ValidTill = _obj.ValidFrom.Value.AddMonths(_obj.DocumentValidity.Value).AddDays(-1);
          
          if (contract.ValidFrom != null && _obj.ValidTill != null)
            currentTerm = ContractsCustom.PublicFunctions.Module.GetDateDifferenceInYear(contract.ValidFrom.Value, _obj.ValidTill.Value);
          else if (_obj.DocumentValidity.HasValue)
            currentTerm = _obj.DocumentValidity.Value / 12.0;
          
          if (currentTerm > maxTerm)
            maxTerm = currentTerm;          
          
          documentTerm = maxTerm;
          
          // Высчитать предыдущий срок.
          if (_obj.State.Properties.DocumentValidity.IsChanged || _obj.State.Properties.ValidTill.IsChanged || _obj.State.Properties.ValidFrom.IsChanged)
          {
            var previousTerm = 0.0;
            if (contract.ValidFrom != null && validTillPreviousValue != null)
              previousTerm = ContractsCustom.PublicFunctions.Module.GetDateDifferenceInYear(contract.ValidFrom.Value, validTillPreviousValue.Value);
            else if (DocumentValidityPreviousValue.HasValue)
              previousTerm = DocumentValidityPreviousValue.Value / 12.0;
            
            if (previousTerm > 0)
              terms.Add(previousTerm);
            
            if (terms.Count > 0)
              documentTermPreviousValue = terms.Max();
          }
          else
            documentTermPreviousValue = documentTerm;
        }
        
        foreach (var dRow in contract.StandartForm.BindingDocumentCondition.Where(d => contract.IsTender == true || d.DocumentsForTender == contract.IsTender))
        {
          var setting = dRow.DocumentKind;
          var settingAmount = setting.TransactionAmount;
          
          if (settingAmount != null)
            settingAmount = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetSummInRUB(settingAmount.Value, setting.Currency ?? currencyRub);
          
          if ((settingAmount == null || documentAmount == 0.0 || documentAmount > settingAmount ) &&
              (setting.DocumentValidity == null || contract.IsTermless == true || documentTerm >= setting.DocumentValidity))
          {
            lukoilApprovingRequired = true;
            if (!_obj.RequiredDocuments.Any(r => r.DocumentKind == setting.Name))
              _obj.RequiredDocuments.AddNew().DocumentKind = setting.Name;
          }
          else
          {
            // Настройка не применилась, удалить вид документа, который был добавлен до изменения значений.
            if ((settingAmount == null || documentAmountPreviousValue == 0.0 || documentAmountPreviousValue > settingAmount ) &&
                (setting.DocumentValidity == null || contract.IsTermless == true || documentTermPreviousValue >= setting.DocumentValidity))
            {
              while (_obj.RequiredDocuments.Any(r => r.DocumentKind == setting.Name && r.Document == null))
                _obj.RequiredDocuments.Remove(_obj.RequiredDocuments.First(r => r.DocumentKind == setting.Name && r.Document == null));
            }
          }
        }
      }
      if (lukoilApprovingRequired == true)
        _obj.LukoilApproving = DirRX.Solution.SupAgreement.LukoilApproving.Required;
      else
        _obj.LukoilApproving = DirRX.Solution.SupAgreement.LukoilApproving.NotRequired;
    }
    
    /// <summary>
    /// Заполнить свойство "Дата уничтожения оригинала".
    /// <description>1 января года следующего за датой окончания действия договорного документа + срок хранения (зафиксирован в категории)</description>
    /// </summary>
    public void ChangeDestructionDate()
    {
      var contract = Contracts.As(_obj.LeadingDocument);
      if (contract != null)
        _obj.DestructionDate = PublicFunctions.Contract.GetDestructionDate(_obj.ValidTill, contract.DocumentGroup);
    }
    
    /// <summary>
    /// Очистить список контрагентов и заполнить первого контрагента из карточки.
    /// </summary>
    public void ClearAndFillFirstCounterparty()
    {
      _obj.Counterparties.Clear();
      if (_obj.Counterparty != null)
      {
        var newCounterparty = _obj.Counterparties.AddNew();
        newCounterparty.Number = 1;
        newCounterparty.Counterparty = _obj.Counterparty;
        newCounterparty.Signatory = DirRX.Solution.Contacts.As(_obj.CounterpartySignatory);
        newCounterparty.Contact = DirRX.Solution.Contacts.As(_obj.Contact);
        newCounterparty.Address = _obj.ShippingAddress;
        newCounterparty.DeliveryMethod = _obj.DeliveryMethod;
        newCounterparty.IDSap = _obj.IDSap;
      }
    }
  }
}