using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Contracts;
using DirRX.ContractsCustom;
using Sungero.Metadata;
using Sungero.Domain.Shared;

namespace DirRX.Solution.Module.ContractsUI.Server
{
  partial class ContractsHistoryFolderHandlers
  {

    public override IQueryable<Sungero.Contracts.IContractualDocument> ContractsHistoryDataQuery(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      var documents = query.Where(d => DirRX.Solution.Contracts.Is(d) || DirRX.Solution.SupAgreements.Is(d));
      
      if (_filter == null)
        return documents;
      
      #region Фильтры
      
      // Фильтр по состоянию.
      var prefix = Sungero.Docflow.Constants.OfficialDocument.Operation.Prefix.LifeCycle;
      if (_filter.Active)
        documents = documents.WhereDocumentHistory(h => h.Operation == new Enumeration(prefix + DirRX.Solution.Contract.LifeCycleState.Active.ToString()));
      if (_filter.Closed)
        documents = documents.WhereDocumentHistory(h => h.Operation == new Enumeration(prefix + DirRX.Solution.Contract.LifeCycleState.Closed.ToString()));
      if (_filter.Obsolete)
        documents = documents.WhereDocumentHistory(h => h.Operation == new Enumeration(prefix + DirRX.Solution.Contract.LifeCycleState.Obsolete.ToString()));
      if (_filter.OpenSAP)
        documents = documents.WhereDocumentHistory(h => h.Operation == new Enumeration(prefix + DirRX.Solution.Contract.LifeCycleState.OpenSAP.ToString()));
      if (_filter.Deleted)
        documents = documents.WhereDocumentHistory(h => h.Operation == new Enumeration(prefix + DirRX.Solution.Contract.LifeCycleState.Deleted.ToString()));
      
      // Фильтр "Контрагент".
      if (_filter.Contractor != null)
        documents = documents.Where(c => (Solution.Contracts.Is(c) && Solution.Contracts.As(c).Counterparties.Any(cp => Companies.Equals(cp.Counterparty, _filter.Contractor))) ||
                                    (SupAgreements.Is(c) && SupAgreements.As(c).Counterparties.Any(cp => Companies.Equals(cp.Counterparty, _filter.Contractor))));
      
      // Фильтр "Подразделение исполнителя".
      if (_filter.DepartmentPerformer != null)
        documents = documents.Where(c => c.ResponsibleEmployee != null && Equals(c.ResponsibleEmployee.Department, _filter.DepartmentPerformer));
      
      // Фильтр "Подразделение соисполнителя".
      if (_filter.DepartmentCoExecutor != null)
        documents = documents.Where(c => (Solution.Contracts.Is(c) && Solution.Contracts.As(c).CoExecutor != null && Equals(Solution.Contracts.As(c).CoExecutor.Department, _filter.DepartmentCoExecutor)) ||
                                    (SupAgreements.Is(c) && SupAgreements.As(c).CoExecutor != null && Equals(SupAgreements.As(c).CoExecutor.Department, _filter.DepartmentCoExecutor)) ||
                                    (DirRX.ContractsCustom.MemoForPayments.Is(c) && DirRX.ContractsCustom.MemoForPayments.As(c).CoExecutor != null && Equals(DirRX.ContractsCustom.MemoForPayments.As(c).CoExecutor.Department, _filter.DepartmentCoExecutor)));
      
      #region Фильтрация по дате договора
      
      DateTime? beginDate = null;
      DateTime? endDate = Calendar.UserToday;
      
      if (_filter.Last30days)
        beginDate = Calendar.UserToday.AddDays(-30);
      
      if (_filter.Last365days)
        beginDate = Calendar.UserToday.AddDays(-365);
      
      if (_filter.ManualPeriod)
      {
        beginDate = _filter.DateRangeFrom;
        endDate = _filter.DateRangeTo;
      }

      if (beginDate != null)
      {
        var serverPeriodBegin = Sungero.Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(beginDate.Value);
        documents = documents.WhereDocumentHistory(q => q.HistoryDate != null && q.HistoryDate >= beginDate.Value ||
                                                   q.HistoryDate == null && q.HistoryDate >= serverPeriodBegin);
      }
      
      if (endDate != null)
      {
        var serverPeriodEnd = endDate.Value.EndOfDay().FromUserTime();
        documents = documents.WhereDocumentHistory(q => q.HistoryDate != null && q.HistoryDate <= endDate.Value ||
                                                   q.HistoryDate == null && q.HistoryDate <= serverPeriodEnd);
      }
      
      #endregion
      
      // Фильтр по типу документа.
      if (_filter.Contracts || _filter.SupAgreements)
        documents = documents.Where(d => (_filter.Contracts && ContractBases.Is(d)) ||
                                    (_filter.SupAgreements && SupAgreements.Is(d)));
      
      #endregion
      
      return documents;
    }
  }

