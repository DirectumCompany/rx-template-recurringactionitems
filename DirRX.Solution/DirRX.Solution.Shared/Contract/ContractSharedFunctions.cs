using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using DirRX.Solution.Contract;

namespace DirRX.Solution.Shared
{
  partial class ContractFunctions
  {
    /// <summary>
	/// Сменить обязательность реквизитов.
	/// </summary>
	public override void SetRequiredProperties()
	{
		base.SetRequiredProperties();
		_obj.State.Properties.Subject.IsRequired = false;
		_obj.State.Properties.DocumentGroup.IsRequired = false;
		_obj.State.Properties.Department.IsRequired = false;
		_obj.State.Properties.ValidTill.IsRequired = false;
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
    /// Установка статуса договора "Ожидает отправки контрагенту".
    /// </summary>
    public void SetStatusOriginalWaitingForSending()
    {
      // Установка статуса договора "Ожидает отправки контрагенту".
      if (_obj.OriginalSigning == DirRX.Solution.Contract.OriginalSigning.Signed &&
          (!string.IsNullOrEmpty(_obj.RegistrationNumber) ||
           (_obj.IsContractorSignsFirst != true && !(_obj.IsScannedImageSign == true && _obj.ContractActivate == ContractActivate.Copy))) &&
          !_obj.Tracking.Where(t => t.Action == Sungero.Docflow.OfficialDocumentTracking.Action.Sending && t.IsOriginal == true).Any())
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalWaitingForSendingGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus,
                                                                              false);
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
    /// Определить доступность полей.
    /// </summary>
    public void ChangeDocumentProperties()
    {
      var isFrameContract = _obj.IsFrameContract == true;
      
      var isDPOEmployeesRole = Users.Current.IncludedIn(DirRX.ContractsCustom.PublicConstants.Module.RoleGuid.DPOEmployeesRole);
      var startConditionsExists = _obj.StartConditionsExists == true;
      _obj.State.Properties.IsCorporateApprovalRequired.IsEnabled = isDPOEmployeesRole;
      _obj.State.Properties.ContractActivate.IsEnabled = isDPOEmployeesRole;
      _obj.State.Properties.StartConditionsExists.IsEnabled = isDPOEmployeesRole;
      _obj.State.Properties.StartConditions.IsEnabled = isDPOEmployeesRole && startConditionsExists;
      _obj.State.Properties.AreConditionsCompleted.IsEnabled = isDPOEmployeesRole && startConditionsExists;
      
      _obj.State.Properties.CoExecutor.IsEnabled = Users.Current.IncludedIn(DirRX.ContractsCustom.PublicConstants.Module.RoleGuid.CustomerServiceEmployeesRole);
      _obj.State.Properties.TenderStep.IsVisible = _obj.IsTender == true;
      
      _obj.State.Properties.IsBranded.IsVisible = _obj.DocumentGroup != null && _obj.DocumentGroup.IsShowBranded.GetValueOrDefault();
      _obj.State.Properties.BrandedProducts.IsVisible = _obj.DocumentGroup != null && _obj.DocumentGroup.IsShowBranded.GetValueOrDefault();
      _obj.State.Properties.HolderTZ.IsVisible = _obj.DocumentGroup != null && _obj.DocumentGroup.IsShowRightsHolder.GetValueOrDefault();
      _obj.State.Properties.LukoilApproving.IsEnabled = isDPOEmployeesRole;
      _obj.State.Properties.Subcategory.IsRequired = _obj.IsStandard.Value && _obj.DocumentGroup != null && _obj.DocumentGroup.CounterpartySubcategories.Any();
      _obj.State.Properties.DestinationCountries.IsEnabled = !(_obj.DocumentGroup != null && _obj.DocumentGroup.DestinationCountry == DirRX.Solution.ContractCategory.DestinationCountry.RF);
      _obj.State.Properties.DestinationCountries.IsVisible = !(_obj.DocumentGroup != null && _obj.DocumentGroup.DestinationCountry == DirRX.Solution.ContractCategory.DestinationCountry.NotRequired);
      
      // Установить доступность полей Код вида договора ИУС ЛЛК, Закупка, Сбыт.
      bool isContractFunctionalityMixed = _obj.ContractFunctionality != null && _obj.ContractFunctionality == ContractFunctionality.Mixed;
  	  _obj.State.Properties.IMSCodeCollection.IsVisible = !isContractFunctionalityMixed;
  	  _obj.State.Properties.IMSCodePurchaseCollection.IsVisible = isContractFunctionalityMixed;
  	  _obj.State.Properties.IMSCodeSaleCollection.IsVisible = isContractFunctionalityMixed;
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
    /// Заполнить системного контрагента, если согласуется проект договора.
    /// </summary>
    public void ProcessDefaultTenderCounterparty()
    {
      var defaultCounterpary = DirRX.Solution.Companies.As(DirRX.ContractsCustom.PublicFunctions.Module.Remote.GetTenderPurchaseCounterparty());
      if (_obj.IsTender != true)
      {
        _obj.State.Properties.Counterparty.IsEnabled = true;
        if (DirRX.Solution.Companies.Equals(defaultCounterpary, _obj.Counterparty))
          _obj.Counterparty = null;
        return;
      }
      
      if (!_obj.ContractFunctionality.HasValue || !_obj.TenderStep.HasValue)
        return;
      
      var enabled = !(_obj.ContractFunctionality == ContractFunctionality.Purchase && _obj.TenderStep == TenderStep.ApprovalProject);
      if (enabled && DirRX.Solution.Companies.Equals(defaultCounterpary, _obj.Counterparty))
        _obj.Counterparty = null;
      if (!enabled)
        _obj.Counterparty = defaultCounterpary;
      
      _obj.State.Properties.Counterparty.IsEnabled = enabled;
    }
    
    /// <summary>
    /// Определить обязательность "Действует по", "Напоминать об окончании за" и "Срок действия документа".
    /// </summary>
    public void CheckValidTillState()
    {
      var isTermless = _obj.IsTermless == true;
      
      //_obj.State.Properties.DaysToFinishWorks.IsRequired = !isTermless;
      //_obj.State.Properties.ValidTill.IsRequired = !isTermless && !_obj.DocumentValidity.HasValue;
      //_obj.State.Properties.DocumentValidity.IsRequired = !isTermless && !_obj.ValidTill.HasValue;
    }
    
    /// <summary>
    /// Заполнить имя.
    /// </summary>
    public override void FillName()
    {
      if (_obj != null &&
          _obj.DocumentKind != null &&
          !_obj.DocumentKind.GenerateDocumentName.Value &&
          _obj.Name == OfficialDocuments.Resources.DocumentNameAutotext)
        _obj.Name = string.Empty;
      
      if (_obj.DocumentKind == null || !_obj.DocumentKind.GenerateDocumentName.Value)
        return;
      
      var name = string.Empty;
      
      /* Имя в формате:
        <Вид документа> №<номер> от <дата документа> с <контрагент> "<категория>".
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (_obj.ActualDate.HasValue)
          name += OfficialDocuments.Resources.DateFrom + _obj.ActualDate.Value.ToString("d");
        
        if (_obj.Counterparty != null)
          name += Sungero.Contracts.ContractBases.Resources.NamePartForContractor + _obj.Counterparty.DisplayValue;
        
        if (_obj.DocumentGroup != null)
          name += " \"" + _obj.DocumentGroup.Name + "\"";
      }
      
      if (string.IsNullOrWhiteSpace(name))
        name = OfficialDocuments.Resources.DocumentNameAutotext;
      else if (_obj.DocumentKind != null)
        name = _obj.DocumentKind.ShortName + name;
      
      name = Sungero.Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Sungero.Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(name, _obj);
    }
    
    /// <summary>
    /// Сменить тип документа на недействующий.
    /// </summary>
    /// <param name="isActive">True, если документ действующий.</param>
    public override void SetObsolete(bool isActive)
    {
      _obj.LifeCycleState = LifeCycleState.Obsolete;
    }
    
    /// <summary>
    /// Подстановка настройки договора по заполненным свойствам.
    /// </summary>
    public void SelectStandartFrom()
    {
      if (_obj.DocumentKind != null && _obj.IsStandard.HasValue && _obj.DocumentGroup != null &&
          (_obj.IsManyCounterparties != true && _obj.Counterparty != null && _obj.Counterparty.IsLUKOILGroup.HasValue) ||
          (_obj.IsManyCounterparties == true && _obj.Counterparties.Any(c => c.Counterparty.IsLUKOILGroup.HasValue)))
      {
        var isLUKOILGroup = _obj.IsManyCounterparties == true ? _obj.Counterparties.All(c => c.Counterparty.IsLUKOILGroup == true) : _obj.Counterparty.IsLUKOILGroup.Value;
        _obj.StandartForm = DirRX.ContractsCustom.PublicFunctions.ContractSettings.Remote.GetContractSetting(_obj.IsStandard.Value,
                                                                                                             DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.Contract,
                                                                                                             _obj.DocumentKind,
                                                                                                             _obj.DocumentGroup,
                                                                                                             _obj.Subcategory,
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
      if (_obj.StandartForm != null && _obj.InternalApprovalState == null)
      {      	
      	if (_obj.StandartForm.BindingDocumentCondition.Any())
      	{
      	  // Высчитать сумму с учетом валюты.
          var currencyRub = DirRX.ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetCurrencyRUB();
          
          var documentAmount = _obj.TransactionAmount;
          
          // Расчитать срок.
          var documentTerm = 0.0;          
          if (_obj.IsTermless != true)
          {
            if (_obj.ValidFrom != null && _obj.ValidTill != null)
              documentTerm = ContractsCustom.PublicFunctions.Module.GetDateDifferenceInYear(_obj.ValidFrom.Value, _obj.ValidTill.Value);
            else if (_obj.DocumentValidity.HasValue)
              documentTerm = _obj.DocumentValidity.Value / 12.0;
          }
          
          var documentAmountPreviousValue = _obj.State.Properties.TransactionAmount.PreviousValue;
          var validTillPreviousValue = _obj.State.Properties.ValidTill.PreviousValue;
          var validFromPreviousValue = _obj.State.Properties.ValidFrom.PreviousValue;
          var documentValidityPreviousValue = _obj.State.Properties.DocumentValidity.PreviousValue;
          var currencyPreviousValue = _obj.State.Properties.Currency.PreviousValue;
          // Расчитать предыдущий срок.
          var documentTermPreviousValue = 0.0;
          if (_obj.State.Properties.IsTermless.PreviousValue != true)
          {
            if (validFromPreviousValue != null && validTillPreviousValue != null)
              documentTermPreviousValue = ContractsCustom.PublicFunctions.Module.GetDateDifferenceInYear(validFromPreviousValue.Value, validTillPreviousValue.Value);
            else if (documentValidityPreviousValue.HasValue)
              documentTermPreviousValue = documentValidityPreviousValue.Value / 12.0;
          }
          
          foreach (var dRow in _obj.StandartForm.BindingDocumentCondition.Where(d => _obj.IsTender == true || d.DocumentsForTender == _obj.IsTender))
      	  {
      	  	var setting = dRow.DocumentKind;
      	  	var settingAmount = setting.TransactionAmount;
      	  	
      	  	if (settingAmount != null)
              settingAmount = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetSummInRUB(settingAmount.Value, setting.Currency ?? currencyRub);
      	  	
      	  	// Высчитать сумму документа, если в настройке она указана.
            if (settingAmount != null && documentAmount != null)
              documentAmount = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetSummInRUB(_obj.TransactionAmount.Value, _obj.Currency ?? currencyRub);
            
            if (settingAmount != null && documentAmountPreviousValue != null)
              documentAmountPreviousValue = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetSummInRUB(documentAmountPreviousValue.Value, currencyPreviousValue ?? currencyRub);
            
            if ((settingAmount == null || documentAmount == null || documentAmount > settingAmount ) &&
              (setting.DocumentValidity == null || _obj.IsTermless == true || documentTerm >= setting.DocumentValidity))
            {
              lukoilApprovingRequired = true;
              if (!_obj.RequiredDocuments.Any(r => r.DocumentKind == setting.Name))
                _obj.RequiredDocuments.AddNew().DocumentKind = setting.Name;
            }
            else
            {
              // Настройка не применилась, удалить вид документа, который был добавлен до изменения значений.
              if ((settingAmount == null || documentAmountPreviousValue == null || documentAmountPreviousValue > settingAmount ) &&
              (setting.DocumentValidity == null || _obj.State.Properties.IsTermless.PreviousValue == true || documentTermPreviousValue >= setting.DocumentValidity))
              {
              	while (_obj.RequiredDocuments.Any(r => r.DocumentKind == setting.Name && r.Document == null))
                  _obj.RequiredDocuments.Remove(_obj.RequiredDocuments.First(r => r.DocumentKind == setting.Name && r.Document == null));
              }
            }
      	  }      	  
      	}
      }
      
      if (lukoilApprovingRequired == true)
        _obj.LukoilApproving = DirRX.Solution.Contract.LukoilApproving.Required;
      else
        _obj.LukoilApproving = DirRX.Solution.Contract.LukoilApproving.NotRequired;	
    }
    
    /// <summary>
    /// Заполнить признак "Требуется анализ на признак МСФО 16", если:
    /// Сумма и срок в документе должны быть больше чем в настройке.
    /// </summary>
    public void UpdateAnalysisRequired()
    {
      if (_obj.StandartForm != null && _obj.StandartForm.IsAnalysisRequired == true && _obj.InternalApprovalState == null)
      {
        // Если контрагент группы Лукойл и резидент, то проверка не требуется.
        var isNonresident = _obj.IsManyCounterparties == true ?
          _obj.Counterparties.All(c => c.Counterparty.IsLUKOILGroup == true && c.Counterparty.Nonresident == false) :
          _obj.Counterparty != null && _obj.Counterparty.IsLUKOILGroup == true && _obj.Counterparty.Nonresident == false;
        
        if (isNonresident)
        {
          _obj.AnalysisRequiredExclude = true;
          _obj.IsAnalysisRequired = false;
          return;
        }
        
        // Высчитать сумму с учетом валюты.
        var currencyRub = DirRX.ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetCurrencyRUB();
        
        var settingAmount = _obj.StandartForm.TransactionAmountAnalysisRequired;
        var documentAmount = _obj.TransactionAmount;
        
        if (settingAmount != null)
          settingAmount = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetSummInRUB(_obj.StandartForm.TransactionAmountAnalysisRequired.Value, _obj.StandartForm.CurrencyAnalysisRequired ?? currencyRub);
        
        // Высчитать сумму документа, если в настройке она указана.
        if (settingAmount != null && documentAmount != null)
          documentAmount = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetSummInRUB(_obj.TransactionAmount.Value, _obj.Currency ?? currencyRub);
        
        // Расчитать срок.
        var documentTerm = 0;
        if (_obj.StandartForm.ContractTermAnalysisRequired.HasValue && _obj.IsTermless != true)
        {
          if (_obj.ValidFrom != null && _obj.ValidTill != null)
            documentTerm = ContractsCustom.PublicFunctions.Module.GetDateDifferenceInMonth(_obj.ValidFrom.Value, _obj.ValidTill.Value);
          else if (_obj.DocumentValidity.HasValue)
            documentTerm = _obj.DocumentValidity.Value;
        }
        
        if ((documentAmount != null && documentAmount >= settingAmount ) &&
            (_obj.IsTermless == true || documentTerm >= _obj.StandartForm.ContractTermAnalysisRequired))
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
    
    /// <summary>
    /// Заполнить свойство "Дата уничтожения оригинала".
    /// <description>1 января года следующего за датой окончания действия договорного документа + срок хранения (зафиксирован в категории)</description>
    /// </summary>
    public void ChangeDestructionDate()
    {
      _obj.DestructionDate = GetDestructionDate(_obj.ValidTill, _obj.DocumentGroup);
    }
    
    /// <summary>
    /// Возвращает дату уничтожения документа
    /// </summary>
    /// <param name="validTill">Действуе по.</param>
    /// <param name="category">Категория</param>
    /// <description>1 января года следующего за датой окончания действия договорного документа + срок хранения (зафиксирован в категории)</description>
    /// <returns>Дата уничтожения документа или null</returns>
    [Public]
    public static DateTime? GetDestructionDate(DateTime? validTill, Solution.IContractCategory category)
    {
      if (validTill.HasValue && category != null && category.FileRetentionPeriod != null && category.FileRetentionPeriod.RetentionPeriod.HasValue)
      {
        var retentionPeriod = category.FileRetentionPeriod.RetentionPeriod.Value;
        return validTill.Value.AddYears(retentionPeriod + 1).BeginningOfYear();
      }
      else
        return null;
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