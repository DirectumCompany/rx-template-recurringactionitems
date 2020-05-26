using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.LocalActs
{
  partial class ApprovalSheetReportServerHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      ApprovalSheetReport.ReportSessionId = Guid.NewGuid().ToString();
      Functions.Module.UpdateApprovalSheetReportTable(ApprovalSheetReport.Document, ApprovalSheetReport.ReportSessionId);
      ApprovalSheetReport.HasRespEmployee = false;
      
      var document = ApprovalSheetReport.Document;
      if (document == null)
        return;
      
      // Наименование отчета.
      ApprovalSheetReport.DocumentName = Sungero.Docflow.PublicFunctions.Module.FormatDocumentNameForReport(document, false);
      
      // НОР.
      var ourOrg = document.BusinessUnit;
      if (ourOrg != null)
        ApprovalSheetReport.OurOrgName = ourOrg.Name;
      
      // Дата отчета.
      ApprovalSheetReport.CurrentDate = Calendar.Now;
      
      // Ответственный.
      var responsibleEmployee = Sungero.Company.Employees.As(document.Author);
      
      if (responsibleEmployee != null &&
          responsibleEmployee.IsSystem != true)
      {
        var jobTitle = string.Empty;
        if (responsibleEmployee.JobTitle != null)
        {
          var jobTitleNormalizeValue = Sungero.Docflow.PublicFunctions.Module.ReplaceFirstSymbolToLowerCase(responsibleEmployee.JobTitle.DisplayValue);
          jobTitle = string.Format(", {0}", jobTitleNormalizeValue);
        }
        
        ApprovalSheetReport.RespEmployee = string.Format("{0}: {1}{2}", DirRX.LocalActs.Reports.Resources.ApprovalSheetReport.ResponsibleEmployee,
                                                               responsibleEmployee.Person.ShortName,
                                                               jobTitle);
        ApprovalSheetReport.HasRespEmployee = true;
      }
      
      // Распечатал.
      if (Sungero.Company.Employees.Current == null)
      {
        ApprovalSheetReport.Clerk = Users.Current.Name;
      }
      else
      {
        var clerkPerson = Sungero.Company.Employees.Current.Person;
        ApprovalSheetReport.Clerk = clerkPerson.ShortName;
      }
      
      // Дата отправки на согласование.
      var lastApprovalTask = Functions.Module.GetLastApprovalTask(document);
      if (lastApprovalTask != null)
        ApprovalSheetReport.StartedDate = lastApprovalTask.Started.Value.Date.ToShortDateString();
    }

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(DirRX.LocalActs.Constants.Module.ApprovalSheetReport.SourceTableName,
                                                              ApprovalSheetReport.ReportSessionId);
    }

  }
}