  partial class RegisterStandardFormsFolderHandlers
  {

    public virtual IQueryable<Sungero.Content.IElectronicDocumentTemplate> RegisterStandardFormsDataQuery(IQueryable<Sungero.Content.IElectronicDocumentTemplate> query)
    {
      return query.Where(t => t.DocumentType == Sungero.Contracts.Server.Contract.ClassTypeGuid ||
                         t.DocumentType == Sungero.Contracts.Server.SupAgreement.ClassTypeGuid);
    }
  }

  partial class RegistrationFolderHandlers
  {

    public virtual IQueryable<Sungero.Contracts.IContractualDocument> RegistrationDataQuery(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      // Выберем договоры и доп. соглашения на этапе регистрации.
      query = query.Where(d => (DirRX.Solution.Contracts.Is(d) && DirRX.Solution.Contracts.As(d).OnRegistration == true) ||
                          (DirRX.Solution.SupAgreements.Is(d) && DirRX.Solution.SupAgreements.As(d).OnRegistration == true));
      
      if (_filter == null)
        return query;
      
      #region Фильтры
      
      query = query
        // Фильтр "Контрагент".
        .Where(d => _filter.Counterparty == null ||
               (Solution.Contracts.Is(d) && Solution.Contracts.As(d).Counterparties.Any(
                 cp => Companies.Equals(cp.Counterparty, _filter.Counterparty))) ||
               (SupAgreements.Is(d) && SupAgreements.As(d).Counterparties.Any(
                 cp => Companies.Equals(cp.Counterparty, _filter.Counterparty))))
        .Where(d => _filter.Performer == null || Employees.Equals(_filter.Performer, d.ResponsibleEmployee));
      
      var contractActivate = new List<Enumeration>();
      if (_filter.ScanFlag)
        contractActivate.Add(Contract.ContractActivate.Copy);
      if (_filter.OriginalFlag)
        contractActivate.Add(Contract.ContractActivate.Original);
      
      if (contractActivate.Any())
        query = query.Where(q => (Solution.Contracts.Is(q) && Solution.Contracts.As(q).ContractActivate != null &&
                                  contractActivate.Contains(Solution.Contracts.As(q).ContractActivate.Value)) ||
                            (Solution.SupAgreements.Is(q) && Solution.SupAgreements.As(q).ContractActivate != null &&
                             contractActivate.Contains(Solution.SupAgreements.As(q).ContractActivate.Value)));
      #endregion
      
      return query;
    }
  }
  
  partial class CopyOnSigningFolderHandlers
  {

