using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ActionItems
{
  partial class PrintActionItemTaskServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.PrintActionItemTask.PrintActionItemTaskTableName, PrintActionItemTask.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      PrintActionItemTask.Author = PrintActionItemTask.Task.Author.DisplayValue;
      PrintActionItemTask.Initiator = PrintActionItemTask.Task.Initiator.DisplayValue;
      PrintActionItemTask.Category = PrintActionItemTask.Task.Category.DisplayValue;
      PrintActionItemTask.Priority = PrintActionItemTask.Task.Priority.DisplayValue;
      PrintActionItemTask.Supervisor = PrintActionItemTask.Task.Supervisor != null ? PrintActionItemTask.Task.Supervisor.DisplayValue : string.Empty;
      PrintActionItemTask.Resolution = PrintActionItemTask.Task.ActionItem != DirRX.ActionItems.Reports.Resources.PrintActionItemTask.DefaultActionItemResolution ?
        DirRX.ActionItems.Reports.Resources.PrintActionItemTask.ResolutionFormat(PrintActionItemTask.Task.ActionItem) :
        string.Empty;
      
      var document = PrintActionItemTask.Task.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
        PrintActionItemTask.Document = document.DisplayValue;
      
      PrintActionItemTask.Printed = string.Format("{0} / {1}", Users.Current.DisplayValue, Calendar.Now.ToUserTime().ToString("d"));
      
      var reportSessionId = System.Guid.NewGuid().ToString();
      PrintActionItemTask.ReportSessionId = reportSessionId;
      
      var reportData = PublicFunctions.Module.GetAllAssignmentsReportData(this.PrintActionItemTask.Task);
      
      foreach (var element in reportData)
        element.ReportSessionId = reportSessionId;
      
      Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.PrintActionItemTask.PrintActionItemTaskTableName, reportData);
    }

  }
}