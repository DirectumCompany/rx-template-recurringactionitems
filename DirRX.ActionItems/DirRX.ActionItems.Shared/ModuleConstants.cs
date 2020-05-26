using System;
using Sungero.Core;

namespace DirRX.ActionItems.Constants
{
  public static class Module
  {    
    // GUID роли "Секретари".
    public static readonly Guid RoleSecretary = Guid.Parse("7EDC2036-E1F0-4B09-92FB-6496E31BB173");
    
    // GUID роли "Ответственные за настройку поручений".
    public static readonly Guid AssignmentSettingResponsiblesRole = Guid.Parse("4EBBB930-C1B2-4460-911A-B7336E133386");
    
    // GUID роли "Помощник ГД (для отчёта)".
    [Public]
    public static readonly Guid CEOAssistant = Guid.Parse("C947DEBC-CC7F-4CD9-80F0-A0852C8281C8");
    
    // Имя параметра в таблице Sungero_Docflow_Params для фиксации последнего выполнения фонового процесса рассылки уведомлений по поручениям.
    public const string LastNotificationDateTimeDocflowParamName = "ByAssignmentLastNotificationDateTime";
    
    public const string OverdueTaskWidgetValue = "overdueTask";
    public const string TaskDeadlineLess1DayWidgetValue = "taskDeadlineLess1Day";
    public const string TaskDeadlineMore1DayWidgetValue = "taskDeadlineMore1Day";
    public const string ExecutedTaskWidgetValue = "executedTask";
  }
}