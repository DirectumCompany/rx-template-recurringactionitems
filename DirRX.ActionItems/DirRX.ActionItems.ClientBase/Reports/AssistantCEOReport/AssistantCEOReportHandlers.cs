using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ActionItems
{
  partial class AssistantCEOReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      var dialog = Dialogs.CreateInputDialog(DirRX.ActionItems.Reports.Resources.AssistantCEOReport.DialogTitle);
      var beginDate = dialog.AddDate(DirRX.ActionItems.Reports.Resources.AssistantCEOReport.StartDate, true, null);
      var endDate = dialog.AddDate(DirRX.ActionItems.Reports.Resources.AssistantCEOReport.EndDate, true, null);
      var mark = dialog.AddSelect(DirRX.ActionItems.Reports.Resources.AssistantCEOReport.Mark, false, DirRX.ActionItems.Marks.Null);
      var businessUnit = dialog.AddSelect(DirRX.ActionItems.Reports.Resources.AssistantCEOReport.BusinessUnit, false, DirRX.Solution.BusinessUnits.Null);
      
      dialog.Buttons.AddOkCancel();
      dialog.Buttons.Default = DialogButtons.Ok;
      if (dialog.Show() == DialogButtons.Ok)
      {
        AssistantCEOReport.BeginDate = beginDate.Value;
        AssistantCEOReport.EndDate = endDate.Value;
        AssistantCEOReport.BusinessUnit = businessUnit.Value;
        AssistantCEOReport.Mark = mark.Value;
      }
      else
        e.Cancel = true;
    }

  }
}