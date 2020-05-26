using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ContractsApprovalRule;

namespace DirRX.Solution.Server
{
  partial class ContractsApprovalRuleFunctions
  {
    /// <summary>
    /// Получить доступные правила по документу.
    /// </summary>
    /// <param name="document">Документ для подбора правила.</param>
    /// <returns>Все правила, которые подходят к документу.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<Sungero.Docflow.IApprovalRuleBase> GetAvailableRulesByDocumentCustom(Sungero.Docflow.IOfficialDocument document)
    {
      var rules = Sungero.Docflow.PublicFunctions.ApprovalRuleBase.Remote.GetAvailableRulesByDocument(document);
      var contract = Contracts.As(document);
      if (contract != null)
      {
        // Функциональность договора.
        rules = contract.ContractFunctionality != null ?
          rules = rules.Where(r => ContractsApprovalRules.Is(r) &&
                              (ContractsApprovalRules.As(r).ContractFunctionality == contract.ContractFunctionality ||
                               ContractsApprovalRules.As(r).ContractFunctionality == null)):
          rules = rules.Where(r => ContractsApprovalRules.Is(r) && ContractsApprovalRules.As(r).ContractFunctionality == null);
        
        // Типовой договор.
        rules = contract.IsStandard == true ?
          rules = rules.Where(r => ContractsApprovalRules.Is(r) &&
                              (ContractsApprovalRules.As(r).IsStandard == IsStandard.Yes ||
                               ContractsApprovalRules.As(r).IsStandard == null)):
          rules = rules.Where(r => ContractsApprovalRules.Is(r) && ContractsApprovalRules.As(r).IsStandard == null);
        
        // Дополнительные условия договоров.
        rules = contract.Subcategory != null ?
          rules.Where(r =>  ContractsApprovalRules.Is(r) &&
                      ContractsApprovalRules.As(r).ConditionSubcategory.Any(o => Equals(o.Subcategory, contract.Subcategory)) ||
                      !ContractsApprovalRules.As(r).ConditionSubcategory.Any()) :
          rules.Where(r => ContractsApprovalRules.Is(r) && !ContractsApprovalRules.As(r).ConditionSubcategory.Any());
        
        // Срочность договоров.
        rules = contract.IsHighUrgency == true ?
          rules = rules.Where(r => ContractsApprovalRules.Is(r) &&
                              (ContractsApprovalRules.As(r).IsHighUrgency == IsHighUrgency.Yes ||
                               ContractsApprovalRules.As(r).IsHighUrgency == null)):
          rules = rules.Where(r => ContractsApprovalRules.Is(r) &&
                              (ContractsApprovalRules.As(r).IsHighUrgency == IsHighUrgency.No ||
                               ContractsApprovalRules.As(r).IsHighUrgency == null));
        
        // Этап тендера.
        rules = contract.TenderStep != null ?
          rules = rules.Where(r => ContractsApprovalRules.Is(r) &&
                              (ContractsApprovalRules.As(r).TenderStep == contract.TenderStep ||
                               ContractsApprovalRules.As(r).TenderStep == null)):
          rules = rules.Where(r => ContractsApprovalRules.Is(r) && ContractsApprovalRules.As(r).TenderStep == null);
      }
      return rules;
    }
    
    public override List<Sungero.Docflow.IApprovalRuleBase> GetDoubleRules()
    {
      var conflictedRules = new List<Sungero.Docflow.IApprovalRuleBase>();
      var allRules = base.GetDoubleRules();
      
      #region Дополнительные условия договоров.
      if (_obj.ConditionSubcategory.Any())
      {
        foreach (var subcategory in _obj.ConditionSubcategory)
        {
          conflictedRules.AddRange(allRules.Where(s => ContractsApprovalRules.Is(s) &&
                                                  ContractsApprovalRules.As(s).ConditionSubcategory.Any(o => o.Subcategory == subcategory.Subcategory)).ToList());
        }
      }
      else
      {
        conflictedRules.AddRange(allRules.Where(s => ContractsApprovalRules.Is(s) &&
                                                !ContractsApprovalRules.As(s).ConditionSubcategory.Any()).ToList());
      }
      #endregion
      
      allRules = conflictedRules.Distinct().ToList();
      conflictedRules.Clear();
      
      return allRules.Where(r => ContractsApprovalRules.Is(r) &&
                            ContractsApprovalRules.As(r).ContractFunctionality == _obj.ContractFunctionality &&
                            ContractsApprovalRules.As(r).IsStandard == _obj.IsStandard &&
                            ContractsApprovalRules.As(r).IsHighUrgency == _obj.IsHighUrgency &&
                            ContractsApprovalRules.As(r).TenderStep == _obj.TenderStep).ToList();
    }
  }
}