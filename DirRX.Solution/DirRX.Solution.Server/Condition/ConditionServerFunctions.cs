using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Condition;

namespace DirRX.Solution.Server
{
  partial class ConditionFunctions
  {
    /// <summary>
    /// Получить текст условия.
    /// </summary>
    /// <returns>Текст условия.</returns>
    public override string GetConditionName()
    {
      using (TenantInfo.Culture.SwitchTo())
      {
        if (_obj.ConditionType == ConditionType.LUKOILApproval)
          return DirRX.Solution.Conditions.Resources.NeedLUKOILApproval;

        if (_obj.ConditionType == ConditionType.PaperSigning)
          return DirRX.Solution.Conditions.Resources.NeedPaperSigning;

        if (_obj.ConditionType == ConditionType.RiskPrevStage)
          return DirRX.Solution.Conditions.Resources.WithRiskInPreviousStage;

        if (_obj.ConditionType == ConditionType.WordMarksRegist)
          return DirRX.Solution.Conditions.Resources.WordMarksRegistered;
        
        if (_obj.ConditionType == ConditionType.NeedOriginals)
          return DirRX.Solution.Conditions.Resources.NeedOriginals;
      }
      
      return base.GetConditionName();
    }
    
    /// <summary>
    /// Получить текст условия с отрицанием.
    /// </summary>
    /// <returns>Текст условия.</returns>
    public override string GetConditionNegationName()
    {
      using (TenantInfo.Culture.SwitchTo())
      {
        if (_obj.ConditionType == ConditionType.LUKOILApproval)
          return DirRX.Solution.Conditions.Resources.DoNotNeedLUKOILApproval;

        if (_obj.ConditionType == ConditionType.PaperSigning)
          return DirRX.Solution.Conditions.Resources.DoNotNeedPaperSigning;
        
        if (_obj.ConditionType == ConditionType.RiskPrevStage)
          return DirRX.Solution.Conditions.Resources.WithoutRiskInPreviousStage;
        
        if (_obj.ConditionType == ConditionType.WordMarksRegist)
          return DirRX.Solution.Conditions.Resources.DoNotWordMarksRegistered;
        
        if (_obj.ConditionType == ConditionType.NeedOriginals)
          return DirRX.Solution.Conditions.Resources.DoNotNeedOriginals;
      }
      
      return base.GetConditionNegationName();
    }
  }
}