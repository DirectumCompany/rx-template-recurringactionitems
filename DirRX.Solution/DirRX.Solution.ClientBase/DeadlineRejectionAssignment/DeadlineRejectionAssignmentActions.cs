using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.DeadlineRejectionAssignment;

namespace DirRX.Solution.Client
{
  partial class DeadlineRejectionAssignmentActions
  {
    public override void ForRework(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      base.ForRework(e);
    }

    public override bool CanForRework(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return base.CanForRework(e) && _obj.Priority.AllowedExtendDeadline == true && _obj.Deadline.Value >= Calendar.Today;
    }

  }

}