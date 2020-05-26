using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ActionItemExecutionTask;

namespace DirRX.Solution.Client
{
  internal static class ActionItemExecutionTaskStaticActions
  {
    public static void FollowUpExecution(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var report = DirRX.ActionItems.Reports.GetCustomActionItemsExecutionReport();
      report.Open();
    }

    public static bool CanFollowUpExecution(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

  partial class ActionItemExecutionTaskActions
  {
    public virtual void PrintActionItem(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var report = DirRX.ActionItems.Reports.GetPrintActionItemTask();
      report.Task = DirRX.Solution.ActionItemExecutionTasks.As(_obj.MainTask);
      report.Open();
    }

    public virtual bool CanPrintActionItem(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Started.HasValue && _obj.Status != Sungero.RecordManagement.ActionItemExecutionTask.Status.Aborted;
    }
    
    public virtual void ChangeSupervisor(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var dialog = Dialogs.CreateInputDialog("Выберите нового контролера");
      var newSupervisor = dialog.AddSelect("Контролер", true, Employees.Null);
      if (dialog.Show() == DialogButtons.Ok)
      {
        _obj.IsUnderControl = true;
        _obj.Supervisor = newSupervisor.Value;
        _obj.AccessRights.Grant(_obj.Supervisor, DefaultAccessRightsTypes.Change);
        if (_obj.State.Properties.Supervisor.OriginalValue != null)
          _obj.AccessRights.Revoke(_obj.State.Properties.Supervisor.OriginalValue, DefaultAccessRightsTypes.Change);
        _obj.AccessRights.Save();
        _obj.Save();
      }
    }

    public virtual bool CanChangeSupervisor(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Sungero.Workflow.Task.Status.InProcess && _obj.AccessRights.CanUpdate();
    }

    #region Из коробки.
    
    public override void RequireReport(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var task = Functions.StatusReportRequestTask.Remote.CreateStatusReportRequest(_obj);
      if (task == null)
        e.AddWarning(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.NoActiveChildActionItems);
      else
        task.Show();
    }
    #endregion

    public override bool CanRequireReport(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanRequireReport(e);
    }

    public virtual void SetRepeat(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.State.IsInserted)
      {
        e.AddError(ActionItemExecutionTasks.Resources.EntityIsInsertedError);
        return;
      }
      
      DirRX.ActionItems.IRepeatSetting repeatSetting;
      
      if (_obj.RepeatSetting != null)
        repeatSetting = _obj.RepeatSetting;
      else
      {
        repeatSetting = DirRX.ActionItems.PublicFunctions.RepeatSetting.Remote.CreateRepeatSetting();
        repeatSetting.Category = _obj.Category;
        repeatSetting.AssignedBy = _obj.AssignedBy;
        repeatSetting.Initiator = _obj.Initiator;
        repeatSetting.Mark = _obj.Mark;
        repeatSetting.ReportDeadline = _obj.ReportDeadline;
        repeatSetting.ActionItem = _obj.ActionItem;
        
        foreach (var subscriber in _obj.Subscribers)
        {
          var subscriberRepeat = repeatSetting.Subscribers.AddNew();
          subscriberRepeat.Subscriber = subscriber.Subscriber;
        }
        
        if (_obj.IsCompoundActionItem.GetValueOrDefault())
        {
          repeatSetting.IsCompoundActionItem = true;
          
          foreach (var job in _obj.ActionItemParts)
          {
            var jobRepeat = repeatSetting.ActionItemsParts.AddNew();
            jobRepeat.ActionItemPart = job.ActionItemPart;
            jobRepeat.Assignee = DirRX.Solution.Employees.As(job.Assignee);
          }
          
          repeatSetting.IsUnderControl = _obj.IsUnderControl.GetValueOrDefault();
          repeatSetting.Supervisor = DirRX.Solution.Employees.As(_obj.Supervisor);
        }
        else
        {
          repeatSetting.Assignee = DirRX.Solution.Employees.As(_obj.Assignee);
          repeatSetting.IsUnderControl = _obj.IsUnderControl.GetValueOrDefault();
          repeatSetting.Supervisor = DirRX.Solution.Employees.As(_obj.Supervisor);
          
          foreach (var coAssignee in _obj.CoAssignees)
          {
            var coAssigneeRepeat = repeatSetting.CoAssignees.AddNew();
            coAssigneeRepeat.Assignee = DirRX.Solution.Employees.As(coAssignee.Assignee);
          }
        }
      }
      
      repeatSetting.ShowModal();
      
      if (!repeatSetting.State.IsInserted && !DirRX.ActionItems.RepeatSettings.Equals(_obj.RepeatSetting, repeatSetting))
        _obj.RepeatSetting = repeatSetting;
      
      if (_obj.RepeatSetting != null)
      {
        // Необходимо вызвать Save() для корректной смены состояния переключателя.
        _obj.Save();
        
        if (_obj.RepeatSetting.Status == DirRX.ActionItems.RepeatSetting.Status.Active)
          _obj.IsRepeating = true;
        else
          _obj.IsRepeating = false;
      }
    }

    public virtual bool CanSetRepeat(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate();
    }

    public override void ChangeCompoundMode(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.IsUnderControl.GetValueOrDefault() == true && _obj.IsCompoundActionItem == false &&
          _obj.Initiator != null && !Solution.Employees.Equals(_obj.Initiator, _obj.Supervisor))
      {
        var dialog = Dialogs.CreateTaskDialog(DirRX.Solution.ActionItemExecutionTasks.Resources.ChangeCompoundModeDialogText,
                                              DirRX.Solution.ActionItemExecutionTasks.Resources.ChangeCompoundModeDialogDescription,
                                              MessageType.Question,
                                              DirRX.Solution.ActionItemExecutionTasks.Resources.ChangeCompoundModeDialogTitle);
        dialog.Buttons.AddYesNo();
        var result = dialog.Show();
        
        if (result == DialogButtons.Yes)
        {
          _obj.Supervisor = _obj.Initiator;
        }
        else
          return;
      }
      base.ChangeCompoundMode(e);
    }

    public override bool CanChangeCompoundMode(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanChangeCompoundMode(e);
    }

    public virtual void AddSubscriber(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var subscribers = DirRX.Solution.PublicFunctions.Module.GetSelectedEmployees(_obj.Subscribers.Select(s => s.Subscriber).ToList());
      if (subscribers.Any())
      {
        var isSend = DirRX.ActionItems.PublicFunctions.SendNoticeQueueItem.Remote.CreateSendNoticeQueueItem(subscribers.ToList(), _obj, null);
        if (isSend)
        {
          foreach (var subscriber in subscribers)
          {
            var newSubscriber = _obj.Subscribers.AddNew();
            newSubscriber.Subscriber = subscriber;
          }
          
          _obj.Save();
        }
        else
          e.AddError(DirRX.ActionItems.Resources.AddSubscriberError);
      }
    }

    public virtual bool CanAddSubscriber(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate() &&
        (_obj.Status == ActionItemExecutionTask.Status.InProcess ||
         _obj.Status == ActionItemExecutionTask.Status.UnderReview);
    }

  }

}