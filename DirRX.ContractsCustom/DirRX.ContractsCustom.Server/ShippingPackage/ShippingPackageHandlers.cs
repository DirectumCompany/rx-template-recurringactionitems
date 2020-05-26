using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ShippingPackage;
using DirRX.Solution;

namespace DirRX.ContractsCustom
{
  partial class ShippingPackageDocumentsDocumentPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentsDocumentFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      #region Фильтр списка "Оригиналы для отправки".
      query = query.Where(d => (Contracts.Is(d) && Contracts.As(d).OriginalSigning == DirRX.Solution.Contract.OriginalSigning.Signed) ||
                          (SupAgreements.Is(d) && SupAgreements.As(d).OriginalSigning == DirRX.Solution.SupAgreement.OriginalSigning.Signed));
      #endregion
      
      // Ограничим документы по контрагенту, способу доставки, адресу и контакту.
      query = query.Where(d =>
                          (Contracts.Is(d) && Contracts.As(d).Counterparties.Any
                           (cp => (_obj.ShippingPackage.Counterparty == null || Solution.Companies.Equals(cp.Counterparty, _obj.ShippingPackage.Counterparty)) &&
                            (_obj.ShippingPackage.DeliveryMethod == null || Solution.MailDeliveryMethods.Equals(cp.DeliveryMethod, _obj.ShippingPackage.DeliveryMethod)) &&
                            (_obj.ShippingPackage.ShippingAddress == null || PartiesControl.ShippingAddresses.Equals(cp.Address, _obj.ShippingPackage.ShippingAddress)) &&
                            ((_obj.ShippingPackage.Contact != null && Sungero.Parties.Contacts.Equals(cp.Contact, _obj.ShippingPackage.Contact)) ||
                             (_obj.ShippingPackage.Contact == null && cp.Contact == null)))) ||
                          (SupAgreements.Is(d) && SupAgreements.As(d).Counterparties.Any
                           (cp => (_obj.ShippingPackage.Counterparty == null || Solution.Companies.Equals(cp.Counterparty, _obj.ShippingPackage.Counterparty)) &&
                            (_obj.ShippingPackage.DeliveryMethod == null || Solution.MailDeliveryMethods.Equals(cp.DeliveryMethod, _obj.ShippingPackage.DeliveryMethod)) &&
                            (_obj.ShippingPackage.ShippingAddress == null || PartiesControl.ShippingAddresses.Equals(cp.Address, _obj.ShippingPackage.ShippingAddress)) &&
                            ((_obj.ShippingPackage.Contact != null && Sungero.Parties.Contacts.Equals(cp.Contact, _obj.ShippingPackage.Contact)) ||
                             (_obj.ShippingPackage.Contact == null && cp.Contact == null)))));
      
      var documentsInPackages = DirRX.ContractsCustom.ShippingPackages.GetAll().ToList().SelectMany(d => d.Documents.Select(x => x.Document)).ToList();

      #region Фильтр списка "Оригиналы для отправки".
      query = query.ToList()
        // Зарегистрирован если первым подписывает контрагент (в независимости от типа активации) ИЛИ первым подписывает общество, контрагент подписывает скан копию, активация по сканам.
        // Во всех остальных случаях может быть не зарегистрирован.
        .Where(d => (Solution.Contracts.Is(d) && (!string.IsNullOrEmpty(d.RegistrationNumber) ||
                                                  (Solution.Contracts.As(d).IsContractorSignsFirst != true && !(Solution.Contracts.As(d).IsScannedImageSign == true && Solution.Contracts.As(d).ContractActivate == Solution.Contract.ContractActivate.Copy)))) ||
               (Solution.SupAgreements.Is(d) && (!string.IsNullOrEmpty(d.RegistrationNumber) ||
                                                 (Solution.SupAgreements.As(d).IsContractorSignsFirst != true && !(Solution.SupAgreements.As(d).IsScannedImageSign == true && Solution.SupAgreements.As(d).ContractActivate == Solution.SupAgreement.ContractActivate.Copy)))))
        // На закладке «Выдача» нет строки «Отправка контрагенту» с признаком «Оригинал» или установлен признак Повторная отправка.
        .Where(d => (!d.Tracking.Where(t => t.Action == Sungero.Docflow.OfficialDocumentTracking.Action.Sending && t.IsOriginal == true).Any() ||
                     (Solution.Contracts.Is(d) && Solution.Contracts.As(d).Resending == true) ||
                     (SupAgreements.Is(d) && SupAgreements.As(d).Resending == true)))
        .Where(d => (Solution.Contracts.Is(d) && Solution.Contracts.As(d).Resending == true) ||
               (SupAgreements.Is(d) && SupAgreements.As(d).Resending == true) ||
               (!documentsInPackages.Contains(d))).AsQueryable();
      #endregion