    public virtual IQueryable<Sungero.Contracts.IContractualDocument> CopyOnSigningDataQuery(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      // Фильтрация "Реестра Скан-копии на подписании"
      query = query
        // Фильтр "Контрагент".
        .Where(d => _filter.Counterparty == null ||
               (Solution.Contracts.Is(d) && Solution.Contracts.As(d).Counterparties.Any(
                 cp => Companies.Equals(cp.Counterparty, _filter.Counterparty))) ||
               (SupAgreements.Is(d) && SupAgreements.As(d).Counterparties.Any(
                 cp => Companies.Equals(cp.Counterparty, _filter.Counterparty))))
        .Where(d => _filter.Performer == null || Employees.Equals(_filter.Performer, d.ResponsibleEmployee))
        .Where(d => _filter.Signatory == null || Employees.Equals(_filter.Signatory, d.OurSignatory))
        .Where(d => (Solution.Contracts.Is(d) && (Solution.Contracts.As(d).IsContractorSignsFirst == true)) || (Solution.SupAgreements.Is(d) && (Solution.SupAgreements.As(d).IsContractorSignsFirst == true)))
        .Where(d => (Solution.Contracts.Is(d) && (Solution.Contracts.As(d).ContractActivate == DirRX.Solution.Contract.ContractActivate.Copy)) ||
               (Solution.SupAgreements.Is(d) && (Solution.SupAgreements.As(d).ContractActivate == DirRX.Solution.SupAgreement.ContractActivate.Copy)))
        .Where(d => (Solution.Contracts.Is(d) && (Solution.Contracts.As(d).ExternalApprovalState == Solution.Contract.ExternalApprovalState.Signed &&
                                                  Solution.Contracts.As(d).InternalApprovalState == Solution.Contract.InternalApprovalState.Signed)) ||
               (Solution.SupAgreements.Is(d) && (Solution.SupAgreements.As(d).ExternalApprovalState == DirRX.Solution.SupAgreement.ExternalApprovalState.Signed &&
                                                 Solution.SupAgreements.As(d).InternalApprovalState == DirRX.Solution.SupAgreement.InternalApprovalState.Signed)))
        .Where(d => (Solution.Contracts.Is(d) && (Solution.Contracts.As(d).OriginalSigning == null)) || (Solution.SupAgreements.Is(d) && (Solution.SupAgreements.As(d).OriginalSigning == null)))
        .Where(d => (Solution.Contracts.Is(d) && (Solution.Contracts.As(d).ContractorOriginalSigning == null)) ||
               (Solution.SupAgreements.Is(d) && (Solution.SupAgreements.As(d).ContractorOriginalSigning == null)))
        .Where(d => (Solution.Contracts.Is(d) &&
                     !Solution.Contracts.As(d).ScanMoveStatuses.Any(s => s.Status.Sid == ContractsCustom.PublicConstants.Module.ContractStatusGuid.ScanSignedByAllSidesGuid.ToString())) ||
               (Solution.SupAgreements.Is(d) &&
                !Solution.SupAgreements.As(d).ScanMoveStatuses.Any(s => s.Status.Sid == ContractsCustom.PublicConstants.Module.ContractStatusGuid.ScanSignedByAllSidesGuid.ToString())));
      
      return query;
    }
  }

  partial class OriginalsOnSigningFolderHandlers
  {

