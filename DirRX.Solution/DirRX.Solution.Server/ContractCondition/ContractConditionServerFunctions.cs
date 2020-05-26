using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ContractCondition;

namespace DirRX.Solution.Server
{
  partial class ContractConditionFunctions
  {
    public override string GetConditionName()
    {
      using (TenantInfo.Culture.SwitchTo())
      {
        if (_obj.ConditionType == ConditionType.ContractStatus)
        {
          var statusList = _obj.ConditionCounterpartyStatus.Select(x => x.CounterpartyStatus).ToList();
          var status = string.Join(", ", statusList);
          return ContractConditions.Resources.CounterpartyStatusIsFormat(status);
        }
        if (_obj.ConditionType == ConditionType.IsLlcGroup)
          return ContractConditions.Resources.IsLlcGroupConditionName;
        if (_obj.ConditionType == ConditionType.UrgentContract)
          return ContractConditions.Resources.UrgentContractConditionName;
        if (_obj.ConditionType == ConditionType.FirstSignCP)
          return ContractConditions.Resources.FirstSignCPartyConditionName;
        if (_obj.ConditionType == ConditionType.CPSignsScan)
          return ContractConditions.Resources.CPartySignsScanConditionName;
        if (_obj.ConditionType == ConditionType.Subcategory)
          return ContractConditions.Resources.SubcategoryConditionNameFormat(string.Join(", ", _obj.ConditionSubcategory.Select(s => s.Subcategory).ToList()));
        if (_obj.ConditionType == ConditionType.Functionality)
          return ContractConditions.Resources.FunctionalityConditionNameFormat(_obj.Info.Properties.ContractFunctionality.GetLocalizedValue(_obj.ContractFunctionality));
        if (_obj.ConditionType == ConditionType.DocsProvided)
          return ContractConditions.Resources.DocsProvidedConditionName;
        if (_obj.ConditionType == ConditionType.TotalDeadline)
        {
          if (_obj.YearCountOperator == YearCountOperator.GreaterOrEqual)
            return _obj.YearCount == 1 ? ContractConditions.Resources.YearCountGreaterOrEqualSingleNameFormat(_obj.YearCount) : ContractConditions.Resources.YearCountGreaterOrEqualNameFormat(_obj.YearCount);
          if (_obj.YearCountOperator == YearCountOperator.GreaterThan)
            return _obj.YearCount == 1 ? ContractConditions.Resources.YearCountGreaterThanSingleNameFormat(_obj.YearCount) : ContractConditions.Resources.YearCountGreaterThanNameFormat(_obj.YearCount);
          if (_obj.YearCountOperator == YearCountOperator.LessThan)
            return _obj.YearCount == 1 ? ContractConditions.Resources.YearCountLessThanSingleNameFormat(_obj.YearCount) : ContractConditions.Resources.YearCountLessThanNameFormat(_obj.YearCount);
          if (_obj.YearCountOperator == YearCountOperator.LessOrEqual)
            return _obj.YearCount == 1 ? ContractConditions.Resources.YearCountLessOrEqualSingleNameFormat(_obj.YearCount) : ContractConditions.Resources.YearCountLessOrEqualNameFormat(_obj.YearCount);
        }
        if (_obj.ConditionType == ConditionType.CorpApprove)
          return ContractConditions.Resources.CorpApproveName;
        if (_obj.ConditionType == ConditionType.CopyActivation)
          return ContractConditions.Resources.CopyActivationName;
        if (_obj.ConditionType == ConditionType.AnalysisRequire)
          return ContractConditions.Resources.AnalysisRequiredName;
        if (_obj.ConditionType == ConditionType.PrevStgRiskLvl)
        {
          if (_obj.RiskLevelCollectionDirRX.Count > 1)
            return DirRX.Solution.ContractConditions.Resources.PrevStgRisksLvlNameFormat(string.Join(", ", _obj.RiskLevelCollectionDirRX.Select(x => x.RiskLevel.DisplayValue).ToArray()));
          else if (_obj.RiskLevelCollectionDirRX.Count == 1)
            return DirRX.Solution.Conditions.Resources.PrevStgRiskLvlNameFormat(_obj.RiskLevelCollectionDirRX.Select(x => x.RiskLevel.DisplayValue).FirstOrDefault().ToString());
        }
        if (_obj.ConditionType == ConditionType.RiskLvl)
          return DirRX.Solution.Conditions.Resources.RiskLvlNameFormat(_obj.RiskLevel);
        if (_obj.ConditionType == ConditionType.IsCountryDisput)
          return DirRX.Solution.ContractConditions.Resources.IsCountryDisputedConditionName;
        if (_obj.ConditionType == ConditionType.IsTermlessContr)
          return DirRX.Solution.ContractConditions.Resources.IsTermlessContrConditionName;
        if (_obj.ConditionType == ConditionType.SanctionCounter)
          return DirRX.Solution.ContractConditions.Resources.CounterpartySanctionsConditionName;
        if (_obj.ConditionType == ConditionType.TZThird)
          return DirRX.Solution.ContractConditions.Resources.HolderTZThirdConditionName;
        if (_obj.ConditionType == ConditionType.AdviseCountry)
          return DirRX.Solution.ContractConditions.Resources.AdviseCountryConditionName;
        if (_obj.ConditionType == ConditionType.LukoilApproved)
          return DirRX.Solution.ContractConditions.Resources.LukoilApprovedConditionName;
        if (_obj.ConditionType == ConditionType.CurAmountIsMore)
          return DirRX.Solution.ContractConditions.Resources.CurAmountIsMoreConditionNameFormat(_obj.Info.Properties.AmountOperator.GetLocalizedValue(_obj.AmountOperator),
                                                                                                Sungero.Docflow.PublicFunctions.ConditionBase.AmountFormat(_obj.Amount),
                                                                                                _obj.Currency.AlphaCode);
        if (_obj.ConditionType == ConditionType.BalancePercent)
          return DirRX.Solution.ContractConditions.Resources.BalancePercentConditionNameFormat(_obj.BalancePercentage.ToString());
				
				if (_obj.ConditionType == ConditionType.TenderType)
					return DirRX.Solution.ContractConditions.Resources.TenderTypeConditionNameFormat(_obj.Info.Properties.TenderType.GetLocalizedValue(_obj.TenderType));
      }
      return base.GetConditionName();
    }
    
