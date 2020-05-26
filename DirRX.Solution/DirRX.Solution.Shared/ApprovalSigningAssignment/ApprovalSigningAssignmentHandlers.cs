using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalSigningAssignment;

namespace DirRX.Solution
{
  partial class ApprovalSigningAssignmentSharedHandlers
  {

    public virtual void SignedDirRXChanged(DirRX.Solution.Shared.ApprovalSigningAssignmentSignedDirRXChangedEventArgs e)
    {
    	if ((e.NewValue != null && e.NewValue != e.OldValue) && _obj.ForWhomDirRX == null)
    		_obj.ForWhomDirRX = e.OldValue; 
    }

  }
}