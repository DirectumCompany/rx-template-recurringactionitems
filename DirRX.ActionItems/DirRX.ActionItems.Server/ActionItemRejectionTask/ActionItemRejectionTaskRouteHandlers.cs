using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;
using DirRX.ActionItems.ActionItemRejectionTask;

namespace DirRX.ActionItems.Server
{
  partial class ActionItemRejectionTaskRouteHandlers
  {

    #region Отправка уведомления.

    public virtual void StartBlock5(DirRX.ActionItems.Server.ActionItemPerformerNoticeArguments e)
    {
      if (_obj.ActionItemExecutionMainTask != null)
      {
        //TODO: Логика уведомлений задаётся в соответствующим справочнике. Удалить, если логика справочника закроет все кейсы.
        //foreach (var noticePerformer in _obj.NoticePerformers)
        //  e.Block.Performers.Add(noticePerformer.Performer);
      }
      else
      {
        // Рассылка уведомлений по событию "Постановщик вернул поручение Исполнителю без изменений".
        try
        {
          ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("ReturnEvent", _obj.ActionItemExecutionTask);
        }
        catch (Exception ex)
        {
          Logger.Error("Error in ReturnEvent", ex);
        }
      }

    }
    
    public virtual void StartNotice5(DirRX.ActionItems.IActionItemPerformerNotice notice, DirRX.ActionItems.Server.ActionItemPerformerNoticeArguments e)
    {
      if (_obj.ActionItemExecutionMainTask == null)
      {
        notice.Subject = Functions.ActionItemRejectionTask.GetSubjectRejectAssignment(ActionItemRejectionTasks.Resources.SubjectReturnNotice, _obj.ActionItemExecutionTask);
        notice.ActionItemGroup.ActionItemExecutionTasks.Add(_obj.ActionItemExecutionTask);
      }
      else
      {
        if (_obj.ActionItemChanged.Value)
        {
          notice.Subject = Functions.ActionItemRejectionTask.GetSubjectRejectAssignment(ActionItemRejectionTasks.Resources.SubjectChangesNotice, _obj.ActionItemExecutionTask);
          notice.ActionItemGroup.ActionItemExecutionTasks.Add(_obj.ActionItemExecutionTask);
        }
        else
        {
          notice.Subject = Functions.ActionItemRejectionTask.GetSubjectRejectAssignment(ActionItemRejectionTasks.Resources.SubjectAbortNotice, _obj.ActionItemExecutionTask);
          notice.ActionItemGroup.ActionItemExecutionTasks.Add(_obj.ActionItemExecutionTask);
        }
      }
      
    }
    
    #endregion
    
    #region Прекращение поручения.
    
    public virtual void Script7Execute()
    {
      _obj.ActionItemExecutionTask.Abort();
    }

    #endregion
    
    #region Изменение параметров поручения.

    public virtual void Script6Execute()
    {
      _obj.ActionItemExecutionTask.Abort();
      
      var coAssigneeTasks = DirRX.Solution.ActionItemExecutionTasks.GetAll()
        .Where(t => t.ActionItemType == Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Additional &&
               DirRX.Solution.ActionItemExecutionTasks.Equals(t.MainTask, _obj.ActionItemExecutionTask));
      
      foreach (var coAssigneeTask in coAssigneeTasks)
        coAssigneeTask.Abort();
      
      _obj.ActionItemExecutionTask.Category = _obj.Category;
      _obj.ActionItemExecutionTask.Priority = _obj.Priority;
      _obj.ActionItemExecutionTask.Initiator = _obj.Initiator;
      _obj.ActionItemExecutionTask.ReportDeadline = _obj.ReportDeadline;
      _obj.ActionItemExecutionTask.Mark = _obj.Mark;
      _obj.ActionItemExecutionTask.AssignedBy = _obj.AssignedBy;
      _obj.ActionItemExecutionTask.IsUnderControl = _obj.IsUnderControl;
      _obj.ActionItemExecutionTask.Supervisor = _obj.Supervisor;

      // Рассылка уведомлений по событию "Параметры поручения изменены".
      _obj.ActionItemExecutionTask.ActionItem = _obj.ActionItem;

      _obj.ActionItemExecutionTask.Subscribers.Clear();
      foreach (var subscriberChanged in _obj.Subscribers)
      {
        var subscriber = _obj.ActionItemExecutionTask.Subscribers.AddNew();
        subscriber.Subscriber = DirRX.Solution.Employees.As(subscriberChanged.Subscriber);
      }
      
      _obj.ActionItemExecutionTask.CoAssignees.Clear();
      foreach (var coAssigneeChanged in _obj.CoAssignees)
      {
        var coAssignee = _obj.ActionItemExecutionTask.CoAssignees.AddNew();
        coAssignee.Assignee = DirRX.Solution.Employees.As(coAssigneeChanged.Assignee);
      }
      
      _obj.ActionItemChanged = true;
      
      _obj.ActionItemExecutionTask.Restart();
      _obj.ActionItemExecutionTask.Start();
    }

