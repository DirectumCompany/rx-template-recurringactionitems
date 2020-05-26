using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ActionItemExecutionTask;

namespace DirRX.Solution.Server
{
  partial class ActionItemExecutionTaskFunctions
  {
    [Public, Remote(IsPure = true)]
    public static DirRX.Solution.IActionItemExecutionTask GetActionItemExecutionTask(int id)
    {
      return ActionItemExecutionTasks.Get(id);
    }
    
    #region Из базовой. Убрано автозаполнение срока из задания.
    
    /// <summary>
    /// Создать поручение из открытого задания.
    /// </summary>
    /// <param name="actionItemAssignment">Задание.</param>
    /// <returns>Поручение.</returns>
    [Remote(PackResultEntityEagerly = true)]
    public static Sungero.RecordManagement.IActionItemExecutionTask CreateActionItemExecutionFromExecutionDir(Sungero.RecordManagement.IActionItemExecutionAssignment actionItemAssignment)
    {
      var task = DirRX.Solution.ActionItemExecutionTasks.Null;
      var document = actionItemAssignment.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      var otherDocuments = actionItemAssignment.OtherGroup.All;
      
      // MainTask должен быть изменен до создания вложений и текстов задачи.
      if (document != null)
        task = DirRX.Solution.ActionItemExecutionTasks.As(Sungero.RecordManagement.PublicFunctions.Module.Remote.CreateActionItemExecution(document, actionItemAssignment));
      else
        task = ActionItemExecutionTasks.CreateAsSubtask(actionItemAssignment);
      foreach (var otherDocument in otherDocuments)
        if (!task.OtherGroup.All.Contains(otherDocument))
          task.OtherGroup.All.Add(otherDocument);
      
      task.Assignee = null;
      task.AssignedBy = Employees.Current;
      task.Initiator = Employees.Current;
      
      if (task.IsUnderControl.GetValueOrDefault())
        task.Supervisor = Employees.Current;
      
      return task;
    }
    
    #endregion
    
    #region Из базовой. Получить задания исполнителей и исполнителей не завершивших работу по поручению.
    
    /// Получить задания исполнителей, не завершивших работу по поручению.
    /// </summary>
    /// <param name="entity"> Поручение, для которого требуется получить исполнителей.</param>
    /// <returns>Список исполнителей, не завершивших работу по поручению.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<IActionItemExecutionAssignment> GetActionItemsDir(Sungero.RecordManagement.IActionItemExecutionTask entity)
    {
      return ActionItemExecutionAssignments.GetAll(j => ((entity.IsCompoundActionItem ?? false) ?
                                                         entity.Equals(j.Task.ParentTask) :
                                                         entity.Equals(j.Task)) &&
                                                   j.Status == Sungero.Workflow.AssignmentBase.Status.InProcess);
    }
    
    /// <summary>
    /// Получить исполнителей, не завершивших работу по поручению.
    /// </summary>
    /// <param name="entity"> Поручение, для которого требуется получить исполнителей.</param>
    /// <returns>Список исполнителей, не завершивших работу по поручению.</returns>
    [Public, Remote(IsPure = true)]
    public static IQueryable<IUser> GetActionItemsPerformersDir(Sungero.RecordManagement.IActionItemExecutionTask entity)
    {
      return GetActionItems(entity).Select(p => p.Performer);
    }
    
    #endregion
    
    public override void SetDocumentStates()
    {
      base.SetDocumentStates();
    }
  }
}