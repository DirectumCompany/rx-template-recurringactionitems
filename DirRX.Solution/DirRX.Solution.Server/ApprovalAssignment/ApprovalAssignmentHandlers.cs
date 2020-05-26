using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalAssignment;

namespace DirRX.Solution
{
  partial class ApprovalAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      base.BeforeComplete(e);
      
      if (_obj.ApprovedWithRisk.GetValueOrDefault())
        e.Result = DirRX.Solution.ApprovalAssignments.Resources.ApprovedWithRiskResultName;
      if (_obj.ForRecycle.GetValueOrDefault())
        e.Result = DirRX.Solution.ApprovalAssignments.Resources.ForRecycleResultName;
      if (_obj.Rejected.GetValueOrDefault())
        e.Result = DirRX.Solution.ApprovalAssignments.Resources.RejectedResultName;
      if (_obj.Denied.GetValueOrDefault())
      {
        e.Result = DirRX.Solution.ApprovalAssignments.Resources.DeniedResultName;
        // Тема - Согласование прекращено по причине: <Комментарий>.
        var subject = DirRX.Solution.ApprovalAssignments.Resources.DeniedSubjectTextFormat(_obj.ActiveText);
        PublicFunctions.Module.Remote.CreateAuthorNotice(_obj, subject);
        PublicFunctions.Module.Remote.AbortTask(_obj.Task.Id);
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      _obj.ForRecycle = false;
      _obj.ApprovedWithRisk = false;
      _obj.Rejected = false;
      _obj.Denied = false;
    }
  }

}