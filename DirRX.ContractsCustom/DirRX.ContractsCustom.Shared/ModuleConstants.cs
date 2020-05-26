using System;
using Sungero.Core;

namespace DirRX.ContractsCustom.Constants
{
  public static class Module
  {

    /// <summary>
    /// Значение по умолчанию для "Напоминать об окончании за мес.".
    /// </summary>
    [Public]
    public const int MonthsToFinishWorks = 2;
    
    // Код страны Российская федерация.
    [Public]
    public const string RussianFederationCountryCode = "643";
    
    // 6 пробелов для индекса
    public const string Spaces6 = "      ";
    
    // Имя параметра в таблице Sungero_Docflow_Params для фиксации последнего выполнения фонового процесса Перевод договора/ДС в состояние Исполнен.
    public const string LastSetContractExecutedDateTimeDocflowParamName = "SetContractExecutedDateTime";
    
    /// <summary>
    /// GUID для системного контрагента - "Контрагент для тендерных договоров закупки".
    /// </summary>
    public static readonly Guid TenderPurchaseCounterpartyGuid = Guid.Parse("4CB30011-9FFC-4927-A5D5-4BA99CAEED4E");

    #region Константы по умолчанию.

    // Имя константы "Общий срок договора и ДС".
    public const string GeneralPeriodContractAndAdditAgreementName = "Общий срок договора и ДС";

    // GUID константы "Общий срок договора и ДС".
    [Public]
    public static readonly Guid GeneralPeriodContractAndAdditAgreementGuid = Guid.Parse("E0C3AC34-2DDA-4A93-BF9E-316732CB7576");

    // Имя константы "Срок досыла оригинала".
    public const string OriginalDeadlineName = "Срок досыла оригинала";

    // GUID константы "Срок досыла оригинала".
    [Public]
    public static readonly Guid OriginalDeadlineGuid = Guid.Parse("B79154D5-149E-4D74-9D1C-C26F63BF6B77");
    
    // Имя константы "Срок формирования задачи для обеспечения возврата оригиналов исполнителем".
    public const string SendToPerformerConstantName = "Срок формирования задачи для обеспечения возврата оригиналов исполнителем";

    // GUID константы "Срок формирования задачи для обеспечения возврата оригиналов исполнителем".
    [Public]
    public static readonly Guid SendToPerformerConstantGuid = Guid.Parse("4027D174-1694-4DA5-B646-1BE6D0BA68F2");
    
    // Имя константы "Срок формирования задачи для обеспечения возврата оригиналов куратором".
    public const string SendToSupervisorConstantName = "Срок формирования задачи для обеспечения возврата оригиналов куратором";

    // GUID константы "Срок формирования задачи для обеспечения возврата оригиналов куратором".
    [Public]
    public static readonly Guid SendToSupervisorConstantGuid = Guid.Parse("794F6210-EB7C-4FB7-B02A-6F5E3A496B5B");
    
    // Имя константы "Срок формирования задачи для обеспечения возврата оригиналов руководителем в прямом подчинении ГД".
    public const string SendToFirstManagerConstantName = "Срок формирования задачи для обеспечения возврата оригиналов руководителем в прямом подчинении ГД";

    // GUID константы "Срок формирования задачи для обеспечения возврата оригиналов руководителем в прямом подчинении ГД".
    [Public]
    public static readonly Guid SendToFirstManagerConstantGuid = Guid.Parse("C6A4FDE0-D99E-419D-92E6-947572A2C806");
    
    // Имя константы "Срок задачи обеспечения возврата оригиналов".
    public const string OriginalsControlTaskDeadlineConstantName = "Срок задачи обеспечения возврата оригиналов";

    // GUID константы "Срок задачи обеспечения возврата оригиналов".
    [Public]
    public static readonly Guid OriginalsControlTaskDeadlineConstantGuid = Guid.Parse("47A63214-A69B-4681-8A5E-975AFE82F29C");
    
    /// <summary>
    /// Имя константы "Срок повторного задания об истечении срока действия договора/ДС".
    /// </summary>
    public const string ContractExecutedRemindName = "Срок повторного задания об истечении срока действия договора/ДС";
    
    /// <summary>
    /// GUID константы "Срок повторного задания об истечении срока действия договора/ДС".
    /// </summary>
    [Public]
    public static readonly Guid ContractExecutedRemindGuid = Guid.Parse("30A2368C-3118-42FC-96F8-0093296B7469");
    
