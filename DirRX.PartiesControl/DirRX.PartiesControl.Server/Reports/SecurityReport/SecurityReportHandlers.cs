using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PartiesControl
{
  partial class SecurityReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.SecurityReport.SecurityReportTableName, SecurityReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var reportSessionId = System.Guid.NewGuid().ToString();
      SecurityReport.ReportSessionId = reportSessionId;

      var reportData = PublicFunctions.Module.GetSecurityReportData(SecurityReport.PeriodFrom.Value,
                                                                    SecurityReport.PeriodTo.Value,
                                                                    SecurityReport.Counterparty,
                                                                    SecurityReport.CheckingResult);
      
      foreach (var element in reportData)
        element.ReportSessionId = reportSessionId;

      Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.SecurityReport.SecurityReportTableName, reportData);
    }

  }
}