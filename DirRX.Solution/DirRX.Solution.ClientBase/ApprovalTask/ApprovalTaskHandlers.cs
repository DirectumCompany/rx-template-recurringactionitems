using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalTask;

namespace DirRX.Solution
{
  partial class ApprovalTaskClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();

      _obj.State.Attachments.RiskAttachmentGroup.IsVisible = document != null && !DirRX.PartiesControl.RevisionRequests.Is(document);
      _obj.State.Properties.NeedLUKOILApproval.IsVisible = document != null && Solution.Orders.Is(document);    
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);
    }

  }
}