    /// <summary>
    /// Имя константы "Срок задачи на вложение документов подтверждающих наступление условий активации".
    /// </summary>
    public const string ConfirmActivationConditionTaskDeadlineName = "Срок задачи на вложение документов подтверждающих наступление условий активации";
    
    [Public]
    /// <summary>
    /// GUID константы "Срок задачи на вложение документов подтверждающих наступление условий активации".
    /// </summary>
    public static readonly Guid ConfirmActivationConditionTaskDeadlineGuid = Guid.Parse("0E172774-6096-45B9-A909-241576DC2F5C");
    
    /// <summary>
    /// Имя константы "Срок разработки типовой формы договора".
    /// </summary>
    [Public]
    public const string TermDevelopNewContractFormName = "Срок разработки типовой формы договора";
    
    /// <summary>
    /// GUID константы "Срок разработки типовой формы договора".
    /// </summary>
    [Public]
    public static readonly Guid TermDevelopNewContractFormGuid = Guid.Parse("BB6E716D-F2EF-45ED-BB96-00E0AD8D3184");
    
    /// <summary>
    /// Имя константы "Максимальная сумма договора".
    /// </summary>
    public const string ContractMaxAmountName = "Максимальная сумма договора";
    
    [Public]
    /// <summary>
    /// GUID константы "Максимальная сумма договора".
    /// </summary>
    public static readonly Guid ContractMaxAmountGuid = Guid.Parse("EE5FE9B0-122B-4BFA-AFA3-1A392A8AC172"); 
    
    /// <summary>
    /// Имя константы "Сумма для получения корпоративного одобрения, 25% от балансовой стоимости".
    /// </summary>
    public const string CorporateApprovalAmountName = "Сумма для получения корпоративного одобрения";
    
    [Public]
    /// <summary>
    /// GUID константы "Сумма для получения корпоративного одобрения, 25% от балансовой стоимости".
    /// </summary>
    public static readonly Guid CorporateApprovalAmountGuid = Guid.Parse("D690C5AB-D83A-4D73-A71B-8A7B8CF25CDF"); 
    
    /// <summary>
    /// Имя константы "Балансовая стоимость активов".
    /// </summary>
    public const string BookValueAssetsName = "Балансовая стоимость активов Общества";
    
    [Public]
    /// <summary>
    /// GUID константы "Балансовая стоимость активов".
    /// </summary>
    public static readonly Guid BookValueAssetsGuid = Guid.Parse("E2013D4C-675D-4BAC-8E07-99D186D21BA9"); 
    
    /// <summary>
    /// Имя константы "Срок задачи на подтверждение отправки документа".
    /// </summary>
    public const string SendWithResposibleDeadlineName = "Срок задачи на подтверждение отправки документа";
    
    [Public]
    /// <summary>
    /// GUID константы "Срок задачи на подтверждение отправки документа".
    /// </summary>
    public static readonly Guid SendWithResposibleDeadlineGuid = Guid.Parse("888BBC1E-69D4-483A-9198-A44CF667E32D"); 
    
    /// <summary>
    /// Имя константы "Срок задачи на подтверждение завершения договора".
    /// </summary>
    public const string ConfirmContractExecutedDeadlineName = "Срок задачи на подтверждение завершения договора";
    
    /// <summary>
    /// GUID константы "Срок задачи на подтверждение завершения договора".
    /// </summary>
    [Public]
    public static readonly Guid ConfirmContractExecutedDeadlineGuid = Guid.Parse("0D121497-CD42-455D-BFE3-91C3CBF9E9FC");

    // Имя константы "Срок задачи на отправку договора в ИУС ЛЛК".
    [Public]
    public const string SendToIMSConstantName = "Срок задачи на отправку договора в ИУС ЛЛК";
    
    // GUID константы "Срок задачи на отправку договора в ИУС ЛЛК".
    [Public]
    public static readonly Guid SendToIMSConstantGuid = Guid.Parse("B204F6F9-CB94-41C6-A8B1-159E5DEC3099");
    
    #endregion
    
