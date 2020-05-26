using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.RevisionRequest;

namespace DirRX.PartiesControl
{
  partial class RevisionRequestBindingDocumentsSharedCollectionHandlers
  {

    public virtual void BindingDocumentsDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e)
    {
      _obj.Relations.Remove(Constants.Module.AddendumRelationName, _deleted.Document);
    }

    public virtual void BindingDocumentsAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.Sent = false;
      _added.Received = false;
      _added.IsRequired = false;
    }
  }

  partial class RevisionRequestBindingDocumentsSharedHandlers
  {

    public virtual void BindingDocumentsReceivedChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (_obj.Received.GetValueOrDefault())
        _obj.ReceiveDate = Calendar.Today;
      else
        _obj.ReceiveDate = null;
    }

    public virtual void BindingDocumentsSentChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (_obj.Sent.GetValueOrDefault())
        _obj.SendDate = Calendar.Today;
      else
        _obj.SendDate = null;
    }
  }

  partial class RevisionRequestSecurityServiceDocumentsSharedHandlers
  {

    public virtual void SecurityServiceDocumentsReceivedChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (_obj.Received.GetValueOrDefault())
        _obj.ReceiveDate = Calendar.Today;
      else
        _obj.ReceiveDate = null;
    }

    public virtual void SecurityServiceDocumentsSentChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (_obj.Sent.GetValueOrDefault())
        _obj.SendDate = Calendar.Today;
      else
        _obj.SendDate = null;
    }
  }

  partial class RevisionRequestSecurityServiceDocumentsSharedCollectionHandlers
  {

    public virtual void SecurityServiceDocumentsAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.Format = RevisionRequestSecurityServiceDocuments.Format.Copy;
      _added.Sent = false;
      _added.Received = false;
    }
  }

  partial class RevisionRequestSharedHandlers
  {

    public override void PreparedByChanged(Sungero.Docflow.Shared.OfficialDocumentPreparedByChangedEventArgs e)
    {
      base.PreparedByChanged(e);
    }

    public virtual void CheckingReasonChanged(DirRX.PartiesControl.Shared.RevisionRequestCheckingReasonChangedEventArgs e)
    {
      if (_obj.Counterparty != null && e.NewValue != null && e.NewValue != e.OldValue)
      {
        var documentsList = DirRX.PartiesControl.PublicFunctions.CheckingDocumentList.Remote.GetCheckingDocumentList(!_obj.Counterparty.Nonresident.GetValueOrDefault(),
                                                                                                                     _obj.Counterparty.CounterpartyType.GetValueOrDefault(),
                                                                                                                     _obj.CheckingReason);
        if (documentsList != null)
        {
          _obj.BindingDocuments.Clear();
          
          foreach (var document in documentsList.Documents)
          {
            var checkingDocument = _obj.BindingDocuments.AddNew();
            checkingDocument.DocumentKind = document.DocumentKind;
            checkingDocument.Comment = document.Comment;
            checkingDocument.Format = document.DocFormat;
            checkingDocument.IsRequired = document.IsRequired;
          }
        }
      }
    }

    public override void DocumentKindChanged(Sungero.Docflow.Shared.OfficialDocumentDocumentKindChangedEventArgs e)
    {
      base.DocumentKindChanged(e);
    }

    public virtual void MainDocumentChanged(DirRX.PartiesControl.Shared.RevisionRequestMainDocumentChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue)
      {
        var contract = DirRX.Solution.Contracts.As(e.NewValue);
        if (contract != null)
        {
          _obj.Supervisor = contract.Supervisor;
          return;
        }
        
        var supAgreement = DirRX.Solution.SupAgreements.As(e.NewValue);
        if (supAgreement != null)
          _obj.Supervisor = supAgreement.Supervisor;
      }
    }

    public virtual void CounterpartyChanged(DirRX.PartiesControl.Shared.RevisionRequestCounterpartyChangedEventArgs e)
    {
      if (_obj.Counterparty != null)
        _obj.CheckingType = _obj.Counterparty.CheckingType;
      Functions.RevisionRequest.FillName(_obj);
    }

    public virtual void CounterpartyApprovalChanged(Sungero.Domain.Shared.CollectionPropertyChangedEventArgs e)
    {
      if (_obj.CounterpartyApproval.Any(x => x.ApprovalResult == RevisionRequestCounterpartyApproval.ApprovalResult.Aborted))
        _obj.ApprovalResult = RevisionRequestCounterpartyApproval.ApprovalResult.Aborted;
      else
        _obj.ApprovalResult = RevisionRequestCounterpartyApproval.ApprovalResult.Approved;
    }
    
  }
}