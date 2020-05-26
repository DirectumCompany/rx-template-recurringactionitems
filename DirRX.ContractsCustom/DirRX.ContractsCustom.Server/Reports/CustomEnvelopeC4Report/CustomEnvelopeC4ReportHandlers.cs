using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ContractsCustom
{
  partial class CustomEnvelopeC4ReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      // Удалить временные таблицы.
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.CustomEnvelopeC4Report.EnvelopesTableName, CustomEnvelopeC4Report.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      CustomEnvelopeC4Report.ReportSessionId = Guid.NewGuid().ToString();
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.CustomEnvelopeC4Report.EnvelopesTableName, CustomEnvelopeC4Report.ReportSessionId);
      Functions.Module.FillEnvelopeTable(CustomEnvelopeC4Report.ReportSessionId, CustomEnvelopeC4Report.ShippingPackages.ToList());
    }

  }
}