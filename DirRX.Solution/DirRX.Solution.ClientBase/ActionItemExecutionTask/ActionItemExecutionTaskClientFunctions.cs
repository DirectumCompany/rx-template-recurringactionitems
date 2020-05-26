using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ActionItemExecutionTask;

namespace DirRX.Solution.Client
{
  partial class ActionItemExecutionTaskFunctions
  {
    /// <summary>
    /// Валидация срока выполнения подчиненного поручения.
    /// </summary>
    public string ValidateChildTaskDeadline(DateTime? deadline)
    {
      if (deadline != null && _obj.ParentAssignment != null && _obj.MainTask != null)
      {
        var mainTask = DirRX.Solution.ActionItemExecutionTasks.As(_obj.MainTask);
        if (mainTask != null)
        {
          var endDate = Sungero.Docflow.PublicFunctions.Module.GetDateWithTime(_obj.ParentAssignment.Deadline.Value, _obj.Initiator);
          var executionPeriod = WorkingTime.GetDurationInWorkingHours(mainTask.Started.Value, endDate, _obj.Initiator);
          var residualPeriod = WorkingTime.GetDurationInWorkingHours(deadline.Value, endDate, _obj.Initiator);
          
          if (executionPeriod == 0 || ((double)residualPeriod / executionPeriod * 100) <= mainTask.Priority.CompletionDeadlinePercent)
            return DirRX.Solution.ActionItemExecutionTasks.Resources.CompletionDeadlinePercentErrorFormat(mainTask.Priority.CompletionDeadlinePercent);
        }
      }
      return string.Empty;
    }
  }
}