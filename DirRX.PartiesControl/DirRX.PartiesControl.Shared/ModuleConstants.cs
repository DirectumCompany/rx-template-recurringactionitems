using System;
using Sungero.Core;

namespace DirRX.PartiesControl.Constants
{
  public static class Module
  {
    /// <summary>
    /// Роль "Ответственный за архив документов контрагентов".
    /// </summary>
    [Public]
    public static readonly Guid ArchiveResponsibleRole = Guid.Parse("2281A5EF-8154-4A16-97D3-F3F45EBE43A3");
    
    /// <summary>
    /// Роль "Сотрудники службы безопасности".
    /// </summary>
    [Public]
    public static readonly Guid SecurityServiceRole = Guid.Parse("83B703A9-77BE-4DD4-AEB3-695F0107A2A0");
    
    /// <summary>
    /// Роль "Ответственный за КССС"
    /// </summary>
    [Public]
    public static readonly Guid KsssResponsibleRole = Guid.Parse("847BD597-A45C-434D-8229-1AE66221968E");

    /// <summary>
    /// Роль "Руководитель клиентского сервиса"
    /// </summary>
    [Public]
    public static readonly Guid ClientServiceManagerRole = Guid.Parse("ADC9EF64-D894-4BDA-B4C4-A19539E71362");
    
    /// <summary>
    /// Роль "Сотрудник ЕЦД"
    /// </summary>
    [Public]
    public static readonly Guid ServiceECDRole = Guid.Parse("6F4456A0-AE66-45D1-9EDC-E09F0BD04A83");
    
    /// <summary>
    /// Роль "Сотрудник казначейства"
    /// </summary>
    [Public]
    public static readonly Guid ServiceTreasuryDepartmentRole = Guid.Parse("4566A94C-879C-44DA-8C3F-BD7C4307365E");

    /// <summary>
    /// Роль "Ответственные за настройку модуля Контрагенты"
    /// </summary>
    [Public]
    public static readonly Guid CounterpartiesModuleRole = Guid.Parse("807C5948-BE09-49EB-B7C7-E65A4015E925");
    
    /// <summary>
    /// Роль "Специалист по комплаенс"
    /// </summary>
    [Public]
    public static readonly Guid ComplianceSpecialistRole = Guid.Parse("8134C214-FDC9-445C-9DAD-790E8CC9EF69");
    
    
    
    /// <summary>
    /// Роль "Уполномоченные лица по заполнению специальных полей в карточке контрагента"
    /// </summary>
    [Public]
    public static readonly Guid SpecialFieldsRole = Guid.Parse("7D0E29E4-1B3B-4559-BAA9-208A9D4D1AD8");
    
    /// <summary>
    /// Роль "Сотрудники, уполномоченные получать уведомления при работе со Стоп-листом"
    /// </summary>
    [Public]
    public static readonly Guid StopListNoticeRole = Guid.Parse("2832D722-79AC-47B9-81CA-2C062AFB2E30");
    
    /// <summary>
    /// Роль "Исполнитель задачи с уведомлением с отчётом для ГД"
    /// </summary>
    [Public]
    public static readonly Guid CEOReportAssigneeRole = Guid.Parse("2B43C7CC-51D5-4DFA-BE95-6B81126FC3DE");
    
    // Guid вида документа "Заявка на проверку контрагента".
    [Public]
    public static readonly Guid RevisionRequestKind = Guid.Parse("F697777B-7D56-4009-B806-68DC0F6A2173");
    
    // Guid вида документа "Анкета контрагента".
    [Public]
    public static readonly Guid CounterpartyInformationKind = Guid.Parse("8221DCB8-9E90-4BD1-A826-A00D5C664BAC");
    
    // Guid вида документа "Анкета контрагента".
    [Public]
    public static readonly Guid CounterpartyDocumentTypeGuid = Guid.Parse("49d0c5e7-7069-44d2-8eb6-6e3098fc8b10");

    // Имя типа связи "Приложение".
    public const string AddendumRelationName = "Addendum";
    
    public const string NotificationDatabaseKey = "LastNotificationOfTransferringOriginals";
    
    public const string SupervisorTask = "SupervisorTask";
    public const string InitiatorTask = "InitiatorTask";
    
    #region Константы по умолчанию.
    
    /// <summary>
    /// Имя константы "Срок формирования уведомления инициатору заявки на проверку о передаче оригиналов в архив".
    /// </summary>
    public const string InitiatorMonthCountName = "Срок формирования уведомления инициатору заявки на проверку о передаче оригиналов в архив";
    
    [Public]
    /// <summary>
    /// GUID константы "Балансовая стоимость активов".
    /// </summary>
    public static readonly Guid InitiatorMonthCountGuid = Guid.Parse("88C62B31-B895-4896-93E4-439C89D13D19");
    
    /// <summary>
    /// Имя константы "Срок формирования уведомления куратору заявки на проверку о передаче оригиналов в архив".
    /// </summary>
    public const string SupervisorMonthCountName = "Срок формирования уведомления куратору заявки на проверку о передаче оригиналов в архив";
    
    [Public]
    /// <summary>
    /// GUID константы "Балансовая стоимость активов".
    /// </summary>
    public static readonly Guid SupervisorMonthCountGuid = Guid.Parse("4BD28293-AF68-4A6E-98F1-18CC1F1A1A21");
    
    #endregion
  }
}