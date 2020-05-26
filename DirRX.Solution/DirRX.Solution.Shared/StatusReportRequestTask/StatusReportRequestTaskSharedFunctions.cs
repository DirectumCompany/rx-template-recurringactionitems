using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.StatusReportRequestTask;

namespace DirRX.Solution.Shared
{
  partial class StatusReportRequestTaskFunctions
  {
  	#region Из коробки.
    /// <summary>
    /// Получить тему запроса отчета.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="beginningSubject">Начальная тема.</param>
    /// <returns>Сформированная тема.</returns>
    public static string GetStatusReportRequestSubjectDir(Sungero.RecordManagement.IStatusReportRequestTask task, CommonLibrary.LocalizedString beginningSubject)
    {
      var actionItemExecution = ActionItemExecutionTasks.As(task.ParentTask) ?? ActionItemExecutionTasks.As(task.ParentAssignment.Task);
      if (actionItemExecution.IsCompoundActionItem.Value && task.Assignee != null)
      {
        var assignment = Functions.ActionItemExecutionTask.Remote.GetActionItemsDir(actionItemExecution)
          .Where(j => Equals(j.Performer, task.Assignee))
          .Where(a => ActionItemExecutionTasks.Is(a.Task))
          .First();
        actionItemExecution = ActionItemExecutionTasks.As(assignment.Task);
      }
      var subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(actionItemExecution, beginningSubject);
      
      return Sungero.Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
    }    
    #endregion
    
    /// <summary>
    /// Получить тему запроса отчета по отдельного поручению в составе составного.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="perforfer">Исполнитель.</param>
    /// <param name="beginningSubject">Начальная тема.</param>
    /// <returns>Сформированная тема.</returns>
    public static string GetStatusReportAssignmentSubject(Sungero.RecordManagement.IStatusReportRequestTask task, Sungero.CoreEntities.IRecipient performer, CommonLibrary.LocalizedString beginningSubject)
    {
      var actionItemExecution = ActionItemExecutionTasks.As(task.ParentTask) ?? ActionItemExecutionTasks.As(task.ParentAssignment.Task);
      if (actionItemExecution.IsCompoundActionItem.Value)
      {
        var assignment = Functions.ActionItemExecutionTask.Remote.GetActionItemsDir(actionItemExecution)
          .Where(j => Equals(j.Performer, performer))
          .Where(a => ActionItemExecutionTasks.Is(a.Task))
          .First();
        actionItemExecution = ActionItemExecutionTasks.As(assignment.Task);
      }
      var subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(actionItemExecution, beginningSubject);
      
      return Sungero.Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
    }    
  }
}