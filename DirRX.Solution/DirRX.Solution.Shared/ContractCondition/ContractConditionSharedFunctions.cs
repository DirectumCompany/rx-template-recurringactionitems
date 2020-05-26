using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ContractCondition;

namespace DirRX.Solution.Shared
{
  partial class ContractConditionFunctions
  {

    public override System.Collections.Generic.Dictionary<string, List<Enumeration?>> GetSupportedConditions()
    {
      // Привязать условие ко всем договорным документам.
      var baseSupport = base.GetSupportedConditions();
      var contracts = Sungero.Docflow.PublicFunctions.DocumentKind.GetDocumentGuids(typeof(Sungero.Contracts.IContractualDocument));
      foreach (var typeGuid in contracts.Concat(contracts))
      {
        baseSupport[typeGuid].Add(ConditionType.ContractStatus);
        // Условие Контрагент входит в Группу ЛУКОЙЛ только для договорных типов.
        baseSupport[typeGuid].Add(ConditionType.IsLlcGroup);
        baseSupport[typeGuid].Add(ConditionType.UrgentContract);
        baseSupport[typeGuid].Add(ConditionType.FirstSignCP);
        baseSupport[typeGuid].Add(ConditionType.CPSignsScan);
        baseSupport[typeGuid].Add(ConditionType.Subcategory);
        baseSupport[typeGuid].Add(ConditionType.Functionality);
        baseSupport[typeGuid].Add(ConditionType.DocsProvided);
        baseSupport[typeGuid].Add(ConditionType.TotalDeadline);
        baseSupport[typeGuid].Add(ConditionType.CorpApprove);
        baseSupport[typeGuid].Add(ConditionType.CopyActivation);
        baseSupport[typeGuid].Add(ConditionType.AnalysisRequire);
        baseSupport[typeGuid].Add(ConditionType.PrevStgRiskLvl);
        baseSupport[typeGuid].Add(ConditionType.RiskLvl);
        baseSupport[typeGuid].Add(ConditionType.IsCountryDisput);
        baseSupport[typeGuid].Add(ConditionType.IsTermlessContr);
        baseSupport[typeGuid].Add(ConditionType.SanctionCounter);
        baseSupport[typeGuid].Add(ConditionType.TZThird);
        baseSupport[typeGuid].Add(ConditionType.AdviseCountry);
        baseSupport[typeGuid].Add(ConditionType.LukoilApproved);
        baseSupport[typeGuid].Add(ConditionType.CurAmountIsMore);
        baseSupport[typeGuid].Add(ConditionType.BalancePercent);
        baseSupport[typeGuid].Add(ConditionType.TenderType);
      }
      return baseSupport;
    }

