using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalAssignment;

namespace DirRX.Solution.Client
{
  partial class ApprovalAssignmentFunctions
  {

    /// <summary>
    /// Доступность выполнения действий Recycling, Reject, Deny в заданиях
    /// </summary>       
    public bool CanExecuteApprovalActions()
    {
      return _obj.Addressee == null &&
        _obj.DocumentGroup.OfficialDocuments.Any() &&
        !_obj.Completed.HasValue &&
        _obj.RiskLevel == null &&
        string.IsNullOrEmpty(_obj.RiskDescription);
    }

  }
}