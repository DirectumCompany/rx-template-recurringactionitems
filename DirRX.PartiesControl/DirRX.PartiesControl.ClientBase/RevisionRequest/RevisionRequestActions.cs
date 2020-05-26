using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.RevisionRequest;

namespace DirRX.PartiesControl.Client
{
  partial class RevisionRequestBindingDocumentsActions
  {

    public virtual bool CanCreateDocument(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return !string.IsNullOrEmpty(_obj.DocumentKind);
    }

    public virtual void CreateDocument(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var docKind = _obj.DocumentKind;
      var doc = DirRX.Solution.PublicFunctions.CounterpartyDocument.Remote.Create(docKind);
      if (doc == null)
      {
        Dialogs.NotifyMessage(DirRX.PartiesControl.RevisionRequests.Resources.DocKindNotFound);
        return;
      }
      else
      {
        doc.Counterparty = RevisionRequests.As(_obj.RootEntity).Counterparty;
        doc.Show();
        _obj.Document = doc;
      }
    }
  }

  partial class RevisionRequestAnyChildEntityCollectionActions
  {
    public override void DeleteChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      base.DeleteChildEntity(e);
    }

    public override bool CanDeleteChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return !(_all is Sungero.Domain.Shared.IChildEntityCollection<DirRX.PartiesControl.IRevisionRequestBindingDocuments>);
    }

  }

  partial class RevisionRequestActions
  {


    public override void ShowRegistrationPane(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ShowRegistrationPane(e);
    }

    public override bool CanShowRegistrationPane(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanShowRegistrationPane(e);
    }

    public virtual void OpenDocumentLink(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.CheckingType.ListDocLink != null)
      {
        if (_obj.CheckingType.Name == CheckingTypes.Resources.DefaultTypeSimpleChecking)
          _obj.CheckingType.ListDocLink.Open();
        else
          _obj.CheckingType.ListDocLink.Show();
      }
    }

    public virtual bool CanOpenDocumentLink(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public override void Save(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.Counterparty != null)
      {
        if (_obj.Counterparty.CounterpartyStatus != null && _obj.Counterparty.CounterpartyStatus.Sid == PartiesControl.PublicConstants.CounterpartyStatus.DefaultStatus.StopListSid)
        {
          var stopListItem = _obj.Counterparty.StoplistHistory.FirstOrDefault(s => !s.ExcludeDate.HasValue);
          Dialogs.ShowMessage(DirRX.PartiesControl.RevisionRequests.Resources.StopListMessageFormat(stopListItem.Reason.Name, stopListItem.IncludeDate.Value.ToString("d")), MessageType.Error);
        }
      }
      base.Save(e);
    }

    public override bool CanSave(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSave(e);
    }

    public virtual void AddDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var result = Functions.RevisionRequest.Remote.AddDoucments(_obj);
      if (!result)
        e.AddInformation(DirRX.PartiesControl.RevisionRequests.Resources.AddDocumentsNoTemplates);
    }

    public virtual bool CanAddDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void PrintInventory(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var report = Reports.GetInternalInventoryReport();
      report.Entity = _obj;
      report.Open();
    }

    public virtual bool CanPrintInventory(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsChanged;
    }

    public virtual void RequestMissingDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!_obj.BindingDocuments.Where(d => d.Received == false).Any())
      {
        e.AddWarning(DirRX.PartiesControl.RevisionRequests.Resources.RequestMissingDocumentsWarning);
        return;
      }
      else
        Functions.RevisionRequest.Remote.SendRequestMissingDocuments(_obj).Show();
    }

    public virtual bool CanRequestMissingDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Functions.RevisionRequest.Remote.CheckUserInRole(Users.Current, Constants.Module.ArchiveResponsibleRole);
    }

    public virtual void Abort(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var idAssignment = CallContext.CalledFrom(DirRX.Solution.ApprovalSimpleAssignments.Info) ?
        CallContext.GetCallerEntityId(DirRX.Solution.ApprovalSimpleAssignments.Info) :
        CallContext.GetCallerEntityId(DirRX.Solution.ApprovalCheckingAssignments.Info);
      
      if (!Functions.RevisionRequest.Remote.CanApproveCounterparty(_obj, idAssignment))
      {
        e.AddError(RevisionRequests.Resources.NoRightsToApprove);
        return;
      }
      
      var dialog = Dialogs.CreateInputDialog(DirRX.PartiesControl.RevisionRequests.Resources.InputCause);
      var textCause = dialog.AddMultilineString(DirRX.PartiesControl.RevisionRequests.Resources.Description, true);
      var resultButton = dialog.Buttons.AddCustom(DirRX.PartiesControl.RevisionRequests.Resources.AbortButtonName);
      dialog.Buttons.AddCancel();
      if ( dialog.Show() == resultButton)
      {
        var approval = _obj.CounterpartyApproval.Where(x => Solution.Employees.Equals(x.Approver, Solution.Employees.Current)).FirstOrDefault();
        if (approval == null)
          approval = _obj.CounterpartyApproval.AddNew();
        approval.Approver = Solution.Employees.Current;
        approval.ApprovalResult = RevisionRequestCounterpartyApproval.ApprovalResult.Aborted;
        approval.ApprovalDate = Calendar.Today;
        approval.ApprovalNote = textCause.Value;
      }
      
    }

    public virtual bool CanAbort(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return CallContext.CalledFrom(DirRX.Solution.ApprovalSimpleAssignments.Info) || CallContext.CalledFrom(DirRX.Solution.ApprovalCheckingAssignments.Info);
    }

    public virtual void Approve(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var idAssignment = CallContext.CalledFrom(DirRX.Solution.ApprovalSimpleAssignments.Info) ?
        CallContext.GetCallerEntityId(DirRX.Solution.ApprovalSimpleAssignments.Info) :
        CallContext.GetCallerEntityId(DirRX.Solution.ApprovalCheckingAssignments.Info);
      
      if (!Functions.RevisionRequest.Remote.CanApproveCounterparty(_obj, idAssignment))
      {
        e.AddError(RevisionRequests.Resources.NoRightsToApprove);
        return;
      }
      
      var approval = _obj.CounterpartyApproval.Where(x => Solution.Employees.Equals(x.Approver, Solution.Employees.Current)).FirstOrDefault();
      if (approval == null)
        approval = _obj.CounterpartyApproval.AddNew();
      approval.Approver = Solution.Employees.Current;
      approval.ApprovalResult = RevisionRequestCounterpartyApproval.ApprovalResult.Approved;
      approval.ApprovalDate = Calendar.Today;
    }

    public virtual bool CanApprove(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return CallContext.CalledFrom(DirRX.Solution.ApprovalSimpleAssignments.Info) || CallContext.CalledFrom(DirRX.Solution.ApprovalCheckingAssignments.Info);
    }

  }

}