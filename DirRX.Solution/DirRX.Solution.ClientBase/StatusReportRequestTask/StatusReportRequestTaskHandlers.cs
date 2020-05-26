using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.StatusReportRequestTask;

namespace DirRX.Solution
{
  partial class StatusReportRequestTaskClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      var isManyAssignees = _obj.IsManyAssignees == true;    	    	
    	if (!isManyAssignees)
    	{
    		_obj.State.Properties.Assignee.IsEnabled = _obj.Assignee == null;
    	}

    	_obj.State.Properties.Assignee.IsRequired = !isManyAssignees;
      _obj.State.Properties.Assignees.IsRequired = isManyAssignees;
    	
      _obj.State.Properties.Assignee.IsVisible = !isManyAssignees;
      _obj.State.Properties.Assignees.IsVisible = isManyAssignees;     
    }

  }
}