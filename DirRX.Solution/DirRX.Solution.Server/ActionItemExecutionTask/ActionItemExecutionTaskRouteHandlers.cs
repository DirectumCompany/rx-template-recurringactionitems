using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;
using DirRX.Solution.ActionItemExecutionTask;

namespace DirRX.Solution.Server
{
  partial class ActionItemExecutionTaskRouteHandlers
  {

    #region Уведомление о приёмке (убрана отпрвка, т.к. уведомления отправляются через настроечный справочник).
    
    public override void StartBlock8(Sungero.RecordManagement.Server.ActionItemExecutionNotificationArguments e)
    {
      // Задать состояние поручения.
      _obj.ExecutionState = ExecutionState.Executed;

      // Установить статусы в документе из поручения.
      Functions.ActionItemExecutionTask.SetDocumentStates(_obj);
    }
    
    #endregion
    
    #region Блок Контроль.
    public override void StartAssignment6(Sungero.RecordManagement.IActionItemSupervisorAssignment assignment, Sungero.RecordManagement.Server.ActionItemSupervisorAssignmentArguments e)
    {
      base.StartAssignment6(assignment, e);
      DirRX.Solution.ActionItemSupervisorAssignments.As(assignment).Category = _obj.Category;
      DirRX.Solution.ActionItemSupervisorAssignments.As(assignment).Priority = _obj.Priority;
      
      // Рассылка уведомлений по событию "Поручение выполнено Исполнителем и ожидает подтверждения Контролером".
      try
      {
        ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("OnControlEvent", _obj);
      }
      catch (Exception ex)
      {
        Logger.Error("Error in OnControlEvent", ex);
      }
    }
    
    public override void CompleteAssignment6(Sungero.RecordManagement.IActionItemSupervisorAssignment assignment, Sungero.RecordManagement.Server.ActionItemSupervisorAssignmentArguments e)
    {
      base.CompleteAssignment6(assignment, e);
      
      // Рассылка уведомлений по событию "Поручение выполнено Исполнителем и ожидает подтверждения Контролером".
      var supervisorAssignment = DirRX.Solution.ActionItemSupervisorAssignments.As(assignment);
      if (supervisorAssignment != null && supervisorAssignment.Result == DirRX.Solution.ActionItemSupervisorAssignment.Result.Agree)
      {
        try
        {
          ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("AcceptEvent", _obj);
        }
        catch (Exception ex)
        {
          Logger.Error("Error in AcceptEvent", ex);
        }
      }
      
      // Рассылка уведомлений по событию "Исполнение поручения отправлено на доработку".
      if (supervisorAssignment != null && supervisorAssignment.Result == DirRX.Solution.ActionItemSupervisorAssignment.Result.ForRework)
      {
        try
        {
          ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("ReworkEvent", _obj);
        }
        catch (Exception ex)
        {
          Logger.Error("Error in ReworkEvent", ex);
        }
      }
    }
    #endregion

    #region Блок Исполнение поручения.
    public override void StartAssignment4(Sungero.RecordManagement.IActionItemExecutionAssignment assignment, Sungero.RecordManagement.Server.ActionItemExecutionAssignmentArguments e)
    {
      base.StartAssignment4(assignment, e);
      
      var executionAssignment = DirRX.Solution.ActionItemExecutionAssignments.As(assignment);
      
      executionAssignment.Category = _obj.Category;
      executionAssignment.Priority = _obj.Priority;
      executionAssignment.Deadline = _obj.Deadline;
      executionAssignment.ReportDeadline = _obj.ReportDeadline;
      executionAssignment.Mark = _obj.Mark;
      executionAssignment.Initiator = _obj.Initiator;
      executionAssignment.IsEscalated = _obj.IsEscalated;
      
      foreach (var subscriber in _obj.Subscribers)
      {
        var assignmentSubscriber = executionAssignment.Subscribers.AddNew();
        assignmentSubscriber.Subscriber = subscriber.Subscriber;
      }
      
      // Рассылка уведомлений по событию "Поручение, не поставленное на контроль, выполнено Исполнителем".
      try
      {
        ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("StartEvent", _obj);
      }
      catch (Exception ex)
      {
        Logger.Error("Error in StartEvent", ex);
      }
    }
    
    public override void CompleteAssignment4(Sungero.RecordManagement.IActionItemExecutionAssignment assignment, Sungero.RecordManagement.Server.ActionItemExecutionAssignmentArguments e)
    {
      base.CompleteAssignment4(assignment, e);
      
      // Рассылка уведомлений по событию "Постановщик сформировал и отправил поручение в системе".
      if (!_obj.IsUnderControl.GetValueOrDefault())
      {
        try
        {
          ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("PerformEvent", _obj);
        }
        catch (Exception ex)
        {
          Logger.Error("Error in PerformEvent", ex);
        }
      }
    }
    
    #endregion

    #region Блок формирования задач на исполнение.
    
    public override void Script10Execute()
    {
      #region Скопировано из стандартной с присвоением настроечных реквизитов.
      
      var subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(_obj, ActionItemExecutionTasks.Resources.TaskSubject);
      var document = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      
      Sungero.Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);
      
