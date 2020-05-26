using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.StatusReportRequestTask;

namespace DirRX.Solution
{
  partial class StatusReportRequestTaskServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      _obj.IsManyAssignees = false;
    }
  }

}