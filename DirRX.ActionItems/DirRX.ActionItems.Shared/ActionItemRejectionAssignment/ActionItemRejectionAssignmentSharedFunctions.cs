using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.ActionItemRejectionAssignment;

namespace DirRX.ActionItems.Shared
{
  partial class ActionItemRejectionAssignmentFunctions
  {
    
    /// <summary>
    /// Задание доступности и видимости свойств.
    /// </summary>
    public void SetStateProperties()
    {
      _obj.State.Properties.ReportDeadline.IsEnabled = _obj.Category != null && _obj.Category.NeedsReportDeadline.GetValueOrDefault();
      _obj.State.Properties.Supervisor.IsEnabled = _obj.IsUnderControl.GetValueOrDefault();
    }
  }
}