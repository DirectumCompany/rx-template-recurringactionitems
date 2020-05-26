using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalReviewAssignment;

namespace DirRX.Solution.Client
{
  partial class ApprovalReviewAssignmentActions
  {

    public virtual void Approve(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var action = new Sungero.Workflow.Client.ExecuteResultActionArgs(e.FormType, e.Entity, e.Action);
      base.Informed(action);
      _obj.IsApproved = true;
      e.CloseFormAfterAction = true;
      _obj.Complete(Result.Informed);
    }

    public virtual bool CanApprove(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      //TODO: разобраться почему не работает код.
      /*var action = new Sungero.Workflow.Client.CanExecuteResultActionArgs(e.FormType, e.Entity);
      return base.CanInformed(action);*/
      return _obj.Status == Status.InProcess;
    }

  }

}