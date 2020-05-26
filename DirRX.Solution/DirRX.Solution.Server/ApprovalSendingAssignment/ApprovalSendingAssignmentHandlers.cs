using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalSendingAssignment;

namespace DirRX.Solution
{
  partial class ApprovalSendingAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      base.BeforeComplete(e);      
      if (_obj.CompletedBy.IsSystem.HasValue && _obj.CompletedBy.IsSystem.Value)
        e.Result = DirRX.Solution.ApprovalSendingAssignments.Resources.JobExecutedAutomatically;
    }

    public override void Saved(Sungero.Domain.SavedEventArgs e)
    {
      base.Saved(e);
      if (_obj.State.IsInserted)
      {
        // Создание нового задания может изменить срок задачи.
        _obj.Task.MaxDeadline = Functions.ApprovalTask.GetExpectedDate(ApprovalTasks.As(_obj.Task));
      }
    }
  }

}