      // Задания соисполнителям.
      if (_obj.CoAssignees != null && _obj.CoAssignees.Count > 0)
      {
        var performer = _obj.CoAssignees.FirstOrDefault(ca => ca.AssignmentCreated != true);
        
        var parentAssignment = ActionItemExecutionAssignments.GetAll()
          .Where(j => Equals(j.Task, _obj))
          .Where(j => j.Status == Sungero.Workflow.AssignmentBase.Status.InProcess)
          .Where(j => Equals(j.Performer, _obj.Assignee))
          .FirstOrDefault();
        var actionItemExecution = ActionItemExecutionTasks.CreateAsSubtask(parentAssignment);
        actionItemExecution.Importance = _obj.Importance;
        actionItemExecution.ActionItemType = ActionItemType.Additional;
        
        // Синхронизировать вложения и выдать права.
        if (document != null)
        {
          actionItemExecution.DocumentsGroup.OfficialDocuments.Add(document);
          if (!document.AccessRights.CanRead(performer.Assignee))
            document.AccessRights.Grant(performer.Assignee, DefaultAccessRightsTypes.Read);
        }
        
        foreach (var addInformation in _obj.OtherGroup.All)
          actionItemExecution.OtherGroup.All.Add(addInformation);
        
        // Задать текст.
        actionItemExecution.Texts.Last().IsAutoGenerated = true;
        
        // Задать поручение.
        actionItemExecution.ActionItem = _obj.ActionItem;
        
        // Задать тему.
        actionItemExecution.Subject = subject;
        
        // Задать исполнителя, ответственного, контролера и инициатора.
        actionItemExecution.Assignee = performer.Assignee;
        actionItemExecution.AssignedBy = DirRX.Solution.Employees.As(_obj.Assignee);
        
        // Задать срок.
        actionItemExecution.Deadline = _obj.Deadline;
        actionItemExecution.MaxDeadline = _obj.Deadline;
        
        // Задать настроечные реквизиты.
        actionItemExecution.Category = _obj.Category;
        actionItemExecution.Initiator = _obj.Initiator;
        actionItemExecution.IsUnderControl = true;
        actionItemExecution.Supervisor = _obj.Assignee;
        actionItemExecution.ReportDeadline = _obj.ReportDeadline;
        
        
        foreach (var subscriber in _obj.Subscribers)
        {
          var newTaskSubscriber = actionItemExecution.Subscribers.AddNew();
          newTaskSubscriber.Subscriber = subscriber.Subscriber;
        }
        
        actionItemExecution.Start();
        
        performer.AssignmentCreated = true;
      }
      
      // Составное поручение.
      if (_obj.ActionItemParts != null && _obj.ActionItemParts.Count > 0)
      {
        var job = _obj.ActionItemParts.FirstOrDefault(aip => aip.AssignmentCreated != true);
        
        var actionItemExecution = ActionItemExecutionTasks.CreateAsSubtask(_obj);
        actionItemExecution.Importance = _obj.Importance;
        actionItemExecution.ActionItemType = ActionItemType.Component;
        
        // Синхронизировать вложения и выдать права.
        if (document != null)
        {
          actionItemExecution.DocumentsGroup.OfficialDocuments.Add(document);
          if (!document.AccessRights.CanRead(job.Assignee))
            document.AccessRights.Grant(job.Assignee, DefaultAccessRightsTypes.Read);
        }
        
        foreach (var addInformation in _obj.OtherGroup.All)
          actionItemExecution.OtherGroup.All.Add(addInformation);
        
        // Задать поручение и текст.
        actionItemExecution.ActionItem = string.IsNullOrWhiteSpace(job.ActionItemPart) ? _obj.ActionItem : job.ActionItemPart;
        
        // Задать тему.
        actionItemExecution.Subject = Functions.ActionItemExecutionTask.GetActionItemExecutionSubject(actionItemExecution, ActionItemExecutionTasks.Resources.TaskSubject);
        
        // Задать исполнителя, ответственного, контролера и инициатора.
        actionItemExecution.Assignee = job.Assignee;
        actionItemExecution.Author = _obj.Author;
        actionItemExecution.AssignedBy = _obj.AssignedBy;
        
        // Задать срок.
        var actionItemDeadline = job.Deadline.HasValue ? job.Deadline : _obj.FinalDeadline;
        actionItemExecution.Deadline = actionItemDeadline;
        actionItemExecution.MaxDeadline = actionItemDeadline;
        
        // Задать настроечные реквизиты.
        actionItemExecution.Category = _obj.Category;
        actionItemExecution.Initiator = _obj.Initiator;
        actionItemExecution.IsUnderControl = _obj.IsUnderControl;
        actionItemExecution.Supervisor = _obj.Supervisor;
        actionItemExecution.ReportDeadline = _obj.ReportDeadline;
        
        foreach (var subscriber in _obj.Subscribers)
        {
          var newTaskSubscriber = actionItemExecution.Subscribers.AddNew();
          newTaskSubscriber.Subscriber = subscriber.Subscriber;
        }
        
        actionItemExecution.Start();
        
        // Добавить составные подзадачи в исходящее.
        if (actionItemExecution.Status == Sungero.Workflow.Task.Status.InProcess)
          Sungero.Workflow.SpecialFolders.GetOutbox(_obj.StartedBy).Items.Add(actionItemExecution);
        
        // Записать ссылку на поручение в составное поручение.
        job.ActionItemPartExecutionTask = actionItemExecution;
        
        job.AssignmentCreated = true;
      }

      #endregion
    }
    
    #endregion
  }

}