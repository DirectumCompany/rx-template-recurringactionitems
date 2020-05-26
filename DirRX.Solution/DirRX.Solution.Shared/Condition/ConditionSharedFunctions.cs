using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Condition;

namespace DirRX.Solution.Shared
{
  partial class ConditionFunctions
  {
    /// <summary>
    /// Добавить поддерживаемые типы условий.
    /// </summary>
    /// <returns>
    /// Словарь.
    /// Ключ - GUID типа документа.
    /// Значение - список поддерживаемых условий.
    /// </returns>
    public override System.Collections.Generic.Dictionary<string, List<Enumeration?>> GetSupportedConditions()
    {
      var baseConditions = base.GetSupportedConditions();
      
      var allTypes = Sungero.Docflow.PublicFunctions.DocumentKind.GetDocumentGuids(typeof(Sungero.Docflow.IOfficialDocument));
      foreach (var type in allTypes)
        baseConditions[type].AddRange(new List<Enumeration?> { DirRX.Solution.Condition.ConditionType.LUKOILApproval,
                                        DirRX.Solution.Condition.ConditionType.PaperSigning,
                                        DirRX.Solution.Condition.ConditionType.RiskPrevStage});
      
      baseConditions[DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.Order].Add(DirRX.Solution.Condition.ConditionType.WordMarksRegist);
      baseConditions[DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.RevisionRequest].Add(DirRX.Solution.Condition.ConditionType.NeedOriginals);
      
      return baseConditions;
    }
    
    /// <summary>
    /// Проверить условия.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>true если условие выполняется и false если не выполняется.</returns>
    public override Sungero.Docflow.Structures.ConditionBase.ConditionResult CheckCondition(Sungero.Docflow.IOfficialDocument document, Sungero.Docflow.IApprovalTask task)
    {
      var approvalTask = DirRX.Solution.ApprovalTasks.As(task);
      if (approvalTask != null)
      {
        if (_obj.ConditionType == ConditionType.LUKOILApproval)
        {
          var isLUKOILApproval = approvalTask.NeedLUKOILApproval.Value;
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(isLUKOILApproval, string.Empty);
        }
        
        if (_obj.ConditionType == ConditionType.PaperSigning)
        {
          var isPaperSigning = approvalTask.NeedPaperSigning.Value;
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(isPaperSigning, string.Empty);
        }
        
        if (_obj.ConditionType == ConditionType.WordMarksRegist)
        {
          
          var order = DirRX.Solution.Orders.As(document);
          if (order != null)
          {
            var isWordMarksRegistered = order.AllWordMarksRegistred.HasValue ? order.AllWordMarksRegistred.Value : false;
            return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(isWordMarksRegistered, string.Empty);
          }
          else
            return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(null, Conditions.Resources.WordMarksRegisteredWrongKind);
        }

        if (_obj.ConditionType == ConditionType.RiskPrevStage)
        {
          var withRisk = Solution.ApprovalTasks.As(task).LastStageWithRisk.GetValueOrDefault();
          return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(withRisk, string.Empty);
        }
        
        if (_obj.ConditionType == ConditionType.NeedOriginals)
        {
          var revisionRequest = PartiesControl.RevisionRequests.As(document);
          if (revisionRequest != null)
          {
            bool needOriginals = revisionRequest.BindingDocuments.Any(d => d.Format == PartiesControl.RevisionRequestBindingDocuments.Format.Original);
            return Sungero.Docflow.Structures.ConditionBase.ConditionResult.Create(needOriginals, string.Empty);
          }
        }
      }
      
      return base.CheckCondition(document, task);
    }
    
    public override void ChangePropertiesAccess()
    {
      base.ChangePropertiesAccess();
    }
    
    public override void ClearHiddenProperties()
    {
      base.ClearHiddenProperties();
    }

  }
}