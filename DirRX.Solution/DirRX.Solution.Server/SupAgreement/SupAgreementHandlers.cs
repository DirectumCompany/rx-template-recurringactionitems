using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.SupAgreement;

namespace DirRX.Solution
{
  partial class SupAgreementCounterpartiesDeliveryMethodPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CounterpartiesDeliveryMethodFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = query.Where(q => q.Sid == null || q.Sid != DirRX.ContractsCustom.PublicConstants.Module.WithRensposibleMailDeliveryMethod.ToString());
      return query;
    }
  }


  partial class SupAgreementDeliveryMethodPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> DeliveryMethodFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = base.DeliveryMethodFiltering(query, e);
      query = query.Where(q => q.Sid == null || q.Sid != DirRX.ContractsCustom.PublicConstants.Module.WithRensposibleMailDeliveryMethod.ToString());
      return query;
    }
  }
  
  partial class SupAgreementCounterpartiesContactPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CounterpartiesContactFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Counterparty != null)
        query = query.Where(c => Equals(c.Company, _obj.Counterparty));
      return query;
    }
  }

  partial class SupAgreementCounterpartiesAddressPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CounterpartiesAddressFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Counterparty != null)
        query = query.Where(x => Equals(x.Counterparty, _obj.Counterparty));
      return query;
    }
  }
  partial class SupAgreementCounterpartiesSignatoryPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CounterpartiesSignatoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Counterparty != null)
        query = query.Where(c => Equals(c.Company, _obj.Counterparty));
      return query;
    }
  }

  partial class SupAgreementShippingAddressPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ShippingAddressFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Counterparty != null)
        query = query.Where(x => Equals(x.Counterparty, _obj.Counterparty));
      return query;
    }
  }

  partial class SupAgreementCreatingFromServerHandler
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
    }
  }

  partial class SupAgreementBackdatingReasonPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> BackdatingReasonFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(r => r.Usage == DirRX.ContractsCustom.DefaultReasons.Usage.Backdating);
    }
  }

  partial class SupAgreementServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      // Формируем префикс и постфикс рег. номера.
      var leadingDocumentNumber = Functions.SupAgreement.GetLeadDocumentNumber(_obj);
      var errorMessage = Functions.DocumentRegister.UpdateDocumentPrefixAndPostfix(_obj, e, leadingDocumentNumber);
      if (errorMessage != string.Empty)
        e.AddError(errorMessage);
      
      #region Заполнение полей для вычислимых списков выданных и отправленных контрагенту

      var folderTracking = _obj.Tracking
        .Where(l => l.DeliveredTo != null && !l.ReturnDate.HasValue)
        .OrderByDescending(l => l.DeliveryDate);

      var issueToContractor = folderTracking
        .Where(l => l.ReturnDeadline.HasValue && l.Action == Solution.SupAgreementTracking.Action.OriginalSend)
        .OrderBy(l => l.ReturnDeadline).ThenBy(l => !(l.IsOriginal ?? false)).FirstOrDefault();

      _obj.ResponsibleForReturnEmployee = issueToContractor != null ? issueToContractor.DeliveredTo : null;
      _obj.IsHeldByCounterParty = issueToContractor != null;
      _obj.ScheduledReturnDateFromCounterparty = issueToContractor != null ? issueToContractor.ReturnDeadline : null;

      #endregion
      
      if (_obj.ValidFrom.HasValue && _obj.DocumentValidity.HasValue)
        _obj.ValidTill = _obj.ValidFrom.Value.AddMonths(_obj.DocumentValidity.Value).AddDays(-1);
      
      #region Работа со статусами движения документа
      
      // Установка статусов отправки pdf/скан-копий контрагенту.
      var addedRecords = (IEnumerable<ISupAgreementTracking>)_obj.State.Properties.Tracking.Added;
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
        if (_obj.Counterparty != null)
          _obj.CounterpartyStatus = _obj.Counterparty.CounterpartyStatus;
        if (_obj.ActualDate == null)
          _obj.ActualDate = e.Signature.SigningDate;
        if (_obj.ValidFrom == null)
          _obj.ValidFrom = e.Signature.SigningDate;
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      // Запретить создание ДС не из договора.
      if (!CallContext.CalledFrom(Solution.Contracts.Info) && !Users.Current.IncludedIn(Roles.Administrators))
        throw new InvalidOperationException("Создавать доп.соглашение можно только из карточки договора.");
      
      base.Created(e);
      
      // Заполнить поля аналогично описанию карточки договора.
      _obj.IsContractorSignsFirst = true;
      _obj.IsHighUrgency = false;
      
      // Обнуление признаков при создании
      _obj.Resending = false;
      _obj.BrandedProducts = false;
      _obj.OnRegistration = false;
      _obj.UsedTrademark = false;
      _obj.IsCorporateApprovalRequired = false;
      _obj.StartConditionsExists = false;
      _obj.AreConditionsCompleted = false;
      _obj.IsMainActivity = false;
      _obj.AnalysisRequiredExclude = false;
      
      _obj.DaysToFinishWorks = DirRX.ContractsCustom.PublicConstants.Module.MonthsToFinishWorks;
      _obj.LukoilApproving = DirRX.Solution.Contract.LukoilApproving.NotRequired;
      
      // Перенести поля из карточки договора.
      if (CallContext.CalledFrom(Solution.Contracts.Info))
      {
        var contract = Solution.Contracts.Get(CallContext.GetCallerEntityId(Solution.Contracts.Info));
        if (contract == null)
          return;
        
        // Для группы полей «Наша сторона».
        if (contract.DocumentGroup != null && contract.DocumentGroup.Supervisor != null)
        {
          var roleRecipients = contract.DocumentGroup.Supervisor.RecipientLinks.FirstOrDefault(x => x.Member != null && DirRX.Solution.Employees.Is(x.Member));
          if (roleRecipients != null)
            _obj.Supervisor = DirRX.Solution.Employees.As(roleRecipients.Member);
        }
        
        _obj.IsAnalysisRequired = contract.IsAnalysisRequired;
        
        if (_obj.IsManyCounterparties == null)
          _obj.IsManyCounterparties = false;
        
        if (contract.DocumentGroup != null && contract.DocumentGroup.DestinationCountry == ContractCategory.DestinationCountry.RF)
        {
          var countryCode = DirRX.ContractsCustom.PublicConstants.Module.RussianFederationCountryCode;
          var country = DirRX.ContractsCustom.PublicFunctions.Module.Remote.GetCountryByCode(countryCode);
          if (country != null)
          {
            _obj.DestinationCountries.Clear();
            _obj.DestinationCountries.AddNew().DestinationCountry = country;
          }
        }
      }
    }
  }

  partial class SupAgreementUrgencyReasonPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> UrgencyReasonFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(x => x.Usage == DirRX.ContractsCustom.DefaultReasons.Usage.Promptly);
    }
  }

}