    public override void ChangePropertiesAccess()
    {
      base.ChangePropertiesAccess();
      var isCounterpartyStatus = _obj.ConditionType == ConditionType.ContractStatus;
      _obj.State.Properties.ConditionCounterpartyStatus.IsVisible = isCounterpartyStatus;
      _obj.State.Properties.ConditionCounterpartyStatus.IsRequired = isCounterpartyStatus;
      
      var isSubcategory = _obj.ConditionType == ConditionType.Subcategory;
      _obj.State.Properties.ConditionSubcategory.IsVisible = isSubcategory;
      _obj.State.Properties.ConditionSubcategory.IsRequired = isSubcategory;
      
      var isContractFunctionality = _obj.ConditionType == ConditionType.Functionality;
      _obj.State.Properties.ContractFunctionality.IsVisible = isContractFunctionality;
      _obj.State.Properties.ContractFunctionality.IsRequired = isContractFunctionality;
      
      var isTotalDeadline = _obj.ConditionType == ConditionType.TotalDeadline;
      _obj.State.Properties.YearCount.IsVisible = isTotalDeadline;
      _obj.State.Properties.YearCount.IsRequired = isTotalDeadline;
      _obj.State.Properties.YearCountOperator.IsVisible = isTotalDeadline;
      _obj.State.Properties.YearCountOperator.IsRequired = isTotalDeadline;
      
      var isPrevStgRiskLvl = _obj.ConditionType == ConditionType.PrevStgRiskLvl;
      _obj.State.Properties.RiskLevelCollectionDirRX.IsVisible = isPrevStgRiskLvl;
      _obj.State.Properties.RiskLevelCollectionDirRX.IsRequired = isPrevStgRiskLvl;
      
      var isRiskLvl = _obj.ConditionType == ConditionType.RiskLvl;
      _obj.State.Properties.RiskLevel.IsVisible = isRiskLvl;
      _obj.State.Properties.RiskLevel.IsRequired = isRiskLvl;
      
      var isCurAmountIsMore = _obj.ConditionType == ConditionType.CurAmountIsMore;
      _obj.State.Properties.Amount.IsVisible = _obj.State.Properties.Amount.IsVisible || isCurAmountIsMore;
      _obj.State.Properties.Amount.IsRequired = _obj.State.Properties.Amount.IsRequired || isCurAmountIsMore;
      _obj.State.Properties.AmountOperator.IsVisible = _obj.State.Properties.AmountOperator.IsVisible || isCurAmountIsMore;
      _obj.State.Properties.AmountOperator.IsRequired = _obj.State.Properties.AmountOperator.IsRequired || isCurAmountIsMore;
      _obj.State.Properties.Currency.IsVisible = isCurAmountIsMore;
      _obj.State.Properties.Currency.IsRequired = isCurAmountIsMore;
      
      var isBalancePercent = _obj.ConditionType == ConditionType.BalancePercent;
      _obj.State.Properties.BalancePercentage.IsVisible = isBalancePercent;
      _obj.State.Properties.BalancePercentage.IsRequired = isBalancePercent;
      
      var isTenderType = _obj.ConditionType == ConditionType.TenderType;
      _obj.State.Properties.TenderType.IsVisible = isTenderType;
      _obj.State.Properties.TenderType.IsRequired = isTenderType;
    }

    public override void ClearHiddenProperties()
    {
      base.ClearHiddenProperties();
      if (!_obj.State.Properties.ConditionCounterpartyStatus.IsVisible)
        _obj.ConditionCounterpartyStatus.Clear();

      if (!_obj.State.Properties.ConditionSubcategory.IsVisible)
        _obj.ConditionSubcategory.Clear();
      
      if (!_obj.State.Properties.ContractFunctionality.IsVisible)
        _obj.ContractFunctionality = null;
      
      if (!_obj.State.Properties.YearCount.IsVisible)
        _obj.YearCount = null;
      
      if (!_obj.State.Properties.YearCountOperator.IsVisible)
        _obj.YearCountOperator = null;
      
      if (!_obj.State.Properties.RiskLevel.IsVisible)
        _obj.RiskLevel = null;
      
      if (!_obj.State.Properties.RiskLevelCollectionDirRX.IsVisible)
        _obj.RiskLevelCollectionDirRX.Clear();
      
      if (!_obj.State.Properties.Currency.IsVisible)
        _obj.Currency = null;
      
      if (!_obj.State.Properties.BalancePercentage.IsVisible)
        _obj.BalancePercentage = null;
      
      if (!_obj.State.Properties.TenderType.IsVisible)
        _obj.TenderType = null;
    }
    
