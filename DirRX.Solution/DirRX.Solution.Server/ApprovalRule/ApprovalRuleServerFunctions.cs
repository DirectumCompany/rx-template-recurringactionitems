using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalRule;

namespace DirRX.Solution.Server
{
  partial class ApprovalRuleFunctions
  {
    /// <summary>
    /// Проверка наличия этапов согласования для условия "Согласовано с рисками на предыдущем этапе".
    /// </summary>
    /// <returns>Список условий для которых предыдущий этап не этап согласования.</returns>
    public List<Sungero.Docflow.IApprovalRuleBaseConditions> GetIncorrectConditions()
    {
      var stagesVariants = base.GetAllStagesVariants();
      var conditions = new List<Sungero.Docflow.IApprovalRuleBaseConditions>() { };
      
      foreach (var stepsNumbers in stagesVariants.AllSteps)
      {
        foreach (var number in stepsNumbers)
        {
          var condition = _obj.Conditions.FirstOrDefault(c => c.Number == number && c.Condition.ConditionType == Solution.Condition.ConditionType.RiskPrevStage);
          if (condition != null)
          {
            var previousStageIndex = stepsNumbers.IndexOf(number) - 1;
            if (previousStageIndex > -1)
            {
              var stage = _obj.Stages.FirstOrDefault(s => s.Number == stepsNumbers[previousStageIndex] && s.Stage.StageType == Solution.ApprovalStage.StageType.Approvers);
              
              if (stage == null && !conditions.Contains(condition))
                conditions.Add(condition);
            }
            else
            {
              if (!conditions.Contains(condition))
                conditions.Add(condition);
            }
          }
        }
      }
      
      return conditions;
    }
  }
}