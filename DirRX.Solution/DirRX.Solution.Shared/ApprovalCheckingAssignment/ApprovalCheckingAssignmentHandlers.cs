using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalCheckingAssignment;

namespace DirRX.Solution
{
  partial class ApprovalCheckingAssignmentSharedHandlers
  {

    public virtual void SignatoryChanged(DirRX.Solution.Shared.ApprovalCheckingAssignmentSignatoryChangedEventArgs e)
    {
      _obj.State.Controls.Control.Refresh(); 
    }

  }
}