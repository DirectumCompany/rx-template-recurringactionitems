using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ControlReturnAssignment;

namespace DirRX.ContractsCustom.Client
{
  partial class ControlReturnAssignmentActions
  {
    public virtual void StopSigning(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      
    }

    public virtual bool CanStopSigning(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void Complete(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      if (_obj.NewDeadline != null && _obj.NewDeadline > Calendar.Today)
      {
        var contract = _obj.AttachmentContractGroup.ContractualDocuments.SingleOrDefault();
        var recordsTracking = contract.Tracking.Where(x => x.Action.Equals(Sungero.Contracts.ContractBaseTracking.Action.Endorsement) || x.Action.Equals(Sungero.Contracts.ContractBaseTracking.Action.Sending));
        foreach (var record in recordsTracking)
          if (record.ReturnDate == null)
            record.ReturnDeadline = _obj.NewDeadline;
        contract.Save();
        
        if (!string.IsNullOrWhiteSpace(_obj.ActiveText))
          _obj.ActiveText += Environment.NewLine;
        _obj.ActiveText += string.Format(DirRX.ContractsCustom.ControlReturnAssignments.Resources.NewReturnDate);
      }
      else
        e.AddError(DirRX.ContractsCustom.ControlReturnAssignments.Resources.NewDeadlineIsNotFiled);
    }

    public virtual bool CanComplete(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

  }

}