    public virtual IQueryable<Sungero.Contracts.IContractualDocument> OriginalsOnSigningDataQuery(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      query = query
        // Фильтр "Контрагент".
        .Where(d => _filter.Counterparty == null ||
               (Solution.Contracts.Is(d) && Solution.Contracts.As(d).Counterparties.Any(
                 cp => Companies.Equals(cp.Counterparty, _filter.Counterparty))) ||
               (SupAgreements.Is(d) && SupAgreements.As(d).Counterparties.Any(
                 cp => Companies.Equals(cp.Counterparty, _filter.Counterparty))))
        .Where(d => _filter.Performer == null || Employees.Equals(_filter.Performer, d.ResponsibleEmployee))
        .Where(d => _filter.Signatory == null || Employees.Equals(_filter.Signatory, d.OurSignatory))
        .Where(d => d.InternalApprovalState == Sungero.Docflow.OfficialDocument.InternalApprovalState.Signed)
        .Where(d => SupAgreements.Is(d) || (Solution.Contracts.Is(d) && (!Solution.ContractCategories.As(d.DocumentGroup).WorkWithOriginals.HasValue || Solution.ContractCategories.As(d.DocumentGroup).WorkWithOriginals != Solution.ContractCategory.WorkWithOriginals.NotSign)))
        .Where(d => (Solution.Contracts.Is(d) && (Solution.Contracts.As(d).OriginalSigning == null || Solution.Contracts.As(d).OriginalSigning == Contract.OriginalSigning.OnSigning)) ||
               (SupAgreements.Is(d) && (SupAgreements.As(d).OriginalSigning == null || SupAgreements.As(d).OriginalSigning == SupAgreement.OriginalSigning.OnSigning)))
        .Where(d => (Solution.Contracts.Is(d) && (Solution.Contracts.As(d).IsContractorSignsFirst == false || Solution.Contracts.As(d).ContractorOriginalSigning == Contract.ContractorOriginalSigning.Signed)) ||
               (SupAgreements.Is(d) && (SupAgreements.As(d).IsContractorSignsFirst == false || SupAgreements.As(d).ContractorOriginalSigning == SupAgreement.ContractorOriginalSigning.Signed)));
      
      return query;
    }
  }

  partial class OriginalsToBeReturnedFolderHandlers
  {

    public virtual IQueryable<Sungero.Contracts.IContractualDocument> OriginalsToBeReturnedDataQuery(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      var actionList = new List<Enumeration> {ContractTracking.Action.Endorsement, ContractTracking.Action.OriginalSend};
      
      DateTime? beginDate = null;
      DateTime? endDate = Calendar.UserToday;
      if (_filter.Last7Days)
        beginDate = Calendar.UserToday.AddDays(-7);
      if (_filter.Last30Days)
        beginDate = Calendar.UserToday.AddDays(-30);
      if (_filter.Last90Days)
        beginDate = Calendar.UserToday.AddDays(-90);
      if (_filter.CustomPeriod)
      {
        beginDate = _filter.DateRangeFrom;
        endDate = _filter.DateRangeTo;
      }
      
      var documents = query
        // Фильтр "Контрагент".
        .Where(d => _filter.Counterparty == null ||
               (Solution.Contracts.Is(d) && Solution.Contracts.As(d).Counterparties.Any(
                 cp => Companies.Equals(cp.Counterparty, _filter.Counterparty))) ||
               (SupAgreements.Is(d) && SupAgreements.As(d).Counterparties.Any(
                 cp => Companies.Equals(cp.Counterparty, _filter.Counterparty))))
        .Where(d => _filter.ResponsibleForReturn == null || Employees.Equals(_filter.ResponsibleForReturn, d.ResponsibleForReturnEmployee))
        .Where(d => SupAgreements.Is(d) || (Solution.Contracts.Is(d) && (!Solution.ContractCategories.As(d.DocumentGroup).WorkWithOriginals.HasValue || Solution.ContractCategories.As(d.DocumentGroup).WorkWithOriginals != Solution.ContractCategory.WorkWithOriginals.NotReturn)))
        .ToList()
        .Where(d => (Solution.Contracts.Is(d) &&
                     d.Tracking.Where(t => (!beginDate.HasValue || t.DeliveryDate.HasValue && t.DeliveryDate.Value >= beginDate.Value) &&
                                      (!endDate.HasValue || t.DeliveryDate.HasValue && t.DeliveryDate.Value <= endDate.Value) &&
                                      t.Action.HasValue && actionList.Contains(t.Action.Value) &&
                                      t.ReturnDeadline.HasValue && t.ReturnResult == null &&
                                      (t.IsOriginal == true || Solution.Contracts.As(d).IsContractorSignsFirst == true))
                     .Any())
               ||
               (SupAgreements.Is(d) &&
                d.Tracking.Where(t => (!beginDate.HasValue || t.DeliveryDate.HasValue && t.DeliveryDate.Value >= beginDate.Value) &&
                                 (!endDate.HasValue || t.DeliveryDate.HasValue && t.DeliveryDate.Value <= endDate.Value) &&
                                 t.Action.HasValue && actionList.Contains(t.Action.Value) &&
                                 t.ReturnDeadline.HasValue && t.ReturnResult == null &&
                                 (t.IsOriginal == true || SupAgreements.As(d).IsContractorSignsFirst == true))
                .Any()))
        .ToList();
      
      return query.Where(d => documents.Contains(d));
    }
  }

