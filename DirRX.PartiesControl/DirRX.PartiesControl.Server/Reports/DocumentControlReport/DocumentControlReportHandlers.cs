using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PartiesControl
{
  partial class DocumentControlReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.DocumentControlReport.DocumentControlReportTableName, DocumentControlReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var reportSessionId = System.Guid.NewGuid().ToString();
      DocumentControlReport.ReportSessionId = reportSessionId;

      var reportData = PublicFunctions.Module.GetDocumentControlReportData();
      
      foreach (var element in reportData)
        element.ReportSessionId = reportSessionId;

      Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.DocumentControlReport.DocumentControlReportTableName, reportData);
    }

  }
}