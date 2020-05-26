using System;
using Sungero.Core;

namespace DirRX.ProcessSubstitutionModule.Constants
{
  public static class Module
  {

    /// <summary>
    /// GUID группы вложений Документ задачи на согласование по регламенту.
    /// </summary>
    public static readonly Guid ApprovalTaskDocGuid = Guid.Parse("08e1ef90-521f-41a1-a13f-c6f175007e54"); 
    
    /// <summary>
    /// GUID типа документов Приказы.
    /// </summary>
    public static readonly Guid OrdersGuid = Guid.Parse("9570e517-7ab7-4f23-a959-3652715efad3");   
     
    /// <summary>
    /// GUID типа документов Договоры.
    /// </summary>
    public static readonly Guid ContractsGuid = Guid.Parse("f37c7e63-b134-4446-9b5b-f8811f6c9666"); 

    /// <summary>
    /// Имя параметра в таблице Sungero_Docflow_Params для фиксации последнего выполнения фонового процесса рассылки заданий-оповещений.
    /// </summary>
    public const string SendNoticeDateTimeDocflowParamName = "SendNoticeDate";
  }
}