using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ActionItemExecutionAssignment;

namespace DirRX.Solution.Client
{
  partial class ActionItemExecutionAssignmentActions
  {
    public override void PrintActionItem(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var report = DirRX.ActionItems.Reports.GetPrintActionItemTask();
      
      if (_obj.MainTask != null)
        report.Task = DirRX.Solution.ActionItemExecutionTasks.As(_obj.MainTask);
      else
        report.Task = DirRX.Solution.ActionItemExecutionTasks.As(_obj.Task);
      
      report.Open();
    }

    public override bool CanPrintActionItem(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status != Sungero.RecordManagement.ActionItemExecutionAssignment.Status.Aborted;
    }

    public override void CreateChildActionItem(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var subTask = Functions.ActionItemExecutionTask.Remote.CreateActionItemExecutionFromExecutionDir(_obj);
      subTask.Show();
    }

    public override bool CanCreateChildActionItem(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var canInPriority = true;
      
      // Ограничение по времени возможности отклонения из справочника приоритетов.
      if (_obj.Priority != null && _obj.Priority.CompletionDeadlinePercent.HasValue)
      {
        var deadline = Sungero.Docflow.PublicFunctions.Module.GetDateWithTime(_obj.Deadline.Value, _obj.Performer);
        var allHours = WorkingTime.GetDurationInWorkingHours(_obj.Created.Value, deadline, _obj.Performer);
        var hoursInWork = WorkingTime.GetDurationInWorkingHours(_obj.Created.Value, Calendar.Now.ToUserTime(), _obj.Performer);
        
        canInPriority = (100 - ((double)hoursInWork / allHours * 100)) > _obj.Priority.CompletionDeadlinePercent.Value;
      }
      
      return base.CanCreateChildActionItem(e) && canInPriority;
    }

    public override void Done(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      #region Скопировано из стандартной разработки.
      
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        e.AddError(ActionItemExecutionAssignments.Resources.ReportIsNotFilled);
        return;
      }
      
      if (!Functions.ActionItemExecutionAssignment.Remote.IsCoAllAssigneeAssignamentCreated(_obj))
      {
        Dialogs.NotifyMessage(ActionItemExecutionAssignments.Resources.AssignmentsNotCreated);
        e.Cancel();
      }
      
      var giveRights = Sungero.Docflow.PublicFunctions.Module.ShowDialogGrantAccessRights(_obj, _obj.ResultGroup.All.Concat(_obj.OtherGroup.All).ToList());
      if (giveRights == false)
        e.Cancel();
      
      // Проверить наличие любых подзадач в работе.
      var subActionItemExecutions = ActionItems.PublicFunctions.Module.Remote.GetSubTasksByAssignment(_obj);
      if (!subActionItemExecutions.Any())
      {
        if (giveRights == null)
        {
          // Замена стандартного диалога подтверждения выполнения действия.
          var confirmationDialog = Dialogs.CreateTaskDialog(ActionItemExecutionAssignments.Resources.DoneConfirmationMessage,
                                                            MessageType.Question);
          confirmationDialog.Buttons.AddYesNo();
          confirmationDialog.Buttons.Default = DialogButtons.Yes;
          if (confirmationDialog.Show() != DialogButtons.Yes)
            e.Cancel();
        }
      }
      else
      {
        var confirmationDialog = Dialogs.CreateTaskDialog(ActionItemExecutionTasks.Resources.StopSubTasks,
                                                          ActionItemExecutionTasks.Resources.StopSubTasksDescription,
                                                          MessageType.Question);
        var abort = confirmationDialog.Buttons.AddCustom(ActionItemExecutionAssignments.Resources.Abort);
        confirmationDialog.Buttons.Default = abort;
        var notAbort = confirmationDialog.Buttons.AddCustom(ActionItemExecutionAssignments.Resources.NotAbort);
        confirmationDialog.Buttons.AddCancel();
        var result = confirmationDialog.Show();
        
        // Необходимость прекращения подчиненных поручений.
        if (result == abort)
          ActionItems.PublicFunctions.Module.Remote.AbortSubTasksByAssignment(_obj);
        
        // Отменить выполнения задания.
        if (result == DialogButtons.Cancel)
          e.Cancel();
      }
      
      #endregion
    }

    public override bool CanDone(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return base.CanDone(e);
    }

    public override void ExtendDeadline(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ExtendDeadline(e);
    }

    public override bool CanExtendDeadline(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      
      return base.CanExtendDeadline(e) && _obj.Priority.AllowedExtendDeadline == true && _obj.Deadline.Value >= Calendar.Today;
    }