    #endregion
    
    #region Задание постановщику.
    
    public virtual void StartBlock4(DirRX.ActionItems.Server.ActionItemRejectionAssignmentArguments e)
    {
      e.Block.Performers.Add(_obj.Initiator);
      e.Block.RelativeDeadlineHours = 4;
      
      // Рассылка уведомлений по событию "Исполнитель отклонил исполнение поручения".
      try
      {
        ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("RejectionEvent", _obj.ActionItemExecutionTask);
      }
      catch (Exception ex)
      {
        Logger.Error("Error in RejectionEvent", ex);
      }
    }

    public virtual void StartAssignment4(DirRX.ActionItems.IActionItemRejectionAssignment assignment, DirRX.ActionItems.Server.ActionItemRejectionAssignmentArguments e)
    {
      assignment.Category = _obj.Category;
      assignment.Priority = _obj.Priority;
      assignment.Initiator = _obj.Initiator;
      assignment.ReportDeadline = _obj.ReportDeadline;
      assignment.Mark = _obj.Mark;
      assignment.AssignedBy = _obj.AssignedBy;
      assignment.Assignee = _obj.Assignee;
      assignment.IsUnderControl = _obj.IsUnderControl;
      assignment.Supervisor = _obj.Supervisor;
      assignment.ActionItemDeadline = _obj.ActionItemDeadline;
      assignment.Reason = _obj.Reason;
      assignment.ActionItem = _obj.ActionItem;

      foreach (var subscriberMain in _obj.Subscribers)
      {
        var subscriber = assignment.Subscribers.AddNew();
        subscriber.Subscriber = DirRX.Solution.Employees.As(subscriberMain.Subscriber);
        
        var noticePerformer = _obj.NoticePerformers.AddNew();
        noticePerformer.Performer = DirRX.Solution.Employees.As(subscriberMain.Subscriber);
      }
      
      foreach (var coAssigneeMain in _obj.CoAssignees)
      {
        var coAssignee = assignment.CoAssignees.AddNew();
        coAssignee.Assignee = DirRX.Solution.Employees.As(coAssigneeMain.Assignee);
        
        var noticePerformer = _obj.NoticePerformers.AddNew();
        noticePerformer.Performer = DirRX.Solution.Employees.As(coAssigneeMain.Assignee);
      }
    }
    
    public virtual void CompleteAssignment4(DirRX.ActionItems.IActionItemRejectionAssignment assignment, DirRX.ActionItems.Server.ActionItemRejectionAssignmentArguments e)
    {
      if (assignment.Result == DirRX.ActionItems.ActionItemRejectionAssignment.Result.Change)
      {
        // Рассылка уведомлений по событию "Исполнитель отклонил исполнение поручения".
        try
        {
          ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("ActionItemChangedEvent", _obj.ActionItemExecutionTask);
        }
        catch (Exception ex)
        {
          Logger.Error("Error in RejectionEvent", ex);
        }
      }
      
      _obj.Category = assignment.Category;
      _obj.Priority = assignment.Priority;
      _obj.Initiator = assignment.Initiator;
      _obj.ReportDeadline = assignment.ReportDeadline;
      _obj.Mark = assignment.Mark;
      _obj.AssignedBy = assignment.AssignedBy;
      _obj.Supervisor = assignment.Supervisor;
      _obj.Assignee = assignment.Assignee;
      _obj.IsUnderControl = assignment.IsUnderControl;
      _obj.ActionItemDeadline = assignment.ActionItemDeadline;
      _obj.ActionItem = assignment.ActionItem;

      _obj.Subscribers.Clear();
      foreach (var subscriberChanged in assignment.Subscribers)
      {
        var subscriber = _obj.Subscribers.AddNew();
        subscriber.Subscriber = DirRX.Solution.Employees.As(subscriberChanged.Subscriber);
        
        if (!_obj.NoticePerformers.Any(p => DirRX.Solution.Employees.Equals(p.Performer, subscriberChanged.Subscriber)))
        {
          var noticePerformer = _obj.NoticePerformers.AddNew();
          noticePerformer.Performer = subscriberChanged.Subscriber;
        }
      }
      
      _obj.CoAssignees.Clear();
      foreach (var coAsigneeChanged in assignment.CoAssignees)
      {
        var coAssigneee = _obj.CoAssignees.AddNew();
        coAssigneee.Assignee = DirRX.Solution.Employees.As(coAsigneeChanged.Assignee);
        
        if (!_obj.NoticePerformers.Any(p => DirRX.Solution.Employees.Equals(p.Performer, coAsigneeChanged.Assignee)))
        {
          var noticePerformer = _obj.NoticePerformers.AddNew();
          noticePerformer.Performer = coAsigneeChanged.Assignee;
        }
      }
      
      if (assignment.Result == DirRX.ActionItems.ActionItemRejectionAssignment.Result.ReturnTask)
        _obj.ActionItemExecutionMainTask = null;
    }

    #endregion

  }
}