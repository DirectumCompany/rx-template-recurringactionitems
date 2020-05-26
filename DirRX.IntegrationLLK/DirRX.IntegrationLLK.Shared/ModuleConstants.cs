using System;
using Sungero.Core;

namespace DirRX.IntegrationLLK.Constants
{
  [Public]
  public static class Module
  {
    
    /// <summary>
    /// GUID для роли "Ответственные за синхронизацию с учетными системами".
    /// </summary>
    [Public]
    public static readonly Guid SynchronizationResponsibleRoleGuid = Guid.Parse("6F98BA36-3B7F-4767-8369-88A65578DC5A");

    /// <summary>
    /// Имя параметра в таблице Sungero_Docflow_Params для фиксации последнего выполнения фонового процесса импорта из ССПД.
    /// </summary>
    public const string LastSSPDImportDateTimeDocflowParamName = "LastSSPDImportDateTime";
    
    /// <summary>
    /// Имя параметра в таблице Sungero_Docflow_Params для фиксации последнего выполнения фонового процесса импорта отсутствий сотрудников.
    /// </summary>
    public const string LastAbsenceImportDateTimeDocflowParamName = "LastAbsenceImportDateTime";
    
    /// <summary>
    /// Код внешней интегрируемой системы - ССПД.
    /// </summary>
    public const string SSPDSystemCode = "SSPD";
    
    #region Скопировано из стандартной (Sungero.ExchangeCore.Constants, Sungero.Docflow.Constants)
    public static class RoleGuid
    { 
      // GUID роли "Ответственные за контрагентов".
      public static readonly Guid CounterpartiesResponsibleRole = Guid.Parse("C719C823-C4BD-4434-A34B-D7E83E524414");
    }
    
    //GUID типа документа "Сведения о контрагенте".
    [Public]
    public static readonly Guid CounterpartyDocumentTypeGuid = Guid.Parse("49d0c5e7-7069-44d2-8eb6-6e3098fc8b10");
    #endregion
    
    //GUID вида документа "Документ из КССС".
    [Public]
    public static readonly Guid KsssDocumentKindGuid = Guid.Parse("CDA1E624-8C31-43D7-8A72-5218F54AB9E9");
    
    //GUID справочника вид документа.
    public static readonly Guid DocumentKindTypeGuid = Guid.Parse("14a59623-89a2-4ea8-b6e9-2ad4365f358c");
    
    //Название линии для занесения оригиналов документов, подписанных контрагентом.
    public const string ContractorsOriginal = "ContractorsOriginal";
    
    //Название линии для занесения оригиналов документов, подписанных с нашей стороны.
    public const string OurOriginal = "OurOriginal";
    
    //Название линии для занесения копий документов, подписанных с нашей стороны.
    public const string OurCopy = "OurCopy";
  }
}