    public override string GetConditionNegationName()
    {
      using (TenantInfo.Culture.SwitchTo())
      {
        if (_obj.ConditionType == ConditionType.ContractStatus)
          return ContractConditions.Resources.CounterpartyStatusOther;
        if (_obj.ConditionType == ConditionType.IsLlcGroup)
          return ContractConditions.Resources.IsLlcGroupConditionNegationName;
        if (_obj.ConditionType == ConditionType.UrgentContract)
          return ContractConditions.Resources.UrgentContractConditionNegationName;
        if (_obj.ConditionType == ConditionType.FirstSignCP)
          return ContractConditions.Resources.FirstSignCPartyConditionNegationName;
        if (_obj.ConditionType == ConditionType.CPSignsScan)
          return ContractConditions.Resources.CPartySignsScanConditionNegationName;
        if (_obj.ConditionType == ConditionType.Subcategory)
          return ContractConditions.Resources.SubcategoryConditionNegationNameFormat(string.Join(", ", _obj.ConditionSubcategory.Select(s => s.Subcategory).ToList()));
        if (_obj.ConditionType == ConditionType.Functionality)
          return ContractConditions.Resources.FunctionalityConditionNegationNameFormat(_obj.Info.Properties.ContractFunctionality.GetLocalizedValue(_obj.ContractFunctionality));
        if (_obj.ConditionType == ConditionType.DocsProvided)
          return ContractConditions.Resources.DocsProvidedConditionNegationName;
        if (_obj.ConditionType == ConditionType.TotalDeadline)
        {
          if (_obj.YearCountOperator == YearCountOperator.GreaterOrEqual)
            return _obj.YearCount == 1 ? ContractConditions.Resources.YearCountGreaterOrEqualSingleNegNameFormat(_obj.YearCount) : ContractConditions.Resources.YearCountGreaterOrEqualNegNameFormat(_obj.YearCount);
          if (_obj.YearCountOperator == YearCountOperator.GreaterThan)
            return _obj.YearCount == 1 ? ContractConditions.Resources.YearCountGreaterThanSingleNegNameFormat(_obj.YearCount) : ContractConditions.Resources.YearCountGreaterThanNegNameFormat(_obj.YearCount);
          if (_obj.YearCountOperator == YearCountOperator.LessThan)
            return _obj.YearCount == 1 ? ContractConditions.Resources.YearCountLessThanSingleNegNameFormat(_obj.YearCount) : ContractConditions.Resources.YearCountLessThanNegNameFormat(_obj.YearCount);
          if (_obj.YearCountOperator == YearCountOperator.LessOrEqual)
            return _obj.YearCount == 1 ? ContractConditions.Resources.YearCountLessOrEqualSingleNegNameFormat(_obj.YearCount) : ContractConditions.Resources.YearCountLessOrEqualNegNameFormat(_obj.YearCount);
        }
        if (_obj.ConditionType == ConditionType.CorpApprove)
          return ContractConditions.Resources.CorpApproveNegName;
        if (_obj.ConditionType == ConditionType.CopyActivation)
          return ContractConditions.Resources.CopyActivationNegName;
        if (_obj.ConditionType == ConditionType.AnalysisRequire)
          return ContractConditions.Resources.AnalysisRequiredNegName;
        if (_obj.ConditionType == ConditionType.PrevStgRiskLvl)
        {
          if (_obj.RiskLevelCollectionDirRX.Count > 1)
            return DirRX.Solution.ContractConditions.Resources.PrevStgRisksLvlNegNameFormat(string.Join(", ", _obj.RiskLevelCollectionDirRX.Select(x => x.RiskLevel.DisplayValue).ToArray()));
          else if (_obj.RiskLevelCollectionDirRX.Count == 1)
            return DirRX.Solution.Conditions.Resources.PrevStgRiskLvlNegNameFormat(_obj.RiskLevelCollectionDirRX.Select(x => x.RiskLevel.DisplayValue).FirstOrDefault().ToString());
        }
        if (_obj.ConditionType == ConditionType.RiskLvl && _obj.RiskLevel != null)
          return DirRX.Solution.Conditions.Resources.RiskLvlNegNameFormat(_obj.RiskLevel.DisplayValue);
        if (_obj.ConditionType == ConditionType.IsCountryDisput)
          return DirRX.Solution.ContractConditions.Resources.IsCountryDisputedNegationName;
        if (_obj.ConditionType == ConditionType.IsTermlessContr)
          return DirRX.Solution.ContractConditions.Resources.IsTermlessContrConditionNegationName;
        if (_obj.ConditionType == ConditionType.SanctionCounter)
          return DirRX.Solution.ContractConditions.Resources.CounterpartySanctionsNegationName;
        if (_obj.ConditionType == ConditionType.TZThird)
          return DirRX.Solution.ContractConditions.Resources.HolderTZThirdNegationName;
        if (_obj.ConditionType == ConditionType.AdviseCountry)
          return DirRX.Solution.ContractConditions.Resources.AdviseCountryNegationName;
        if (_obj.ConditionType == ConditionType.LukoilApproved)
          return DirRX.Solution.ContractConditions.Resources.LukoilApprovedNegationName;
        if (_obj.ConditionType == ConditionType.CurAmountIsMore)
          return DirRX.Solution.ContractConditions.Resources.CurAmountIsMoreConditionNegationNameFormat(_obj.Info.Properties.AmountOperator.GetLocalizedValue(_obj.AmountOperator),
                                                                                                        Sungero.Docflow.PublicFunctions.ConditionBase.AmountFormat(_obj.Amount),
                                                                                                        _obj.Currency.AlphaCode);
        if (_obj.ConditionType == ConditionType.BalancePercent)
          return DirRX.Solution.ContractConditions.Resources.BalancePercentConditionNegationNameFormat(_obj.BalancePercentage.ToString());
				
				if (_obj.ConditionType == ConditionType.TenderType)
					return DirRX.Solution.ContractConditions.Resources.TenderTypeConditionNegationNameFormat(_obj.Info.Properties.TenderType.GetLocalizedValue(_obj.TenderType));
      }
      return base.GetConditionNegationName();
    }
  }
}