using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalRule;
using Sungero.Domain.Shared;

namespace DirRX.Solution
{
  partial class ApprovalRuleServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      // Условие "Согласовано с рисками на предыдущем этапе" доступно только после этапа согласования.
      foreach (var condition in Functions.ApprovalRule.GetIncorrectConditions(_obj))
      {
        e.AddError(condition,
                   Sungero.Docflow.ApprovalRuleBases.Info.Properties.Conditions.Properties.Condition,
                   Solution.ApprovalRules.Resources.NeedApproversStageFormat(Solution.Conditions.Info.Properties.ConditionType.GetLocalizedValue(Solution.Condition.ConditionType.RiskPrevStage)));
      }
    }
  }

}