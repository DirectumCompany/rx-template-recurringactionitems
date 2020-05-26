using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalSimpleAssignment;

namespace DirRX.Solution
{
  partial class ApprovalSimpleAssignmentServerHandlers
  {

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