    public virtual void AddSubscriber(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var subscribers = DirRX.Solution.PublicFunctions.Module.GetSelectedEmployees(_obj.Subscribers.Select(s => s.Subscriber).ToList());
      if (subscribers.Any())
      {
        var isSend = DirRX.ActionItems.PublicFunctions.SendNoticeQueueItem.Remote.CreateSendNoticeQueueItem(subscribers.ToList(), null, _obj);
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
      return _obj.AccessRights.CanUpdate() && _obj.Task.Status == Status.InProcess;
    }
    
    public virtual void ChangePerformer(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var subAssignments = Functions.ActionItemExecutionAssignment.Remote.GetSubAssignments(_obj, ActionItemExecutionTask.ActionItemType.Main);
      if (subAssignments.Any())
      {
        var dialog = Dialogs.CreateTaskDialog(ActionItemExecutionTasks.Resources.SubAssignmentsInWorkFinded, MessageType.Question);
        dialog.Buttons.AddYes();
        dialog.Buttons.AddNo();
        dialog.Buttons.Default = DialogButtons.No;
        
        var result = dialog.Show();
        if (result == DialogButtons.Yes)
          Functions.ActionItemExecutionAssignment.Remote.AbortSubAssignments(subAssignments);
      }

      if (_obj.EmployeePerformer == null)
      {
        e.AddError(ActionItemExecutionAssignments.Resources.NewPerformerIsEmpty);
        return;
      }
      
      var executionAssignmentTask = ActionItemExecutionTasks.As(_obj.Task);
      var currentCoPerformerList = executionAssignmentTask.CoAssignees.Select(a => DirRX.Solution.Employees.As(a.Assignee)).ToList<DirRX.Solution.IEmployee>();
      var newCoPerformerList = _obj.CoPerformers.Select(a => DirRX.Solution.Employees.As(a.CoPerformer)).ToList<DirRX.Solution.IEmployee>();
      
      if (newCoPerformerList.Any())
      {
        if (!Equals(_obj.EmployeePerformer, DirRX.Solution.Employees.As(_obj.Performer)) || !currentCoPerformerList.SequenceEqual(newCoPerformerList))
        {
          Functions.ActionItemExecutionAssignment.Remote.StartNewPerformerAssignment(_obj, _obj.EmployeePerformer, newCoPerformerList);
          e.CloseFormAfterAction = true;
        }
        else
          e.AddWarning(ActionItemExecutionAssignments.Resources.PerformerDoesNotChanged);
      }
      else
      {
        if (!Equals(_obj.EmployeePerformer, DirRX.Solution.Employees.As(_obj.Performer)))
        {
          Functions.ActionItemExecutionAssignment.Remote.StartNewPerformerAssignment(_obj, _obj.EmployeePerformer, new List<IEmployee> {});
          e.CloseFormAfterAction = true;
        }
        else
          e.AddWarning(ActionItemExecutionAssignments.Resources.PerformerDoesNotChanged);
      }
      
    }

    public virtual bool CanChangePerformer(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == ActionItemExecutionAssignment.Status.InProcess;
    }

    public virtual void Rejection(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var rejectionTaskCreated = DirRX.ActionItems.PublicFunctions.Module.Remote.RejectionTaskCreated(_obj);
      if (rejectionTaskCreated)
      {
        e.AddError(ActionItemExecutionAssignments.Resources.RejectionTaskInWorkOrCreated);
        return;
      }
      
      var dialog = Dialogs.CreateInputDialog(ActionItemExecutionAssignments.Resources.RejectionDialogSubject);
      var rejectionReason = dialog.AddMultilineString(ActionItemExecutionAssignments.Resources.RejectionDialogReason, false);
      
      dialog.SetOnButtonClick(args =>
                              {
                                if (string.IsNullOrWhiteSpace(rejectionReason.Value))
                                  args.AddError(ActionItemExecutionAssignments.Resources.RejectionEmptyReason);
                              });
      
      if (dialog.Show() == DialogButtons.Ok)
      {
        var actionItemExecutionTask	= ActionItemExecutionTasks.As(_obj.Task);
        var actionItemExecutionMainTask	= ActionItemExecutionTasks.Is(_obj.MainTask) ? ActionItemExecutionTasks.As(_obj.MainTask) : actionItemExecutionTask;
        
        // Ограничиваем причину по максимальной длине свойства в задаче.
        if (rejectionReason.Value.Length > DirRX.ActionItems.ActionItemRejectionTasks.Info.Properties.Reason.Length)
          rejectionReason.Value = rejectionReason.Value.Substring(0, DirRX.ActionItems.ActionItemRejectionTasks.Info.Properties.Reason.Length);
        
        DirRX.ActionItems.PublicFunctions.Module.Remote.SendRejectionTask(actionItemExecutionMainTask, actionItemExecutionTask, rejectionReason.Value);
        e.CloseFormAfterAction = true;
      }
    }

    public virtual bool CanRejection(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var canInPriority = true;
      
      // Ограничение по времени возможности отклонения из справочника приоритетов.
      if (_obj.Priority != null && _obj.Priority.RejectionDeadlinePercent.HasValue)
      {
        var deadline = Sungero.Docflow.PublicFunctions.Module.GetDateWithTime(_obj.Deadline.Value, _obj.Performer);
        var allHours = WorkingTime.GetDurationInWorkingHours(_obj.Created.Value, deadline, _obj.Performer);
        var hoursInWork = WorkingTime.GetDurationInWorkingHours(_obj.Created.Value, Calendar.Now.ToUserTime(), _obj.Performer);
        
        canInPriority = ((double)hoursInWork / allHours * 100) < _obj.Priority.RejectionDeadlinePercent.Value;
      }

      var actionItemExecutionTask = DirRX.Solution.ActionItemExecutionTasks.As(_obj.Task);
      return actionItemExecutionTask.ActionItemType != Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Additional &&
        canInPriority;
    }

  }

}