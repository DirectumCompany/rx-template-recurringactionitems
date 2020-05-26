using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalSimpleAssignment;

namespace DirRX.Solution
{
  partial class ApprovalSimpleAssignmentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();

      var isVisible = document != null &&
        !DirRX.PartiesControl.RevisionRequests.Is(document);
      
      _obj.State.Attachments.RiskAttachmentGroup.IsVisible = isVisible;
    }

  }
}