    public override Sungero.Docflow.Structures.ConditionBase.ConditionResult CheckCondition(Sungero.Docflow.IOfficialDocument document, Sungero.Docflow.IApprovalTask task)
    {
      #region Статус контрагента.
      if (_obj.ConditionType == ConditionType.ContractStatus)
      {
        var counterpartyStatuses = new List<DirRX.PartiesControl.ICounterpartyStatus>();
        
        var contract = DirRX.Solution.Contracts.As(document);
        if (contract != null)
          counterpartyStatuses.AddRange(contract.Counterparties.Select(c => c.Counterparty.CounterpartyStatus));
        
        var supAgreement = DirRX.Solution.SupAgreements.As(document);
        if (supAgreement != null)
          counterpartyStatuses.AddRange(supAgreement.Counterparties.Select(c => c.Counterparty.CounterpartyStatus));
        
        if (supAgreement == null && contract == null)
        {
          var contractualDoc = Sungero.Contracts.ContractualDocuments.As(document);
          if (contractualDoc != null)
            counterpartyStatuses.Add(Solution.Companies.As(contractualDoc.Counterparty).CounterpartyStatus);
          else
            return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
        }
        
        if (counterpartyStatuses.Any())
        {
          var possibleStatuses = _obj.ConditionCounterpartyStatus.Select(x => x.CounterpartyStatus);
          var notValidStatus = counterpartyStatuses.Any(s => !possibleStatuses.Contains(s));
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(!notValidStatus, string.Empty);
        }
        else
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidCounterpartyStatus);
      }
      #endregion
      #region Контрагент входит в Группу ЛУКОЙЛ.
      if (_obj.ConditionType == ConditionType.IsLlcGroup)
      {
        var contract = Contracts.As(document);
        if (contract != null)
          // Условие истинно, если во всех карточках Организаций установлен признак Организация Группы ЛУКОЙЛ.
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(
            !contract.Counterparties.Any(cp => DirRX.Solution.Companies.As(cp.Counterparty).IsLUKOILGroup != true), string.Empty);
        
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
          // Условие истинно, если во всех карточках Организаций установлен признак Организация Группы ЛУКОЙЛ.
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(
            !supAgreement.Counterparties.Any(cp => DirRX.Solution.Companies.As(cp.Counterparty).IsLUKOILGroup != true), string.Empty);
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Согласование срочное.
      if (_obj.ConditionType == ConditionType.UrgentContract)
      {
        var contract = Contracts.As(document);
        if (contract != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(contract.IsHighUrgency == true, string.Empty);
        
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(supAgreement.IsHighUrgency == true, string.Empty);
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Первым подписывает контрагент.
      if (_obj.ConditionType == ConditionType.FirstSignCP)
      {
        var contract = Contracts.As(document);
        if (contract != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(contract.IsContractorSignsFirst == true, string.Empty);
        
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(supAgreement.IsContractorSignsFirst == true, string.Empty);
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Контрагент подписывает скан-образ.
      if (_obj.ConditionType == ConditionType.CPSignsScan)
      {
        var contract = Contracts.As(document);
        if (contract != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(contract.IsScannedImageSign == true, string.Empty);
        
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(supAgreement.IsScannedImageSign == true, string.Empty);
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Подкатегория договора.
      if (_obj.ConditionType == ConditionType.Subcategory)
      {
        var contract = Contracts.As(document);
        if (contract != null)
        {
          var containSubcategory = _obj.ConditionSubcategory
            .Where(s => DirRX.ContractsCustom.ContractSubcategories.Equals(s.Subcategory, contract.Subcategory))
            .Any();
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(containSubcategory, string.Empty);
        }
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
        {
          var mainDoc = Contracts.As(supAgreement.LeadingDocument);
          if (mainDoc != null)
          {
            var containSubcategory = _obj.ConditionSubcategory
              .Any(s => DirRX.ContractsCustom.ContractSubcategories.Equals(s.Subcategory, mainDoc.Subcategory));
            return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(containSubcategory, string.Empty);
          }
        }
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Функциональность договора.
      if (_obj.ConditionType == ConditionType.Functionality)
      {
        var contract = Contracts.As(document);
        if (contract != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(_obj.ContractFunctionality == contract.ContractFunctionality, string.Empty);
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
        {
          var mainDoc = Contracts.As(supAgreement.LeadingDocument);
          if (mainDoc != null)
            return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(_obj.ContractFunctionality == mainDoc.ContractFunctionality, string.Empty);
        }
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Документы предоставлены.
      if (_obj.ConditionType == ConditionType.DocsProvided)
      {
        var contract = Contracts.As(document);
        if (contract != null)
          // Условие истинно, если во всех карточках Организаций установлен признак Документы представлены.
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(
            !contract.Counterparties.Any(cp => Companies.As(cp.Counterparty).IsDocumentsProvided != true), string.Empty);
        
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
          // Условие истинно, если во всех карточках Организаций установлен признак Документы представлены.
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(
            !supAgreement.Counterparties.Any(cp => Companies.As(cp.Counterparty).IsDocumentsProvided != true), string.Empty);
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Общий срок договора и дополнительного соглашения (лет).
      if (_obj.ConditionType == ConditionType.TotalDeadline)
      {
        var contract = Solution.Contracts.As(document);
        var monthCount = _obj.YearCount.Value * 12;
        if (contract != null)
        {
          if (!contract.ValidFrom.HasValue && !contract.ValidTill.HasValue && contract.DocumentValidity.HasValue)
            return GetTotalDeadlineCondition(monthCount, contract.DocumentValidity.Value, contract.IsTermless == true);
          
          return GetTotalDeadlineCondition(
            contract.ValidFrom.HasValue ? contract.ValidFrom.Value.AddYears(_obj.YearCount.Value) : Calendar.SqlMinValue,
            contract.ValidTill.HasValue ? contract.ValidTill.Value : Calendar.SqlMaxValue,
            contract.IsTermless == true);
        }

        var supAgreement = Solution.SupAgreements.As(document);
        if (supAgreement != null)
        {
          if (!supAgreement.ValidFrom.HasValue && !supAgreement.ValidTill.HasValue && supAgreement.DocumentValidity.HasValue)
            return GetTotalDeadlineCondition(monthCount, supAgreement.DocumentValidity.Value, Contracts.As(supAgreement.LeadingDocument).IsTermless == true);
          
          return GetTotalDeadlineCondition(
            supAgreement.LeadingDocument.ValidFrom.HasValue ? supAgreement.LeadingDocument.ValidFrom.Value.AddYears(_obj.YearCount.Value) : Calendar.SqlMinValue,
            supAgreement.ValidTill.HasValue ? supAgreement.ValidTill.Value : Calendar.SqlMaxValue,
            Contracts.Is(supAgreement.LeadingDocument) && Contracts.As(supAgreement.LeadingDocument).IsTermless == true);
        }

        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Требуется корпоративное одобрение.
      if (_obj.ConditionType == ConditionType.CorpApprove)
      {
        var contract = Contracts.As(document);
        if (contract != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(contract.IsCorporateApprovalRequired == true, string.Empty);
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(supAgreement.IsCorporateApprovalRequired == true, string.Empty);
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Активация по сканам.
      if (_obj.ConditionType == ConditionType.CopyActivation)
      {
        var contract = Contracts.As(document);
        if (contract != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(contract.ContractActivate == DirRX.Solution.Contract.ContractActivate.Copy, string.Empty);
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(supAgreement.ContractActivate == DirRX.Solution.SupAgreement.ContractActivate.Copy, string.Empty);
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Требуется анализ на признак МСФО 16.
      if (_obj.ConditionType == ConditionType.AnalysisRequire)
      {
        var contract = Contracts.As(document);
        if (contract != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(contract.IsAnalysisRequired == true, string.Empty);
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(supAgreement.IsAnalysisRequired == true, string.Empty);
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Согласовано с рисками на предыдущем этапе.
      if (_obj.ConditionType == ConditionType.PrevStgRiskLvl && _obj.RiskLevelCollectionDirRX.Any())
      {
        var solutionTask = Solution.ApprovalTasks.As(task);
        if (solutionTask != null)
        {
          var withRisk = solutionTask.LastStageWithRisk.GetValueOrDefault();
          if (withRisk)
          {
            var withRiskLevel = solutionTask.RiskAttachmentGroup.Risks.Any(r => r.Status == Sungero.CoreEntities.DatabookEntry.Status.Active &&
                                                                           _obj.RiskLevelCollectionDirRX.Any(x => DirRX.LocalActs.RiskLevels.Equals(r.Level, x.RiskLevel)));
            return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(withRiskLevel, string.Empty);
          }
          else
            return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(false, string.Empty);
        }
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Согласовано с рисками по ходу согласования.
      if (_obj.ConditionType == ConditionType.RiskLvl && _obj.RiskLevel != null)
      {
        var solutionTask = Solution.ApprovalTasks.As(task);
        if (solutionTask != null)
        {
          // Согласовано с рисками, если есть хоть одно пересечение рисков.
          var withRiskLevel = solutionTask.RiskAttachmentGroup.Risks.Any(r => r.Status == Sungero.CoreEntities.DatabookEntry.Status.Active &&
                                                                         DirRX.LocalActs.RiskLevels.Equals(r.Level, _obj.RiskLevel));
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(withRiskLevel, string.Empty);
        }
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Страна назначения/поставки входит в перечень спорных территорий.
      if (_obj.ConditionType == ConditionType.IsCountryDisput)
      {
        var contract = Contracts.As(document);
        if (contract != null && contract.ContractFunctionality == DirRX.Solution.Contract.ContractFunctionality.Sale)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(contract.DestinationCountries.Any(d => d.DestinationCountry.IsIncludedInDisputedTerritories == true),
                                                                                 string.Empty);
        
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
        {
          // Проверить функциональность договора-основания.
          var mainDoc = Contracts.As(supAgreement.LeadingDocument);
          if (mainDoc != null && mainDoc.ContractFunctionality == DirRX.Solution.Contract.ContractFunctionality.Sale)
            return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(supAgreement.DestinationCountries.Any(d => d.DestinationCountry.IsIncludedInDisputedTerritories == true),
                                                                                   string.Empty);
          else
            return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.InvalidLeadingDocument);
        }
      }
      #endregion
      #region Договор бессрочный.
      if (_obj.ConditionType == ConditionType.IsTermlessContr)
      {
        var contract = Contracts.As(document);
        if (contract != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(contract.IsTermless == true, string.Empty);
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
        {
          var mainDoc = Contracts.As(supAgreement.LeadingDocument);
          if (mainDoc != null)
            return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(mainDoc.IsTermless == true, string.Empty);
        }
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Контрагент под санкциями.
      if (_obj.ConditionType == ConditionType.SanctionCounter)
      {
        var contract = Contracts.As(document);
        if (contract != null)
          // Условие истинно, если хотя бы в одной карточке Организации установлен признак Санкции.
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(contract.Counterparties.Any(
            cp => Solution.Companies.As(cp.Counterparty).IsSanctions == true), string.Empty);
        
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
          // Условие истинно, если хотя бы в одной карточке Организации установлен признак Санкции.
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(supAgreement.Counterparties.Any(
            cp => Solution.Companies.As(cp.Counterparty).IsSanctions == true), string.Empty);
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Правообладатель ТЗ 3-е лицо.
      if (_obj.ConditionType == ConditionType.TZThird)
      {
        var contract = Contracts.As(document);
        if (contract != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(contract.HolderTZ.HasValue && contract.HolderTZ == Solution.Contract.HolderTZ.Third, string.Empty);
        
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(supAgreement.HolderTZ.HasValue && supAgreement.HolderTZ == Solution.SupAgreement.HolderTZ.Third, string.Empty);
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Страны поставки входят в перечень рекомендуемых.
      if (_obj.ConditionType == ConditionType.AdviseCountry)
      {
        var contract = Contracts.As(document);
        if (contract != null)
        {
          var isAdviseCountry = !contract.DestinationCountries.Any() ||
            // Не установлен признак "Фирменная продукция".
            contract.BrandedProducts != true ||
            // Все страны у документа должны иметь в качестве рекомендуемой организации - нашу организацию из документа.
            !contract.DestinationCountries.Any(x => !x.DestinationCountry.RecommendDeliveryForCompanies.Any(y => BusinessUnits.Equals(y.Organization, contract.BusinessUnit)));
          
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(isAdviseCountry, string.Empty);
        }
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
        {
          var isAdviseCountry = !supAgreement.DestinationCountries.Any() ||
            // Не установлен признак "Фирменная продукция".
            supAgreement.BrandedProducts != true ||
            // Все страны у документа должны иметь в качестве рекомендуемой организации - нашу организацию из документа.
            !supAgreement.DestinationCountries.Any(x => !x.DestinationCountry.RecommendDeliveryForCompanies.Any(y => BusinessUnits.Equals(y.Organization, supAgreement.BusinessUnit)));
          
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(isAdviseCountry, string.Empty);
        }
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Требуется согласование ПАО «ЛУКОЙЛ».
      if (_obj.ConditionType == ConditionType.LukoilApproved)
      {
        var contract = Contracts.As(document);
        if (contract != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(contract.LukoilApproving == DirRX.Solution.Contract.LukoilApproving.Required, string.Empty);
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(supAgreement.LukoilApproving == DirRX.Solution.SupAgreement.LukoilApproving.Required, string.Empty);
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Сумма в валюте.
      if (_obj.ConditionType == ConditionType.CurAmountIsMore)
      {
        var contract = Contracts.As(document);
        if (contract != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(CheckCurrencyAmount(contract.TransactionAmount.Value, contract.Currency), string.Empty);
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
        {
          var leadDoc = Contracts.As(supAgreement.LeadingDocument);
          var amount = leadDoc.IsFrameContract == true ? leadDoc.TransactionAmount.Value : Solution.PublicFunctions.SupAgreement.Remote.GetContractTotalAmount(supAgreement);
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(CheckCurrencyAmount(amount, supAgreement.Currency), string.Empty);
        }
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region % балансовой стоимости активов
      if (_obj.ConditionType == ConditionType.BalancePercent)
      {
        var contract = Contracts.As(document);
        if (contract != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(CheckBalancePercentage(contract.TransactionAmount.Value, contract.Currency), string.Empty);
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
        {
          var leadDoc = Contracts.As(supAgreement.LeadingDocument);
          var amount = leadDoc.IsFrameContract == true ? leadDoc.TransactionAmount.Value : Solution.PublicFunctions.SupAgreement.Remote.GetContractTotalAmount(supAgreement);
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(CheckBalancePercentage(amount, supAgreement.Currency), string.Empty);
        }
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Тип тендера.
      if (_obj.ConditionType == ConditionType.TenderType)
      {
        var contract = Contracts.As(document);
        if (contract != null)
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(_obj.TenderType == contract.TenderType, string.Empty);
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
        {
          var mainDoc = Contracts.As(supAgreement.LeadingDocument);
          if (mainDoc != null)
            return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(_obj.TenderType == mainDoc.TenderType, string.Empty);
        }
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      #region Контрагент-нерезидент
      if (_obj.ConditionType == ConditionType.Nonresident)
      {
        var contract = Contracts.As(document);
        if (contract != null)
          // Условие истинно, если хотя бы в одной карточке Организации установлен признак Нерезидент.
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(contract.Counterparties.Any(
            cp => cp.Counterparty.Nonresident == true), string.Empty);
        
        var supAgreement = SupAgreements.As(document);
        if (supAgreement != null)
          // Условие истинно, если хотя бы в одной карточке Организации установлен признак Нерезидент.
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(supAgreement.Counterparties.Any(
            cp => cp.Counterparty.Nonresident == true), string.Empty);
        
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
      }
      #endregion
      return base.CheckCondition(document, task);
    }

    /// <summary>
    /// Вычислить условие срока действия договорного документа.
    /// </summary>
    /// <param name="validFromAddYears">Дата начала + срок из условия.</param>
    /// <param name="validTill">Дата окончания.</param>
    /// <param name="isTermless">Бессрочный.</param>
    /// <returns>Условие.</returns>
    private Sungero.Docflow.Structures.ConditionBase.ConditionResult GetTotalDeadlineCondition(DateTime validFromAddYears, DateTime validTill, bool isTermless)
    {
      if (isTermless)
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(true, string.Empty);
      if (_obj.YearCountOperator == YearCountOperator.GreaterThan)
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(validFromAddYears < validTill, string.Empty);
      if (_obj.YearCountOperator == YearCountOperator.GreaterOrEqual)
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(validFromAddYears <= validTill, string.Empty);
      if (_obj.YearCountOperator == YearCountOperator.LessThan)
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(validFromAddYears > validTill, string.Empty);
      if (_obj.YearCountOperator == YearCountOperator.LessOrEqual)
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(validFromAddYears >= validTill, string.Empty);
      
      return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
    }
    
    /// <summary>
    /// Вычислить условие срока действия договорного документа.
    /// </summary>
    /// <param name="validFromAddYears">Срок документа в условии.</param>
    /// <param name="validTill">Срок действия документа.</param>
    /// <param name="isTermless">Бессрочный.</param>
    /// <returns>Условие.</returns>
    private Sungero.Docflow.Structures.ConditionBase.ConditionResult GetTotalDeadlineCondition(int validFromAddYears, int validTill, bool isTermless)
    {
      if (isTermless)
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(true, string.Empty);
      if (_obj.YearCountOperator == YearCountOperator.GreaterThan)
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(validFromAddYears < validTill, string.Empty);
      if (_obj.YearCountOperator == YearCountOperator.GreaterOrEqual)
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(validFromAddYears <= validTill, string.Empty);
      if (_obj.YearCountOperator == YearCountOperator.LessThan)
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(validFromAddYears > validTill, string.Empty);
      if (_obj.YearCountOperator == YearCountOperator.LessOrEqual)
        return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(validFromAddYears >= validTill, string.Empty);
      
      return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, DirRX.Solution.ContractConditions.Resources.IsNotValidKindDocument);
    }
    
    /// <summary>
    /// Проверить сумму согласуемого документа в валюте.
    /// </summary>
    /// <param name="documentAmount">Сумма.</param>
    /// <param name="currency">Валюта.</param>
    /// <returns>Условие.</returns>
    private bool CheckCurrencyAmount(double amount, Sungero.Commons.ICurrency currency)
    {
      if (currency == null)
        currency = Sungero.Commons.PublicFunctions.Currency.Remote.GetDefaultCurrency();
      
      var conditionAmount = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetSummInRUB(_obj.Amount.Value, _obj.Currency);
      var documentAmount = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetSummInRUB(amount, currency);
      
      if (_obj.AmountOperator == AmountOperator.GreaterThan)
        return documentAmount > conditionAmount;
      if (_obj.AmountOperator == AmountOperator.GreaterOrEqual)
        return documentAmount >= conditionAmount;
      if (_obj.AmountOperator == AmountOperator.LessThan)
        return documentAmount < conditionAmount;
      if (_obj.AmountOperator == AmountOperator.LessOrEqual)
        return documentAmount <= conditionAmount;
      
      return false;
    }
    
    /// <summary>
    /// Проверить условие сумма больше % балансовой стоимости активов.
    /// </summary>
    /// <param name="documentAmount">Сумма.</param>
    /// <param name="currency">Валюта.</param>
    /// <returns>Условие.</returns>
    private bool CheckBalancePercentage(double amount, Sungero.Commons.ICurrency currency)
    {
      var constant = ContractsCustom.PublicFunctions.Module.Remote.GetContractConstant(ContractsCustom.PublicConstants.Module.BookValueAssetsGuid.ToString());
      
      if (constant == null || !constant.Amount.HasValue || constant.Currency == null || currency == null)
        return false;
      
      var constAmount = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetSummInRUB(constant.Amount.Value, constant.Currency);
      var documentAmount = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetSummInRUB(amount, currency);
      var percent = _obj.BalancePercentage / 100;
      
      return documentAmount >= constAmount * percent;
    }
  }
}