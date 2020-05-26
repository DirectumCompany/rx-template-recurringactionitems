using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.ActionItemRejectionAssignment;

namespace DirRX.ActionItems.Client
{
  partial class ActionItemRejectionAssignmentActions
  {
    public virtual void CancelChanged(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var task = ActionItemRejectionTasks.As(_obj.Task);
      
      _obj.Category = task.Category;
      _obj.Priority = task.Priority;
      _obj.Initiator = task.Initiator;
      _obj.ReportDeadline = task.ReportDeadline;
      _obj.Mark = task.Mark;
      _obj.AssignedBy = task.AssignedBy;
      _obj.Supervisor = task.Supervisor;
      _obj.Assignee = task.Assignee;
      _obj.IsUnderControl = task.IsUnderControl;
      _obj.ActionItemDeadline = task.ActionItemDeadline;
      _obj.ActionItem = task.ActionItem;

      _obj.Subscribers.Clear();
      foreach (var subscriberMain in task.Subscribers)
      {
        var subscriber = _obj.Subscribers.AddNew();
        subscriber.Subscriber = DirRX.Solution.Employees.As(subscriberMain.Subscriber);
      }
      
      _obj.CoAssignees.Clear();
      foreach (var coAssigneeMain in task.CoAssignees)
      {
        var coAssignee = _obj.CoAssignees.AddNew();
        coAssignee.Assignee = DirRX.Solution.Employees.As(coAssigneeMain.Assignee);
      }
    }

    public virtual bool CanCancelChanged(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ReturnTask(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (Functions.ActionItemRejectionAssignment.ParamsChanged(_obj))
      {
        e.AddError(ActionItemRejectionAssignments.Resources.ChangedParams, _obj.Info.Actions.CancelChanged);
        return;
      }
    }

    public virtual bool CanReturnTask(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void Change(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.ActionItemRejectionAssignment.ParamsChanged(_obj))
      {
        e.AddError(ActionItemRejectionAssignments.Resources.ParamsNotChanged);
        return;
      }
    }

    public virtual bool CanChange(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void Abort(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      
    }

    public virtual bool CanAbort(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

  }

}