    #region Статусы договоров по умолчанию.
    /// <summary>
    /// Действия над статусами договоров.
    /// </summary>
    [Public]
    public static class StatusAction
    {
      /// <summary>
      /// Тип статуса договора "Проверка контрагента".
      /// </summary>
      [Public]
      public const string AddAction = "Add";
      
      /// <summary>
      /// Тип статуса договора "Проверка контрагента".
      /// </summary>
      [Public]
      public const string RemoveAction = "Remove";
    }
    
    /// <summary>
    /// Типы статусов договоров.
    /// </summary>
    [Public]
    public static class ContractStatusType
    {
      /// <summary>
      /// Тип статуса договора "Проверка контрагента".
      /// </summary>
      [Public]
      public const string ApprovalStatus = "Статус согласования";
      
      /// <summary>
      /// Тип статуса договора "Проверка контрагента".
      /// </summary>
      [Public]
      public const string ScanMoveStatus = "Статус движения скан-копий";
      
      /// <summary>
      /// Тип статуса договора "Проверка контрагента".
      /// </summary>
      [Public]
      public const string OriginalMoveStatus = "Статус движения оригиналов";
    }
    
    /// <summary>
    /// GUID`ы статусов договоров.
    /// </summary>
    [Public]
    public static class ContractStatusGuid
    {
      #region Статусы согласования.
      
      /// <summary>
      /// GUID статуса договора "Проверка контрагента".
      /// </summary>
      [Public]
      public static readonly Guid CounterpartyCheckingGuid = Guid.Parse("A667557F-A661-4679-AA08-FC9CF4B00DF9");

      /// <summary>
      /// GUID статуса договора "На согласовании".
      /// </summary>
      [Public]
      public static readonly Guid OnApprovingGuid = Guid.Parse("15FB4519-7A86-414A-9140-1FDF0312B787");
      
      /// <summary>
      /// GUID статуса договора "На доработке".
      /// </summary>
      [Public]
      public static readonly Guid OnReworkGuid = Guid.Parse("42245CD8-5624-41F4-83B9-EB4C69E604CC");
      
      /// <summary>
      /// GUID статуса договора "Получение корпоративного одобрения".
      /// </summary>
      [Public]
      public static readonly Guid CorpAcceptanceGuid = Guid.Parse("1B8D4B8F-A694-40DD-9E48-79DFC43C07BB");
      
      /// <summary>
      /// GUID статуса договора "Получение согласования ПАО «ЛУКОЙЛ»".
      /// </summary>
      [Public]
      public static readonly Guid LukoilApprovedGuid = Guid.Parse("2A7DA2F2-FE8B-443E-8BE5-C22E6B60EACC");
      
      /// <summary>
      /// GUID статуса договора "Согласован".
      /// </summary>
      [Public]
      public static readonly Guid ApprovedGuid = Guid.Parse("F7CE95D5-24E4-49BD-BEA0-8B238B8DDFCD");
      
      /// <summary>
      /// GUID статуса договора "Отказ от заключения договора".
      /// </summary>
      [Public]
      public static readonly Guid RejectedGuid = Guid.Parse("75DCF6D8-83B9-49A1-8137-FDE70CCF48B0");
      
      /// <summary>
      /// GUID статуса договора "Передан Подписанту на подтверждение в электронном виде".
      /// </summary>
      [Public]
      public static readonly Guid SendedToSignerGuid = Guid.Parse("0832E5C5-D8CC-4A18-B124-1BBE97835BA6");
      
      /// <summary>
      /// GUID статуса договора "Подтвержден Подписантом в электронном виде".
      /// </summary>
      [Public]
      public static readonly Guid SignerAcceptedGuid = Guid.Parse("400DD430-5288-429F-940A-7441DC262EB4");
      
      #endregion
      
      #region Статусы движения скан-копий.
      
      /// <summary>
      /// GUID статуса договора "Контрагенту отправлен PDF-файл на подписание".
      /// </summary>
      [Public]
      public static readonly Guid PDFSendedForSigningGuid = Guid.Parse("72768B12-EA5D-4CDF-BADA-C72265470BA2");
      
      /// <summary>
      /// GUID статуса договора "Контрагенту отправлена скан-копия на подписание".
      /// </summary>
      [Public]
      public static readonly Guid ScanSendedCounterpartyForSigningGuid = Guid.Parse("AAD528FD-9DF6-49D5-86DE-7D033CF23EDC");
      