      return query;
    }
  }

  partial class ShippingPackageFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      // Проверка того, что панель фильтрации включена.
      if (_filter == null)
        return query;
      #region Фильтры.
      #region Состояние.
      if (_filter.InitFlag || _filter.AcceptedFlag || _filter.SentFlag)
      {
        query = query.Where(q => ((_filter.InitFlag && q.PackageStatus == PackageStatus.Init) ||
                                  (_filter.AcceptedFlag && q.PackageStatus == PackageStatus.Accepted) ||
                                  (_filter.SentFlag && q.PackageStatus == PackageStatus.Sent)
                                 ));
      }
      #endregion
      #region Способ доставки.
      if (_filter.DeliveryMethod != null)
        query = query.Where(q => DirRX.Solution.MailDeliveryMethods.Equals(q.DeliveryMethod, _filter.DeliveryMethod));
      #endregion
      #region Контрагент.
      if (_filter.Counterparty != null)
        query = query.Where(q => DirRX.Solution.Companies.Equals(q.Counterparty, _filter.Counterparty));
      #endregion
      #region Период дат.
      var periodBegin = Calendar.UserToday.AddDays(-30);
      var periodEnd = Calendar.UserToday.EndOfDay();
      
      if (_filter.WeekFlag)
        periodBegin = Calendar.UserToday.AddDays(-7);
      
      if (_filter.MonthFlag)
        periodBegin = Calendar.UserToday.AddDays(-30);
      
      if (_filter.QuarterFlag)
        periodBegin = Calendar.UserToday.AddDays(-90);
      
      if (_filter.PeriodFlag)
      {
        periodBegin = _filter.DateRangeFrom ?? Calendar.SqlMinValue;
        periodEnd = _filter.DateRangeTo ?? Calendar.SqlMaxValue;
      }
      query = query.Where(q => q.Date.Between(periodBegin, periodEnd));
      #endregion
      #endregion
      return query;
    }
  }


  partial class ShippingPackageServerHandlers
  {

    public override void BeforeDelete(Sungero.Domain.BeforeDeleteEventArgs e)
    {
      foreach (var doc in _obj.Documents.Select(d => d.Document))
      {
        // Запись в очередь на удаление статуса договора "Оригинал документа помещен в пакет для отправки".
        var removeItem = ContractsCustom.ContractQueueItems.Create();
        removeItem.DocumentId = doc.Id;
        removeItem.ContractStatusAction = ContractsCustom.PublicConstants.Module.StatusAction.RemoveAction;
        removeItem.ContractStatusType = ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus;
        removeItem.ContractStatusSid = ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalPlacedForSendingGuid.ToString();
        removeItem.Save();
      }
      // Запустить агент установки статусов договоров.
      ContractsCustom.Jobs.SetContractStatusInPackagesJob.Enqueue();
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Наименование заполняется автоматически после сохранения карточки по маске «<Контрагент>. Адрес: <Адрес отправки>. Дата: <Дата формирования пакета>»
      var counterpartyName = _obj.Counterparty != null ? _obj.Counterparty.Name : string.Empty;
      var shippingAddress = _obj.ShippingAddress != null ? _obj.ShippingAddress.Name : string.Empty;
      _obj.Name = DirRX.ContractsCustom.ShippingPackages.Resources.FormatNameFormat(counterpartyName, shippingAddress, _obj.Date.HasValue ? _obj.Date.Value.ToString("d") : string.Empty);
      if (_obj.Name.Length > _obj.Info.Properties.Name.Length)
        _obj.Name = _obj.Name.Remove(_obj.Info.Properties.Name.Length);
      
      if (_obj.State.Properties.PackageStatus.PreviousValue != _obj.PackageStatus && _obj.PackageStatus == PackageStatus.Accepted)
      {
        foreach (var doc in _obj.Documents.Select(d => d.Document))
        {
          // Запись в очередь на удаление статуса договора "Оригинал документа помещен в пакет для отправки".
          var removeItem = ContractsCustom.ContractQueueItems.Create();
          removeItem.DocumentId = doc.Id;
          removeItem.ContractStatusAction = ContractsCustom.PublicConstants.Module.StatusAction.RemoveAction;
          removeItem.ContractStatusType = ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus;
          removeItem.ContractStatusSid = ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalPlacedForSendingGuid.ToString();
          removeItem.Save();
          
          // Запись в очередь на установку статуса договора "Оригинал документа принят к отправке".
          var addItem = ContractsCustom.ContractQueueItems.Create();
          addItem.DocumentId = doc.Id;
          addItem.ContractStatusAction = ContractsCustom.PublicConstants.Module.StatusAction.AddAction;
          addItem.ContractStatusType = ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus;
          addItem.ContractStatusSid = ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalAcceptedForSendingGuid.ToString();
          addItem.Save();
        }
        // Запустить агент установки статусов договоров.
        ContractsCustom.Jobs.SetContractStatusInPackagesJob.Enqueue();
      }
      
      if (_obj.State.Properties.PackageStatus.PreviousValue != _obj.PackageStatus && _obj.PackageStatus == PackageStatus.Sent)
      {
        foreach (var doc in _obj.Documents.Select(d => d.Document))
        {
          // Запись в очередь на удаление статуса договора "Оригинал документа принят к отправке".
          var removeAcceptedItem = ContractsCustom.ContractQueueItems.Create();
          removeAcceptedItem.DocumentId = doc.Id;
          removeAcceptedItem.ContractStatusAction = ContractsCustom.PublicConstants.Module.StatusAction.RemoveAction;
          removeAcceptedItem.ContractStatusType = ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus;
          removeAcceptedItem.ContractStatusSid = ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalAcceptedForSendingGuid.ToString();
          removeAcceptedItem.Save();
          
          // Запись в очередь на удаление статуса договора "Ожидает отправки контрагенту".
          var removeWaitingItem = ContractsCustom.ContractQueueItems.Create();
          removeWaitingItem.DocumentId = doc.Id;
          removeWaitingItem.ContractStatusAction = ContractsCustom.PublicConstants.Module.StatusAction.RemoveAction;
          removeWaitingItem.ContractStatusType = ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus;
          removeWaitingItem.ContractStatusSid = ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalWaitingForSendingGuid.ToString();
          removeWaitingItem.Save();
          
          // Запись в очередь на установку статуса договора "Оригинал документа отправлен контрагенту".
          var addItem = ContractsCustom.ContractQueueItems.Create();
          addItem.DocumentId = doc.Id;
          addItem.ContractStatusAction = ContractsCustom.PublicConstants.Module.StatusAction.AddAction;
          addItem.ContractStatusType = ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus;
          addItem.ContractStatusSid = ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalSendedToCounterpartyGuid.ToString();
          addItem.Save();
        }
        
        // Запустить агент установки статусов договоров.
        ContractsCustom.Jobs.SetContractStatusInPackagesJob.Enqueue();
      }
      
      // Если вручную добавили/удалили документы в пакете.
      if (!_obj.State.IsInserted && _obj.PackageStatus == PackageStatus.Init)
      {
        foreach (var row in _obj.State.Properties.Documents.Added)
        {
          // Запись в очередь на установку статуса договора "Оригинал документа помещен в пакет для отправки".
          var addItem = ContractsCustom.ContractQueueItems.Create();
          addItem.DocumentId = row.Document.Id;
          addItem.ContractStatusAction = ContractsCustom.PublicConstants.Module.StatusAction.AddAction;
          addItem.ContractStatusType = ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus;
          addItem.ContractStatusSid = ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalPlacedForSendingGuid.ToString();
          addItem.Save();
        }
        
        // Если документы удалили, то удалим статус.
        var deletedRows = _obj.State.Properties.Documents.Deleted;
        foreach (var row in _obj.State.Properties.Documents.Deleted)
        {
          // Запись в очередь на удаление статуса договора "Оригинал документа помещен в пакет для отправки".
          var removeItem = ContractsCustom.ContractQueueItems.Create();
          removeItem.DocumentId = row.Document.Id;
          removeItem.ContractStatusAction = ContractsCustom.PublicConstants.Module.StatusAction.RemoveAction;
          removeItem.ContractStatusType = ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus;
          removeItem.ContractStatusSid = ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalPlacedForSendingGuid.ToString();
          removeItem.Save();
        }
        // Запустить агент установки статусов договоров.
        ContractsCustom.Jobs.SetContractStatusInPackagesJob.Enqueue();
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      // До сохранения карточки в поле отображается подсказка «Имя будет сформировано автоматически».
      _obj.Name = DirRX.ContractsCustom.ShippingPackages.Resources.DefaultObjName;
      // Дата формирования пакета – устанавливается автоматически датой создания пакета.
      _obj.Date = Calendar.Today;
      // При создании нового пакета устанавливается статус «Новый».
      _obj.PackageStatus = PackageStatus.Init;
      // Наш контакт заполняется автоматически сотрудником, сформировавшим данный пакет.
      _obj.Employee = Sungero.Company.Employees.Current;
    }
  }

}