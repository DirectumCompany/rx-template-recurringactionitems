using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Contract;

namespace DirRX.Solution
{
  partial class ContractCounterpartiesDeliveryMethodPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CounterpartiesDeliveryMethodFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = query.Where(q => q.Sid == null || q.Sid != DirRX.ContractsCustom.PublicConstants.Module.WithRensposibleMailDeliveryMethod.ToString());
      return query;
    }
  }

  partial class ContractDeliveryMethodPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> DeliveryMethodFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = base.DeliveryMethodFiltering(query, e);
      query = query.Where(q => q.Sid == null || q.Sid != DirRX.ContractsCustom.PublicConstants.Module.WithRensposibleMailDeliveryMethod.ToString());
      return query;
    }
  }
  
  partial class ContractCounterpartiesContactPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CounterpartiesContactFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Counterparty != null)
        query = query.Where(c => Equals(c.Company, _obj.Counterparty));
      return query;
    }
  }

  partial class ContractCounterpartiesAddressPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CounterpartiesAddressFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Counterparty != null)
        query = query.Where(x => Equals(x.Counterparty, _obj.Counterparty));
      return query;
    }
  }


  partial class ContractCounterpartiesSignatoryPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CounterpartiesSignatoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Counterparty != null)
        query = query.Where(c => Equals(c.Company, _obj.Counterparty));
      return query;
    }
  }

  partial class ContractCounterpartiesCounterpartyPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CounterpartiesCounterpartyFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Contract.IsTender != true)
        query = query.Where(x => !DirRX.Solution.Companies.Equals(x, DirRX.Solution.Companies.As(DirRX.ContractsCustom.PublicFunctions.Module.Remote.GetTenderPurchaseCounterparty())));
      return query;
    }
  }

  partial class ContractShippingAddressPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ShippingAddressFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Counterparty != null)
        query = query.Where(x => Equals(x.Counterparty, _obj.Counterparty));
      return query;
    }
  }

  partial class ContractSubcategoryPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> SubcategoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.DocumentGroup != null)
      {
        var subcategories = _obj.DocumentGroup.CounterpartySubcategories.Where(s => s.Subcategories != null).Select(s => s.Subcategories).ToList();
        return query.Where(s => subcategories.Contains(s));
      }
      
      return query;
    }
  }

  partial class ContractDocumentGroupPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> DocumentGroupFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = base.DocumentGroupFiltering(query, e);

      return query.Where(c => c.DocumentKinds.Select(k => Sungero.Docflow.DocumentKinds.Equals(k.DocumentKind, _obj.DocumentKind)).Any());
    }
  }

  partial class ContractCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      
      e.Without(_info.Properties.CSBExportDate);
      e.Without(_info.Properties.Counterparties.Properties.CSBExportDate);
      
      e.Without(_info.Properties.ApproveStatuses);
      e.Without(_info.Properties.ScanMoveStatuses);
      e.Without(_info.Properties.OriginalMoveStatuses);
      e.Without(_info.Properties.ApproveLabel);
      e.Without(_info.Properties.ScanMoveLabel);
      e.Without(_info.Properties.OriginalMoveLabel);
      
      e.Without(_info.Properties.OriginalSigning);
      e.Without(_info.Properties.ContractorOriginalSigning);
      e.Without(_info.Properties.ChangingShippingReason);
      
      // Занесем в параметры контакт, способ доставки и адрес, чтобы заполнить эти поля с событии создания.
      if (_source.ShippingAddress != null)
        e.Params.AddOrUpdate(_info.Properties.ShippingAddress.Name, _source.ShippingAddress.Id);
      if (_source.DeliveryMethod != null)
        e.Params.AddOrUpdate(_info.Properties.DeliveryMethod.Name, _source.DeliveryMethod.Id);
      if (_source.Contact != null)
        e.Params.AddOrUpdate(_info.Properties.Contact.Name, _source.Contact.Id);
    }
  }

  partial class ContractBackdatingReasonPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> BackdatingReasonFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(r => r.Usage == DirRX.ContractsCustom.DefaultReasons.Usage.Backdating);
    }
  }

  partial class ContractCounterpartyPropertyFilteringServerHandler<T>
  {
    public override IQueryable<T> CounterpartyFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = base.CounterpartyFiltering(query, e);
      if (_obj.IsTender != true)
        query = query.Where(x => !DirRX.Solution.Companies.Equals(x, DirRX.Solution.Companies.As(DirRX.ContractsCustom.PublicFunctions.Module.Remote.GetTenderPurchaseCounterparty())));
      return query;
    }
  }

  partial class ContractServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      // Формируем префикс и постфикс рег. номера.
      var leadingDocumentNumber = Functions.Contract.GetLeadDocumentNumber(_obj);
      var errorMessage = Functions.DocumentRegister.UpdateDocumentPrefixAndPostfix(_obj, e, leadingDocumentNumber);
      if (errorMessage != string.Empty)
        e.AddError(errorMessage);
      
      #region Заполнение полей для вычислимых списков выданных и отправленных контрагенту

      var folderTracking = _obj.Tracking
        .Where(l => l.DeliveredTo != null && !l.ReturnDate.HasValue)
        .OrderByDescending(l => l.DeliveryDate);

      var issueToContractor = folderTracking
        .Where(l => l.ReturnDeadline.HasValue && l.Action == Solution.ContractTracking.Action.OriginalSend)
        .OrderBy(l => l.ReturnDeadline).ThenBy(l => !(l.IsOriginal ?? false)).FirstOrDefault();

      _obj.ResponsibleForReturnEmployee = issueToContractor != null ? issueToContractor.DeliveredTo : null;
      _obj.IsHeldByCounterParty = issueToContractor != null;
      _obj.ScheduledReturnDateFromCounterparty = issueToContractor != null ? issueToContractor.ReturnDeadline : null;

      #endregion
      
      if (_obj.ValidFrom.HasValue && _obj.DocumentValidity.HasValue)
        _obj.ValidTill = _obj.ValidFrom.Value.AddMonths(_obj.DocumentValidity.Value).AddDays(-1);

      #region Работа со статусами движения документа
      
      // Установка статусов отправки pdf/скан-копий контрагенту.
      var addedRecords = (IEnumerable<IContractTracking>)_obj.State.Properties.Tracking.Added;
      var sendPDF = addedRecords.Any(l => (l.Action == ContractTracking.Action.Sending && l.Format == ContractTracking.Format.Pdf) ||
                                     (l.Action == ContractTracking.Action.Endorsement && l.Format == ContractTracking.Format.Pdf && l.ReturnDate.HasValue));
      if (sendPDF)
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              DirRX.ContractsCustom.PublicConstants.Module.ContractStatusGuid.PDFSendedForSigningGuid,
                                                                              DirRX.ContractsCustom.PublicConstants.Module.ContractStatusType.ScanMoveStatus,
                                                                              true);
      
      var sendCopy = addedRecords.Any(l => (l.Action == ContractTracking.Action.Sending && l.Format == ContractTracking.Format.CopyScan) ||
                                      (l.Action == ContractTracking.Action.Endorsement && l.Format == ContractTracking.Format.CopyScan && l.ReturnDate.HasValue));
      
      if (sendCopy)
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              DirRX.ContractsCustom.PublicConstants.Module.ContractStatusGuid.ScanSendedCounterpartyForSigningGuid,
                                                                              DirRX.ContractsCustom.PublicConstants.Module.ContractStatusType.ScanMoveStatus,
                                                                              true);

      // Запись статусов в строки для отображения.
      var labelStatuses = new StringBuilder();
      foreach (var status in _obj.ApproveStatuses.Select(s => s.Status))
        labelStatuses.AppendLine(status.Name);
      _obj.ApproveLabel = labelStatuses.Length > 0 ? labelStatuses.ToString() : null;
      labelStatuses.Clear();
      foreach (var status in _obj.ScanMoveStatuses.Select(s => s.Status))
        labelStatuses.AppendLine(status.Name);
      _obj.ScanMoveLabel = labelStatuses.Length > 0 ? labelStatuses.ToString() : null;
      labelStatuses.Clear();
      foreach (var status in _obj.OriginalMoveStatuses.Select(s => s.Status))
        labelStatuses.AppendLine(status.Name);
      _obj.OriginalMoveLabel = labelStatuses.Length > 0 ? labelStatuses.ToString() : null;
      
      #endregion
      
      if (_obj.State.Properties.LifeCycleState.IsChanged && _obj.LifeCycleState == LifeCycleState.Active)
      {
        // Отправить ответственному задачу на отправку документа в ИСУ "ЛЛК".
        var asyncConvertHandler = DirRX.ContractsCustom.AsyncHandlers.SendTaskToIMSResponsible.Create();
        asyncConvertHandler.DocumentId = _obj.Id;
        asyncConvertHandler.ExecuteAsync();
      }
    }

    public override void BeforeSigning(Sungero.Domain.BeforeSigningEventArgs e)
    {
      base.BeforeSigning(e);
      
      if (e.Signature.SignatureType == SignatureType.Approval)
      {
        _obj.CounterpartyStatus = _obj.Counterparty.CounterpartyStatus;
        
        if (_obj.ActualDate == null)
          _obj.ActualDate = e.Signature.SigningDate;
        if (_obj.ValidFrom == null)
          _obj.ValidFrom = e.Signature.SigningDate;
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);

      if (!_obj.State.IsCopied)
      {
        _obj.IsContractorSignsFirst = true;
        _obj.IsTender = false;
        _obj.IsHighUrgency = false;
        _obj.IsScannedImageSign = false;
        _obj.DaysToFinishWorks = DirRX.ContractsCustom.PublicConstants.Module.MonthsToFinishWorks;
        _obj.IsFrameContract = false;
        _obj.IsAutomaticRenewal = false;
        _obj.IsAnalysisRequired = false;
        _obj.IsCorporateApprovalRequired = false;
        _obj.StartConditionsExists = false;
        _obj.AreConditionsCompleted = false;
        _obj.IsTermless = false;
        _obj.IsMainActivity = false;
        _obj.IsBranded = false;
        _obj.AnalysisRequiredExclude = false;
        _obj.LukoilApproving = DirRX.Solution.Contract.LukoilApproving.NotRequired;
        _obj.BrandedProducts = false;
      }
      else
      {
        // Заполним поля из копируемого документа, которые сохранили в параметры в событии копирования записи.
        var shippingAddressId = 0;
        if (e.Params.TryGetValue(_obj.Info.Properties.ShippingAddress.Name, out shippingAddressId))
        {
          _obj.ShippingAddress = DirRX.PartiesControl.ShippingAddresses.Get(shippingAddressId);
          e.Params.Remove(_obj.Info.Properties.ShippingAddress.Name);
        }
        var deliveryMethodId = 0;
        if (e.Params.TryGetValue(_obj.Info.Properties.DeliveryMethod.Name, out deliveryMethodId))
        {
          _obj.DeliveryMethod = DirRX.Solution.MailDeliveryMethods.Get(deliveryMethodId);
          e.Params.Remove(_obj.Info.Properties.DeliveryMethod.Name);
          // Уберем обязательность с причины смены способа доставки.
          _obj.State.Properties.ChangingShippingReason.IsRequired = false;
        }
        var contactId = 0;
        if (e.Params.TryGetValue(_obj.Info.Properties.Contact.Name, out contactId))
        {
          _obj.Contact = Sungero.Parties.Contacts.Get(contactId);
          e.Params.Remove(_obj.Info.Properties.Contact.Name);
        }
      }
      // Обнуление признаков при создании
      _obj.Resending = false;
      _obj.OnRegistration = false;
      
      if (_obj.IsManyCounterparties == null)
        _obj.IsManyCounterparties = false;
    }
  }

  partial class ContractUrgencyReasonPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> UrgencyReasonFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(x => x.Usage == DirRX.ContractsCustom.DefaultReasons.Usage.Promptly);
    }
  }
}