      /// <summary>
      /// GUID статуса договора "Подписана скан-копия со стороны Контрагента".
      /// </summary>
      [Public]
      public static readonly Guid ScanSignedByCounterpartyGuid = Guid.Parse("7C68D7F3-F0E0-4767-8D51-798B5B481976");
      
      /// <summary>
      /// GUID статуса договора "Скан-копия передана на подписание в Обществе".
      /// </summary>
      [Public]
      public static readonly Guid ScanSendedBusinessUnitForSigningGuid = Guid.Parse("1C8AD070-B449-4B9F-B692-987B12DBA802");
      
      /// <summary>
      /// GUID статуса договора "Скан-копия подписана всеми сторонами".
      /// </summary>
      [Public]
      public static readonly Guid ScanSignedByAllSidesGuid = Guid.Parse("6066796E-BA35-46E6-B453-D73B355EE62E");
      
      /// <summary>
      /// GUID статуса договора "Контрагент отказался подписать документ".
      /// </summary>
      [Public]
      public static readonly Guid CounterpartyRejectedSigningGuid = Guid.Parse("519363D1-408F-4FDA-9E1C-A89CB3FB4CAC");
      
      #endregion
      
      #region Статусы движения оригиналов.
      
      /// <summary>
      /// GUID статуса договора "Оригинал передан на подписание в Обществе".
      /// </summary>
      [Public]
      public static readonly Guid OriginalSendedBusinessUnitForSigningGuid = Guid.Parse("3F169D87-1393-4A09-87DF-C14757A53461");
      
      /// <summary>
      /// GUID статуса договора "Оригинал подписан в Обществе".
      /// </summary>
      [Public]
      public static readonly Guid OriginalSignedByBusinessUnitGuid = Guid.Parse("659B3F6A-6DDD-464E-B3B3-B251EE23F1AB");
      
      /// <summary>
      /// GUID статуса договора "Подписант отказался подписать документ".
      /// </summary>
      [Public]
      public static readonly Guid SignerRejectedSigningGuid = Guid.Parse("1F2B7010-51E7-485F-B445-F68768453F2F");
      
      /// <summary>
      /// GUID статуса договора "Оригиналы подписаны всеми сторонами".
      /// </summary>
      [Public]
      public static readonly Guid OriginalSignedByAllSidesGuid = Guid.Parse("75659C80-0D16-4B32-8ECB-7DEE739E9DB9");
      
      /// <summary>
      /// GUID статуса договора "Ожидает отправки контрагенту".
      /// </summary>
      [Public]
      public static readonly Guid OriginalWaitingForSendingGuid = Guid.Parse("E17314EE-3BB6-4272-99CE-CBF63AA571F2");
      
      /// <summary>
      /// GUID статуса договора "Оригинал документа помещен в пакет для отправки".
      /// </summary>
      [Public]
      public static readonly Guid OriginalPlacedForSendingGuid = Guid.Parse("F0395A10-A483-454C-8796-E24AEE78EE55");
      
      /// <summary>
      /// GUID статуса договора "Оригинал документа принят к отправке".
      /// </summary>
      [Public]
      public static readonly Guid OriginalAcceptedForSendingGuid = Guid.Parse("ABFEA5BD-C5A4-47BE-9768-0BDABC600881");
      
      /// <summary>
      /// GUID статуса договора "Оригинал документа отправлен контрагенту".
      /// </summary>
      [Public]
      public static readonly Guid OriginalSendedToCounterpartyGuid = Guid.Parse("439BA0A4-B4EB-499D-A631-E91F2097C227");
      
      /// <summary>
      /// GUID статуса договора "Получен оригинал, подписанный Контрагентом".
      /// </summary>
      [Public]
      public static readonly Guid OriginalReceivedFromCounterpartyGuid = Guid.Parse("F12DF632-36D1-49BD-A51B-A6E0CF2CD6EB");
      
      /// <summary>
      /// GUID статуса договора "Контрагент вернул неподписанный документ".
      /// </summary>
      [Public]
      public static readonly Guid OriginalReceivedNonSignedGuid = Guid.Parse("123A3280-74D4-486E-AAB2-7132F2BAD3B3");
      
      /// <summary>
      /// GUID статуса договора "Оригинал возвращен почтовой службой".
      /// </summary>
      [Public]
      public static readonly Guid OriginalReturnedByPostGuid = Guid.Parse("4E9E0990-4F4A-42AE-B3F8-3534DDB488E6");
      
