using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.LocalActs
{
  partial class DocumentApprovalStatisticsReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      var dialog = Dialogs.CreateInputDialog(DirRX.LocalActs.Reports.Resources.DocumentApprovalStatisticsReport.DocumentApprovalStatisticsReport);
      
      var businessUnit = dialog.AddSelect(DirRX.LocalActs.Reports.Resources.DocumentApprovalStatisticsReport.BusinessUnit, false, Sungero.Company.BusinessUnits.Null);
      var department = dialog.AddSelect(DirRX.LocalActs.Reports.Resources.DocumentApprovalStatisticsReport.Department, false, Sungero.Company.Departments.Null);
      var preparedBy = dialog.AddSelect(DirRX.LocalActs.Reports.Resources.DocumentApprovalStatisticsReport.PreparedBy, false, Sungero.Company.Employees.Null);
      var beginDate = dialog.AddDate(DirRX.LocalActs.Reports.Resources.DocumentApprovalStatisticsReport.BeginDate, false);
      var endDate = dialog.AddDate(DirRX.LocalActs.Reports.Resources.DocumentApprovalStatisticsReport.EndDate, false);
      var docKinds = dialog.AddSelectMany(DirRX.LocalActs.Reports.Resources.DocumentApprovalStatisticsReport.DocKinds, false, Sungero.Docflow.DocumentKinds.Null);
      
      dialog.SetOnButtonClick((args) =>
                              {
                                Sungero.Docflow.PublicFunctions.Module.CheckReportDialogPeriod(args, beginDate, endDate);
                              });
      
      dialog.Buttons.AddOkCancel();
      
      if (dialog.Show() == DialogButtons.Ok)
      {
        DocumentApprovalStatisticsReport.BeginDate = beginDate.Value;
        DocumentApprovalStatisticsReport.EndDate = endDate.Value;
        DocumentApprovalStatisticsReport.BusinessUnit = businessUnit.Value;
        DocumentApprovalStatisticsReport.Department = department.Value;
        DocumentApprovalStatisticsReport.PreparedBy = preparedBy.Value;
        DocumentApprovalStatisticsReport.DocKinds.AddRange(docKinds.Value.ToList());
      }
      else
        e.Cancel = true;
    }

  }
}