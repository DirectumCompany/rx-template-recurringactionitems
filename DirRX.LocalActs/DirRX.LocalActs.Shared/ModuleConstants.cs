using System;
using Sungero.Core;

namespace DirRX.LocalActs.Constants
{
  public static class Module
  {
    // Имя параметра в таблице Sungero_Docflow_Params для фиксации последнего выполнения фонового процесса рассылки уведомлений по поручениям.
    public const string LastNoticeSendDateTimeDocflowParamName = "ByAssignmentLastNoticeSendDateTime";
    
    // Имя параметра в таблице Sungero_Docflow_Params для фиксации последнего выполнения фонового процесса Актуальность задачи согласования.
    public const string RequestInitiatorActuallyNoticeDateTimeDocflowParamName = "RequestInitiatorActuallyNoticeSendDateTime";
    
    /// <summary>
    /// GUID типа документов "Регламентирующие документы".
    /// </summary>
    public static readonly Guid RegulatoryDocumentTypeGuid = Guid.Parse("6c5b18f8-aff3-43d8-9a8f-b81bb7797129"); 
    
    /// <summary>
    /// Наименование типа связи при создании новой редакции регламентирующего документа.
    /// </summary>
    [Public]
    public const string RegulatoryNewEditionRelationName = "Cancel";
    
    /// <summary>
    /// Наименование типа связи при создании новой редакции регламентирующего документа.
    /// </summary>
    [Public]
    public const string RegulatoryOrderRelationName = "Addendum";
    
    /// <summary>
    /// GUID`ы ролей.
    /// </summary>
    [Public]
    public static class RoleGuid
    {
      // GUID роли "Пользователи с правами на признак «Налоговый мониторинг»".
      [Public]
      public static readonly Guid TaxMonitoringGuid = Guid.Parse("FF34F3C4-4572-45EC-BA2C-F7B781D51DD3");

      // GUID роли "Проектные команды".
      [Public]
      public static readonly Guid LocalActsRoleGuid = Guid.Parse("A8BC17A6-3A46-41E4-B034-62F5EAAEDF20");
      
      // GUID роли "Ответственные за внесение ранее изданных регламентирующих документов".
      [Public]
      public static readonly Guid RegulatoryDocumentsUpdaterRoleGuid = Guid.Parse("33A16D3D-1D38-47D2-B571-D9928D786D9A");
      
      // GUID роли "Пользователи с правами добавления согласующих".
      [Public]
      public static readonly Guid AddApproversRoleGuid = Guid.Parse("7A721FD2-FAC9-44E9-BD0B-B2FDB1C4981E");
      
      // GUID роли "Ответственные за контрагентов".
      [Public]
      public static readonly Guid CounterpartiesResponsibleRole = Guid.Parse("C719C823-C4BD-4434-A34B-D7E83E524414");
      
      // GUID роли "Сотрудники исключаемые из задачи на ознакомление".
      [Public]
      public static readonly Guid ExcludeFromAcquaintanceTaskRole = Guid.Parse("E619D04D-690B-47BE-852F-D9651EC5740B");
    }
    
    public static class ApprovalSheetReport
    {
      public const string SourceTableName = "Sungero_Reports_ApprSheet";
    }
    
    /// <summary>
    // GUID главной группы вложений в задаче на согласование.
    /// </summary>
    [Public]
    public static readonly Guid DocumentGroupApprovalTask = Guid.Parse("08e1ef90-521f-41a1-a13f-c6f175007e54");
  }
}