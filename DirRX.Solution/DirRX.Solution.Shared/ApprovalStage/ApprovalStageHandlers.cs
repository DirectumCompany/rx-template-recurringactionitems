using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalStage;

namespace DirRX.Solution
{
  partial class ApprovalStageSharedHandlers
  {

    public override void StageTypeChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      base.StageTypeChanged(e);
      
      Functions.ApprovalStage.SetVisibilityProperties(_obj);
      Functions.ApprovalStage.SetAvailabilityProperties(_obj);
      
      if (e.NewValue != StageType.SimpleAgr)
      {
        _obj.AllowSendToRecycling = false;
        _obj.IsRiskConfirmation = false;
        _obj.NeedCounterpartyApproval = false;
        _obj.IsAssignmentAllDocsReceived = false;
        _obj.IsAssignmentCorporateApproval = false;
      }
    }

    public override void AllowSendToReworkChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue.Value == e.OldValue)
        return;
      
      Functions.ApprovalStage.SetRequiredProperties(_obj);
      Functions.ApprovalStage.SetVisibilityProperties(_obj);
      Functions.ApprovalStage.SetAvailabilityProperties(_obj);
      
      if (e.NewValue == false && _obj.AllowSendToRecycling == false)
        _obj.ReworkType = null;
      if (e.NewValue == true && _obj.AllowSendToRecycling == false)
        _obj.ReworkType = ApprovalStage.ReworkType.AfterAll;
    }

    public virtual void AllowSendToRecyclingChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue.Value == e.OldValue)
        return;
      
      Functions.ApprovalStage.SetRequiredProperties(_obj);
      Functions.ApprovalStage.SetVisibilityProperties(_obj);
      Functions.ApprovalStage.SetAvailabilityProperties(_obj);
      
      if (e.NewValue == false && _obj.AllowSendToRework == false)
        _obj.ReworkType = null;
      if (e.NewValue == true && _obj.AllowSendToRework == false)
        _obj.ReworkType = ApprovalStage.ReworkType.AfterAll;
    }
  }
}