using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalSigningAssignment;

namespace DirRX.Solution
{
  partial class ApprovalSigningAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      Functions.ApprovalSigningAssignment.HideFildForWhom(_obj);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);      
      Functions.ApprovalSigningAssignment.HideFildForWhom(_obj);
    }

  }
}