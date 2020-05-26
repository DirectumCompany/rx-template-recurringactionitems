using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalAssignment;

namespace DirRX.Solution
{
  partial class ApprovalAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      
      _obj.State.Attachments.RiskAttachmentGroup.IsVisible = document != null && (DirRX.Solution.ApprovalStages.As(_obj.Stage).ApprovalWithRiskOn == true ||
                                                                                  DirRX.Solution.Contracts.Is(document) || DirRX.Solution.SupAgreements.Is(document));
      
      var isVisible = DirRX.Solution.ApprovalStages.As(_obj.Stage).ApprovalWithRiskOn == true;
      _obj.State.Properties.RiskDescription.IsVisible = isVisible;
      _obj.State.Properties.RiskLevel.IsVisible = isVisible;
    }
    
    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);

			if (!Users.Current.IncludedIn(DirRX.LocalActs.PublicConstants.Module.RoleGuid.AddApproversRoleGuid) &&
			    !OutgoingLetters.Is(_obj.DocumentGroup.OfficialDocuments.FirstOrDefault()) &&
			    !Memos.Is(_obj.DocumentGroup.OfficialDocuments.FirstOrDefault()))
        e.HideAction(_obj.Info.Actions.AddApprover);
      
      // Cкрытие действий Запрос инициатору, Согласовать с рисками и На переработку в зависимости от настройки этапа
      var stage = DirRX.Solution.ApprovalStages.As(_obj.Stage);
      
      if (stage == null || stage.RequestInitiatorOn == false)
        e.HideAction(_obj.Info.Actions.RequestInititatorAction);
      
      if (stage == null || stage.ApprovalWithRiskOn != true)
      {
        e.HideAction(_obj.Info.Actions.ApprovedWithRisk);
        _obj.State.Properties.RiskLevel.IsVisible = false;
        _obj.State.Properties.RiskDescription.IsVisible = false;
      }
      
      if (stage == null || stage.AllowSendToRecycling == false)
        e.HideAction(_obj.Info.Actions.Recycling);

      
      if (stage == null || stage.CanDenyApprovalOn == false)
        e.HideAction(_obj.Info.Actions.Reject);
      
      if (stage == null || stage.CanFailOn == false)
        e.HideAction(_obj.Info.Actions.Deny);
      
    }

  }
}