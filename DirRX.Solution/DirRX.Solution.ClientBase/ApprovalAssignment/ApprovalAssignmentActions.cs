using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalAssignment;

namespace DirRX.Solution.Client
{
  partial class ApprovalAssignmentActions
  {
    public virtual bool CanReject(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Functions.ApprovalAssignment.CanExecuteApprovalActions(_obj);
    }

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
      base.ForRevision(action);
      _obj.Denied = true;
      _obj.Complete(Result.ForRevision);
      e.CloseFormAfterAction = true;
      _obj.Save();
    }

    public virtual bool CanDeny(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Functions.ApprovalAssignment.CanExecuteApprovalActions(_obj);
    }

    public virtual void Reject(Sungero.Domain.Client.ExecuteActionArgs e)
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
      base.Approved(action);
      _obj.Rejected = true;
      _obj.Complete(Result.Approved);
      e.CloseFormAfterAction = true;
      _obj.Save();
    }

    
    
    public override void Approved(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      base.Approved(e);
    }

    public override bool CanApproved(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return base.CanApproved(e) && _obj.RiskLevel == null && string.IsNullOrEmpty(_obj.RiskDescription);
    }

    public override void Forward(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      base.Forward(e);
    }

    public override bool CanForward(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return base.CanForward(e) && _obj.RiskLevel == null && string.IsNullOrEmpty(_obj.RiskDescription);
    }

    public override void ForRevision(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      base.ForRevision(e);
    }

    public override bool CanForRevision(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return base.CanForRevision(e) && _obj.RiskLevel == null && string.IsNullOrEmpty(_obj.RiskDescription);
    }

    public virtual void RequestInititatorAction(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      string subject = document == null ? string.Empty : LocalActs.RequestInitiatorTasks.Resources.ThemeInitiatorTaskFormat(document.Name);
      LocalActs.PublicFunctions.RequestInitiatorTask.Remote.CreateNewRequestInitiatorTask(subject, _obj).Show();
    }

    public virtual bool CanRequestInititatorAction(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == Status.InProcess;
    }


    public virtual void ApprovedWithRisk(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.RiskLevel == null || _obj.RiskDescription == null)
        e.AddError(DirRX.Solution.ApprovalAssignments.Resources.ApprovedWithRiskEmptyError);
      else
      {
        var resultAction = new Sungero.Workflow.Client.ExecuteResultActionArgs(e.FormType, e.Entity, e.Action);
        var risk = DirRX.LocalActs.PublicFunctions.Risk.Remote.CreateRisk(_obj);
        base.Approved(resultAction);
        _obj.ApprovedWithRisk = true;
        _obj.Complete(Result.Approved);
        _obj.RiskAttachmentGroup.Risks.Add(risk);
        e.CloseFormAfterAction = true;
        _obj.Save();
      }
    }

    public virtual bool CanApprovedWithRisk(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      var resultAction = new Sungero.Workflow.Client.CanExecuteResultActionArgs(e.FormType, e.Entity);
      return base.CanApproved(resultAction) && _obj.Status == Status.InProcess;
    }

    public virtual void Recycling(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.Recycling(_obj, e, Sungero.Docflow.ApprovalAssignment.Result.ForRevision);
    }

    public virtual bool CanRecycling(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Functions.ApprovalAssignment.CanExecuteApprovalActions(_obj);
    }

    public virtual void AddSubscribers(Sungero.Domain.Client.ExecuteActionArgs e)
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

    public virtual bool CanAddSubscribers(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate() && _obj.Status == Status.InProcess;
    }

  }
}