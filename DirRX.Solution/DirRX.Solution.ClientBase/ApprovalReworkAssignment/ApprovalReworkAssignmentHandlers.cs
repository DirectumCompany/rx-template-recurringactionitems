using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalReworkAssignment;

namespace DirRX.Solution
{
  partial class ApprovalReworkAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      if (ApprovalTasks.As(_obj.Task).IsRecycle.GetValueOrDefault())
        _obj.State.Properties.Approvers.Properties.Action.IsEnabled = false;
    }
  }
}