  partial class OriginalsToSendFolderHandlers
  {

    public virtual IQueryable<Sungero.Contracts.IContractualDocument> OriginalsToSendDataQuery(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      //TODO: при внесении изменений в ограничение списка документов, внести такие же в фильтрацию документов DocumentsDocumentFiltering в справочнике ShippingPackage.
      //TODO: при внесении изменений в ограничение списка документов, внести такие же в функции SetStatusOriginalWaitingForSending в договоре и ДС.
      var documents = query
        .Where(d => (Solution.Contracts.Is(d) && Solution.Contracts.As(d).OriginalSigning == Contract.OriginalSigning.Signed) ||
               (SupAgreements.Is(d) && SupAgreements.As(d).OriginalSigning == SupAgreement.OriginalSigning.Signed))
        // Фильтр "Контрагент".
        .Where(d => _filter.Counterparty == null ||
               (Solution.Contracts.Is(d) && Solution.Contracts.As(d).Counterparties.Any(
                 cp => Companies.Equals(cp.Counterparty, _filter.Counterparty))) ||
               (SupAgreements.Is(d) && SupAgreements.As(d).Counterparties.Any(
                 cp => Companies.Equals(cp.Counterparty, _filter.Counterparty))))
        // Фильтр "Количество контрагентов"
        .Where(d => _filter.AllFlag ||
               (_filter.SingleCounterpartyFlag && ((Solution.Contracts.Is(d) && Solution.Contracts.As(d).IsManyCounterparties != true) ||
                                                   (SupAgreements.Is(d) && SupAgreements.As(d).IsManyCounterparties != true))) ||
               (_filter.ManyCounterpartyFlag && ((Solution.Contracts.Is(d) && Solution.Contracts.As(d).IsManyCounterparties == true) ||
                                                 (SupAgreements.Is(d) && SupAgreements.As(d).IsManyCounterparties == true))))
        // Фильтр "Способ доставки".
        .Where(d => _filter.DeliveryMethod == null ||
               (Solution.Contracts.Is(d) && Solution.Contracts.As(d).Counterparties.Any(
                 cp => DirRX.Solution.MailDeliveryMethods.Equals(cp.DeliveryMethod, _filter.DeliveryMethod))) ||
               (SupAgreements.Is(d) && SupAgreements.As(d).Counterparties.Any(
                 cp => DirRX.Solution.MailDeliveryMethods.Equals(cp.DeliveryMethod, _filter.DeliveryMethod))))
        .Where(d => _filter.Performer == null || Employees.Equals(_filter.Performer, d.ResponsibleEmployee))
        .Where(d => _filter.Department == null || Departments.Equals(_filter.Department, d.Department))
        .ToList()
        // Зарегистрирован если первым подписывает контрагент (в независимости от типа активации) ИЛИ первым подписывает общество, контрагент подписывает скан копию, активация по сканам.
        // Во всех остальных случаях может быть не зарегистрирован.
        .Where(d => (Solution.Contracts.Is(d) && (!string.IsNullOrEmpty(d.RegistrationNumber) ||
                                                  (Solution.Contracts.As(d).IsContractorSignsFirst != true && !(Solution.Contracts.As(d).IsScannedImageSign == true && Solution.Contracts.As(d).ContractActivate == Contract.ContractActivate.Copy)))) ||
               (Solution.SupAgreements.Is(d) && (!string.IsNullOrEmpty(d.RegistrationNumber) ||
                                                 (Solution.SupAgreements.As(d).IsContractorSignsFirst != true && !(Solution.SupAgreements.As(d).IsScannedImageSign == true && Solution.SupAgreements.As(d).ContractActivate == SupAgreement.ContractActivate.Copy)))))
        // На закладке «Выдача» нет строки «Отправка контрагенту» с признаком «Оригинал», документ не включен в пакет на отправку или установлен признак Повторная отправка.
        .Where(d => (Solution.Contracts.Is(d) && ((!d.Tracking.Where(t => t.Action == Sungero.Docflow.OfficialDocumentTracking.Action.Sending && t.IsOriginal == true).Any() &&
                                                   !Solution.Contracts.As(d).OriginalMoveStatuses.Any(s => s.Status.Sid == ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalAcceptedForSendingGuid.ToString() ||
                                                                                                      s.Status.Sid == ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalPlacedForSendingGuid.ToString())) ||
                                                  Solution.Contracts.As(d).Resending == true)) ||
               (SupAgreements.Is(d) && ((!d.Tracking.Where(t => t.Action == Sungero.Docflow.OfficialDocumentTracking.Action.Sending && t.IsOriginal == true).Any() &&
                                         !Solution.SupAgreements.As(d).OriginalMoveStatuses.Any(s => s.Status.Sid == ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalAcceptedForSendingGuid.ToString() ||
                                                                                                s.Status.Sid == ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalPlacedForSendingGuid.ToString())) ||
                                        SupAgreements.As(d).Resending == true)))
        .ToList();
      
      return query.Where(d => documents.Contains(d));
    }
  }

