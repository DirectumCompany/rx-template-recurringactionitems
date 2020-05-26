using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalStage;

namespace DirRX.Solution
{
  partial class ApprovalStageClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.ApprovalStage.SetVisibilityProperties(_obj);
      Functions.ApprovalStage.SetAvailabilityProperties(_obj);
      base.Refresh(e);
      
      // Недоступность доработки при использовании этапа в правилах.
      bool hasRules;
      if (e.Params.TryGetValue(Sungero.Docflow.Constants.ApprovalStage.HasRules, out hasRules) && hasRules)
        _obj.State.Properties.ReworkType.IsEnabled = false;
      
      Functions.ApprovalStage.SetRequiredProperties(_obj);
    }

  }
}