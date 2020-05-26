using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.RevisionRequest;

namespace DirRX.PartiesControl
{
  partial class RevisionRequestSecurityServiceDocumentsDocumentPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> SecurityServiceDocumentsDocumentFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(cd => DirRX.Solution.Companies.Equals(_obj.RevisionRequest.Counterparty, cd.Counterparty));
    }
  }

  partial class RevisionRequestBindingDocumentsDocumentPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> BindingDocumentsDocumentFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(cd => DirRX.Solution.Companies.Equals(_obj.RevisionRequest.Counterparty, cd.Counterparty));
    }
  }

  partial class RevisionRequestCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      e.Without(_info.Properties.CheckingDate);
      e.Without(_info.Properties.CheckingResult);
      e.Without(_info.Properties.ApprovalResult);
      e.Without(_info.Properties.CounterpartyApproval);
      e.Without(_info.Properties.SecurityComment);
      e.Without(_info.Properties.CaseFile);
      e.Without(_info.Properties.PlacedToCaseFileDate);
    }
  }

  partial class RevisionRequestFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return query;
      
      if (_filter.CheckingResult != null)
        query = query.Where(x => CheckingResults.Equals(x.CheckingResult, _filter.CheckingResult));
      
      if (_filter.CheckingType != null)
        query = query.Where(x => CheckingTypes.Equals(x.CheckingType, _filter.CheckingType));
      
      if (_filter.CheckingReason != null)
        query = query.Where(x => CheckingReasons.Equals(x.CheckingReason, _filter.CheckingReason));
      
      
      if (_filter.HasDocuments)
        query = query.Where(x => x.Counterparty.IsDocumentsProvided == _filter.HasDocuments);
      
      // Фильтр по интервалу времени
      var periodBegin = Calendar.UserToday.AddDays(-7);
      var periodEnd = Calendar.UserToday.EndOfDay();
      
      if (_filter.LastWeek)
        periodBegin = Calendar.UserToday.AddDays(-7);
      
      if (_filter.LastMonth)
        periodBegin = Calendar.UserToday.AddDays(-30);
      
      if (_filter.Last90Days)
        periodBegin = Calendar.UserToday.AddDays(-90);
      
      if (_filter.ManualPeriod)
      {
        periodBegin = _filter.DateRangeFrom ?? Calendar.SqlMinValue;
        periodEnd = _filter.DateRangeTo ?? Calendar.SqlMaxValue;
      }
      
      var serverPeriodBegin = Equals(Calendar.SqlMinValue, periodBegin) ? periodBegin : Sungero.Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(periodBegin);
      var serverPeriodEnd = Equals(Calendar.SqlMaxValue, periodEnd) ? periodEnd : periodEnd.EndOfDay().FromUserTime();
      query = query.Where(j => (j.CheckingDate.HasValue && j.CheckingDate.Between(periodBegin, periodEnd)) ||
                          (j.Created.HasValue && j.Created.Between(periodBegin, periodEnd)));
      
      return query;
    }
  }

  partial class RevisionRequestServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.Counterparty != null)
      {
        if (_obj.Counterparty.CounterpartyStatus != null && _obj.Counterparty.CounterpartyStatus.Sid == PartiesControl.PublicConstants.CounterpartyStatus.DefaultStatus.StopListSid)
        {
          var stopListItem = _obj.Counterparty.StoplistHistory.FirstOrDefault(s => !s.ExcludeDate.HasValue);
          e.AddError(DirRX.PartiesControl.RevisionRequests.Resources.StopListMessageFormat(stopListItem.Reason.Name, stopListItem.IncludeDate.Value.ToString("d")));
          return;
        }
      }
      
      base.BeforeSave(e);
      
      #region Обновление связей документов
      var oldRelatedDocs = _obj.Relations.GetRelated();
      var newRelatedDocs = _obj.BindingDocuments.Where(d => d.Document != null).Select(d => d.Document).ToList();
      newRelatedDocs.AddRange(_obj.SecurityServiceDocuments.Where(d => d.Document != null).Select(d => d.Document).ToList());
      
      foreach (var doc in newRelatedDocs.Where(d => !oldRelatedDocs.Any(od => Sungero.Content.IElectronicDocument.Equals(od, d))))
        _obj.Relations.Add(Constants.Module.AddendumRelationName, doc);
      #endregion
      
      // Признак о получении документов. Простановка в карточке контрагента происходит в ФП AllDocsReceivedHandling.
      _obj.AllDocsReceived = !_obj.BindingDocuments.Any(d => d.Document != null && (d.Sent == false || d.Received == false)) &&
        !_obj.SecurityServiceDocuments.Any(d => d.Document != null && (d.Sent == false || d.Received == false));

      
      var approvalTasks = Solution.ApprovalTasks.GetAll(t => t.Status == Solution.ApprovalTask.Status.InProcess)
        .ToList()
        .Where(t => DirRX.PartiesControl.RevisionRequests.Equals(t.DocumentGroup.OfficialDocuments.FirstOrDefault(), _obj));
      
      if (approvalTasks.Any())
      {
        var bindingSequrityDocCopies = _obj.SecurityServiceDocuments.Where(d => d.Format == DirRX.PartiesControl.RevisionRequestBindingDocuments.Format.Copy);
        foreach (var bindingSequrityDocRow in bindingSequrityDocCopies)
        {
          bindingSequrityDocRow.Sent = true;
          bindingSequrityDocRow.SendDate = Calendar.UserToday;
        }
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      if (CallContext.CalledFrom(Solution.Companies.Info))
        _obj.Counterparty = Solution.Companies.Get(CallContext.GetCallerEntityId(Solution.Companies.Info));
      
      if (Solution.Employees.Current != null)
        _obj.Supervisor = DirRX.ActionItems.PublicFunctions.ActionItemsRole.Remote.GetInitCEOManager(Solution.Employees.As(Solution.Employees.Current));
      
      if (_obj.State.IsCopied)
      {
        foreach (var bindingDocument in _obj.BindingDocuments)
        {
          bindingDocument.SendDate = null;
          bindingDocument.Sent = false;
          bindingDocument.ReceiveDate = null;
          bindingDocument.Received = false;
        }
        
        foreach (var securityDocument in _obj.SecurityServiceDocuments)
        {
          securityDocument.SendDate = null;
          securityDocument.Sent = false;
          securityDocument.ReceiveDate = null;
          securityDocument.Received = false;
        }
      }
      
      if (_obj.Counterparty != null)
        Functions.RevisionRequest.AddCounterpartyInformation(_obj);
    }
  }

}