      /// <summary>
      /// GUID статуса договора "Документ помещен в архив".
      /// </summary>
      [Public]
      public static readonly Guid OriginalArchivedGuid = Guid.Parse("5E9F4CF7-D0ED-46D7-A0C0-0E9AE3B55520");

      #endregion
    }
    #endregion
    
    /// <summary>
    /// GUID`ы ролей.
    /// </summary>
    [Public]
    public static class RoleGuid
    {
      // GUID роли "Сотрудники клиентского сервиса".
      [Public]
      public static readonly Guid CustomerServiceEmployeesRole = Guid.Parse("3B034629-605B-4F69-8005-5D9AFEB46062");
      
      // GUID роли "Сотрудники ДПО".
      [Public]
      public static readonly Guid DPOEmployeesRole = Guid.Parse("421E5B06-B380-4ADB-A8A2-4063C405EB7B");
      
      // GUID Ответственный за настройку модуля "Договоры".
      [Public]
      public static readonly Guid ResponsibleSettingContractRole = Guid.Parse("2098A78A-57B5-449E-A821-5EABB9FCB27D");
      
      // GUID роли "Сотрудники ЕЦД".
      [Public]
      public static readonly Guid ECDEmployeesRole = Guid.Parse("C8E4CDE6-E0C2-41F4-A77E-45F75D73B88D");
      
      // GUID роли "ЕЦД. Отзыв оферты".
      [Public]
      public static readonly Guid ECDCancelOfferRole = Guid.Parse("50F903F1-DBA8-4C41-846B-858095504B11");
      
      // GUID роли "Ответственный за SAP".
      [Public]
      public static readonly Guid SAPResponsibleRole = Guid.Parse("158DA7A2-C807-4A60-9516-7D65B7719994");
      
      /// <summary>
      /// GUID роли "Сотрудник ДПО".
      /// </summary>
      [Public]
      public static readonly Guid DPOEmployeRole = Guid.Parse("8AF28682-AB44-45E9-969C-9C2C33E32F54");
      
      /// <summary>
      /// GUID роли "Ответственные за отправку".
      /// </summary>
      [Public]
      public static readonly Guid SendingResponsiblesRole = Guid.Parse("1D609EDA-03F7-45EA-85E9-E2F912E0DE2F");
    }
    
    /// <summary>
    /// GUID`ы видов документов.
    /// </summary>
    [Public]
    public static class DocumentKindGuid
    {      
      /// <summary>
      /// GUID вида документа "Заявка на разработку формы договора для разовой сделки".
      /// </summary>
      [Public]
      public static readonly Guid ApplicationNewContractFormGuid = Guid.Parse("32D5288E-97CC-4DA3-85EE-6BE2ED99FDD4");      
      
      /// <summary>
      /// GUID вида документа "Типовая форма договора".
      /// </summary>
      [Public]
      public static readonly Guid StandardContractFormGuid = Guid.Parse("1B9EE23F-28D0-47A5-90C9-6784F8E60F11");
    }
    
    // Значение по умолчанию для "Напоминать об окончании за".
    [Public]
    public const int DaysToFinishWorks = 60;
    
    // Имя параметра в таблице Sungero_Docflow_Params для фиксации последнего выполнения фонового процесса контроля наличия оригиналов для активированных договоров.
    public const string LastContractOriginalsControlJobParamName = "LastContractOriginalsControlJobDate";
    
    // Имя параметра в таблице Sungero_Docflow_Params для фиксации последнего выполнения фонового процесса по выполнению условия активации договорного документа.
    public const string LastConditionsActivationCompleteJobParamName = "LastConditionsActivationCompleteJobDate";

    /// <summary>
    // GUID главной группы вложений в задаче на согласование.
    /// </summary>
    public static readonly Guid DocumentGroupApprovalTask = Guid.Parse("08e1ef90-521f-41a1-a13f-c6f175007e54");
    
    /// <summary>
    // GUID способа доставки Нарочным.
    /// </summary>
    [Public]
    public static readonly Guid WithRensposibleMailDeliveryMethod = Guid.Parse("B97BCD3B-342D-4FA1-9552-0F90A7F1D87F");
  }
}