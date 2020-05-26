using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalCheckingAssignment;

namespace DirRX.Solution.Client
{
  partial class ApprovalCheckingAssignmentActions
  {
    public virtual void Deny(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Валидация заполненности активного текста.
      if (string.IsNullOrWhiteSpace(_obj.ActiveText))
      {
        e.AddError(DirRX.Solution.ApprovalAssignments.Resources.ActiveTextEmpty);
        return;
      }
      
      var mainDocument = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var accessInfo = Locks.GetLockInfo(mainDocument);
      if (accessInfo.IsLocked)
      {
        e.AddError(accessInfo.LockedMessage);
        return;
      }
      
      var action = new Sungero.Workflow.Client.ExecuteResultActionArgs(e.FormType, e.Entity, e.Action);
      base.ForRework(action);
      _obj.Denied = true;
      _obj.Complete(Result.ForRework);
      e.CloseFormAfterAction = true;
      _obj.Save();
    }

    public virtual bool CanDeny(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var canResultAction = new Sungero.Workflow.Client.CanExecuteResultActionArgs(e.FormType, e.Entity);
      return _obj.Status == Status.InProcess && base.CanForRework(canResultAction);
    }

    public override void Accept(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (_obj.ApprovalStage != null && _obj.ApprovalStage.NeedCounterpartyApproval.GetValueOrDefault())
      {
        var request = DirRX.PartiesControl.RevisionRequests.As(_obj.DocumentGroup.OfficialDocuments.FirstOrDefault());
        if (request != null && !request.CounterpartyApproval.Any(a => Users.Equals(Users.Current, a.Approver)))
        {
          e.AddError(ApprovalSimpleAssignments.Resources.NeedCounterpartyApprove);
          return;
        }
      }
      
      base.Accept(e);
    }

    public override bool CanAccept(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return base.CanAccept(e);
    }

    public virtual void RequestInititator(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      string subject = document == null ? string.Empty : LocalActs.RequestInitiatorTasks.Resources.ThemeInitiatorTaskFormat(document.Name);
      LocalActs.PublicFunctions.RequestInitiatorTask.Remote.CreateNewRequestInitiatorTask(subject, _obj).Show();
    }

    public virtual bool CanRequestInititator(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Status.InProcess;
    }

    public override void ForRework(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (!Functions.ApprovalTask.ValidateBeforeRework(_obj, ApprovalTasks.Resources.NeedTextForRework, e))
        return;
      
      base.ForRework(e);
    }

    public override bool CanForRework(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return base.CanForRework(e);
    }

    public virtual void Recycling(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.ApprovalTask.ValidateBeforeRework(_obj, DirRX.Solution.Resources.ActiveTextEmpty, e))
        return;
      
      var resultAction = new Sungero.Workflow.Client.ExecuteResultActionArgs(e.FormType, e.Entity, e.Action);
      base.ForRework(resultAction);
      _obj.ForRecycle = true;
      e.CloseFormAfterAction = true;
      _obj.Complete(Result.ForRework);
    }

    public virtual bool CanRecycling(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var canResultAction = new Sungero.Workflow.Client.CanExecuteResultActionArgs(e.FormType, e.Entity);
      return _obj.Status == Status.InProcess && base.CanForRework(canResultAction);
    }

  }

}