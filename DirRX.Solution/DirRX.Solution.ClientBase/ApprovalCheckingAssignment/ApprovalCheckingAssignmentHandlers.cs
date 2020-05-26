using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalCheckingAssignment;

namespace DirRX.Solution
{
	partial class ApprovalCheckingAssignmentClientHandlers
	{

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();

      var isVisible = document != null &&
        !DirRX.PartiesControl.RevisionRequests.Is(document);
      
      _obj.State.Attachments.RiskAttachmentGroup.IsVisible = isVisible;
      
      var stage = Functions.ApprovalTask.GetStage(Solution.ApprovalTasks.As(_obj.Task), ApprovalStage.StageType.SimpleAgr);
      
      if (stage != null && stage.AllowSelectSigner == true)
      {
        var hasSignStage = Solution.ApprovalTasks.As(_obj.Task).ApprovalRule.Stages.Where(s => s.Stage.StageType == ApprovalStage.StageType.Sign).Any();
        _obj.State.Properties.Signatory.IsVisible = hasSignStage;
        _obj.State.Properties.Signatory.IsRequired = hasSignStage;

        if (Solution.ApprovalTasks.As(_obj.Task).ApprovalRule == null)
          _obj.State.Properties.Signatory.IsEnabled = false;
        else
          _obj.State.Properties.Signatory.IsEnabled = hasSignStage && _obj.Status.Value == Sungero.Workflow.AssignmentBase.Status.InProcess;
      }
      else
      {
        _obj.State.Properties.Signatory.IsVisible = false;
        _obj.State.Properties.Signatory.IsRequired = false;
      }
      if (stage != null)
        _obj.State.Properties.SubjectTransaction.IsVisible = stage.IsSubjectTransactionConfirmation == true;
    }

    public virtual void SignatoryValueInput(DirRX.Solution.Client.ApprovalCheckingAssignmentSignatoryValueInputEventArgs e)
    {
      _obj.State.Controls.Control.Refresh();
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);
      
      var stage = Functions.ApprovalTask.GetStage(Solution.ApprovalTasks.As(_obj.Task), Sungero.Docflow.ApprovalStage.StageType.SimpleAgr);
      
      if (stage == null || stage.AllowSendToRework == false)
        e.HideAction(_obj.Info.Actions.ForRework);
      
      if (stage == null || stage.AllowSendToRecycling == false)
        e.HideAction(_obj.Info.Actions.Recycling);
      
      // скрытие действия Запрос инициатору в зависимости от настройки этапа
      if (stage == null || stage.RequestInitiatorOn == false)
        e.HideAction(_obj.Info.Actions.RequestInititator);
      
      if (stage == null || stage.CanFailOn == false)
        e.HideAction(_obj.Info.Actions.Deny);
    }

	}
}