  partial class ContractsListFolderHandlers
  {

    public override IQueryable<Sungero.Contracts.IContractualDocument> ContractsListDataQuery(IQueryable<Sungero.Contracts.IContractualDocument> query)
    {
      var documents = query.Where(d => ContractBases.Is(d) || SupAgreements.Is(d) || MemoForPayments.Is(d));
      
      if (_filter == null)
        return documents;
      
      #region Фильтры
      
      // Фильтр по состоянию.
      var statuses = new List<Enumeration>();
      if (_filter.Draft)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Draft);
      if (_filter.Active)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Active);
      if (_filter.Closed)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Closed);
      if (_filter.Obsolete)
        statuses.Add(Sungero.Contracts.ContractBase.LifeCycleState.Obsolete);
      if (_filter.OpenSAP)
        statuses.Add(DirRX.Solution.Contract.LifeCycleState.OpenSAP);
      if (_filter.Deleted)
        statuses.Add(DirRX.Solution.Contract.LifeCycleState.Deleted);
      
      // Фильтр по состоянию.
      if (statuses.Any())
        documents = documents.Where(q => q.LifeCycleState != null && statuses.Contains(q.LifeCycleState.Value));
      
      // Фильтр "Вид документа".
      if (_filter.DocumentKind != null)
        documents = documents.Where(c => Equals(c.DocumentKind, _filter.DocumentKind) ||
                                    (SupAgreements.Is(c) && SupAgreements.As(c).LeadingDocument != null &&
                                     Equals(SupAgreements.As(c).LeadingDocument.DocumentKind, _filter.DocumentKind)));
      
      // Фильтр "Категория".
      if (_filter.Category != null)
        documents = documents.Where(c => (ContractBases.Is(c) && Equals(c.DocumentGroup, _filter.Category)) ||
                                    (SupAgreements.Is(c) && SupAgreements.As(c).LeadingDocument != null &&
                                     Equals(SupAgreements.As(c).LeadingDocument.DocumentGroup, _filter.Category)));
      
      // Фильтр "Контрагент".
      if (_filter.Contractor != null)
        documents = documents.Where(c => (Solution.Contracts.Is(c) && Solution.Contracts.As(c).Counterparties.Any(cp => Companies.Equals(cp.Counterparty, _filter.Contractor))) ||
                                    (SupAgreements.Is(c) && SupAgreements.As(c).Counterparties.Any(cp => Companies.Equals(cp.Counterparty, _filter.Contractor))));
      
      // Фильтр "Подразделение исполнителя".
      if (_filter.DepartmentPerformer != null)
        documents = documents.Where(c => c.ResponsibleEmployee != null && Equals(c.ResponsibleEmployee.Department, _filter.DepartmentPerformer));
      
      // Фильтр "Подразделение соисполнителя".
      if (_filter.DepartmentCoExecutor != null)
        documents = documents.Where(c => (Solution.Contracts.Is(c) && Solution.Contracts.As(c).CoExecutor != null && Equals(Solution.Contracts.As(c).CoExecutor.Department, _filter.DepartmentCoExecutor)) ||
                                    (SupAgreements.Is(c) && SupAgreements.As(c).CoExecutor != null && Equals(SupAgreements.As(c).CoExecutor.Department, _filter.DepartmentCoExecutor)) ||
                                    (DirRX.ContractsCustom.MemoForPayments.Is(c) && DirRX.ContractsCustom.MemoForPayments.As(c).CoExecutor != null && Equals(DirRX.ContractsCustom.MemoForPayments.As(c).CoExecutor.Department, _filter.DepartmentCoExecutor)));
      
      #region Фильтрация по дате договора
      
      DateTime? beginDate = null;
      DateTime? endDate = Calendar.UserToday;
      
      if (_filter.Last30days)
        beginDate = Calendar.UserToday.AddDays(-30);
      
      if (_filter.Last365days)
        beginDate = Calendar.UserToday.AddDays(-365);
      
      if (_filter.ManualPeriod)
      {
        beginDate = _filter.DateRangeFrom;
        endDate = _filter.DateRangeTo;
      }

      if (beginDate != null)
      {
        var serverPeriodBegin = Sungero.Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(beginDate.Value);
        documents = documents.Where(q => q.RegistrationDate != null && q.RegistrationDate >= beginDate.Value ||
                                    q.RegistrationDate == null && q.Created >= serverPeriodBegin);
      }
      
      if (endDate != null)
      {
        var serverPeriodEnd = endDate.Value.EndOfDay().FromUserTime();
        documents = documents.Where(q => q.RegistrationDate != null && q.RegistrationDate <= endDate.Value ||
                                    q.RegistrationDate == null && q.Created <= serverPeriodEnd);
      }
      
      #endregion
      
      // Фильтр по функциональности договора.
      var functionality = new List<Enumeration>();
      if (_filter.Sale)
        functionality.Add(Solution.Contract.ContractFunctionality.Sale);
      if (_filter.Purchase)
        functionality.Add(Solution.Contract.ContractFunctionality.Purchase);
      if (_filter.Mixed)
        functionality.Add(Solution.Contract.ContractFunctionality.Mixed);
      
      if (functionality.Any())
        documents = documents.Where(q => Solution.Contracts.Is(q) && Solution.Contracts.As(q).ContractFunctionality != null &&
                                    functionality.Contains(Solution.Contracts.As(q).ContractFunctionality.Value));
      
      // Фильтр по типу документа.
      if (_filter.Contracts || _filter.SupAgreements || _filter.MemoForPayment)
        documents = documents.Where(d => (_filter.Contracts && ContractBases.Is(d)) ||
                                    (_filter.SupAgreements && SupAgreements.Is(d)) ||
                                    (_filter.MemoForPayment && MemoForPayments.Is(d)));
      
      // Фильтр по заключенным договорам.
      if (_filter.ByTender || _filter.NotByTender)
        documents = documents.Where(d =>(_filter.ByTender && Solution.Contracts.Is(d) && Solution.Contracts.As(d).IsTender == true) ||
                                    (_filter.NotByTender && Solution.Contracts.Is(d) && Solution.Contracts.As(d).IsTender != true));
      
      #endregion
      
      return documents;
    }
  }

  partial class ContractsUIHandlers
  {
  }
}