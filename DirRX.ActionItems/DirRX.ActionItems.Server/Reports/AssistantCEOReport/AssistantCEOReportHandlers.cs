using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Company;

namespace DirRX.ActionItems
{
  partial class AssistantCEOReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      // Удалить временные данные из таблицы.
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.AssistantCEOReport.SourceTableName, AssistantCEOReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var dataTable = new List<Structures.AssistantCEOReport.TableLine>();
      
      AssistantCEOReport.ReportSessionId = Guid.NewGuid().ToString();
      AssistantCEOReport.ReportDate = Calendar.Now.ToShortDateString();
      AssistantCEOReport.PeriodText = Reports.Resources.AssistantCEOReport.PeriodTemplateFormat(AssistantCEOReport.BeginDate.Value.ToShortDateString(), AssistantCEOReport.EndDate.Value.ToShortDateString());
      if (AssistantCEOReport.BusinessUnit != null)
        AssistantCEOReport.BusinessUnitText = Reports.Resources.AssistantCEOReport.BusinessUnitTemplateFormat(AssistantCEOReport.BusinessUnit);
      var tasks = new List<DirRX.Solution.IActionItemExecutionTask>();
      
      // В отчет должны попадать поручения, где ГД или помощник ГД указан автором, поставщиком или контролером.
      var CEO = DirRX.Solution.Employees.Null;
      if (Employees.Current != null)
        CEO = Functions.ActionItemsRole.GetCEO(DirRX.Solution.Employees.Current);
      var CEOAssistantsRole = Roles.GetAll(r => r.Sid == Constants.Module.CEOAssistant).FirstOrDefault();
      
      AccessRights.AllowRead(
        () =>
        {
          var query = DirRX.Solution.ActionItemExecutionTasks.GetAll()
            .Where(t => t.Status == Sungero.Workflow.Task.Status.Completed || t.Status == Sungero.Workflow.Task.Status.InProcess)
            .Where(t => t.Deadline >= AssistantCEOReport.BeginDate || t.Deadline <= AssistantCEOReport.EndDate)
            .Where(t => AssistantCEOReport.Mark == null || DirRX.ActionItems.Marks.Equals(t.Mark, AssistantCEOReport.Mark))
            .ToList()
            .Where(t => AssistantCEOReport.BusinessUnit == null ||
                   t.DocumentsGroup.OfficialDocuments.Any(d => DirRX.Solution.BusinessUnits.Is(d) &&
                                                          DirRX.Solution.BusinessUnits.Equals(Sungero.Docflow.OfficialDocuments.As(d).BusinessUnit, AssistantCEOReport.BusinessUnit)))
            .Where(t => (t.Supervisor != null && (Employees.Equals(t.Supervisor, CEO) || t.Supervisor.IncludedIn(CEOAssistantsRole))) ||
                        Employees.Equals(t.Initiator, CEO) || t.Initiator.IncludedIn(CEOAssistantsRole) ||
                        Employees.Equals(t.AssignedBy, CEO) || t.AssignedBy.IncludedIn(CEOAssistantsRole));
          tasks = query.ToList();
        });
      
      foreach (var task in tasks)
      {
        var tableLine = Structures.AssistantCEOReport.TableLine.Create();
        tableLine.ReportSessionId = AssistantCEOReport.ReportSessionId;
        
        tableLine.Mark = task.Mark != null ? task.Mark.ToString() : string.Empty;
        tableLine.Id = task.Id;
        tableLine.Hyperlink = Sungero.Core.Hyperlinks.Get(DirRX.Solution.ActionItemExecutionTasks.Info, task.Id);
        var document = task.DocumentsGroup.OfficialDocuments.FirstOrDefault();
        tableLine.DocumentInfo = document != null ? document.ToString() : string.Empty;
        tableLine.ActionItemText = task.ActionItem;
        tableLine.PlanDate = task.Deadline.HasValue ? task.Deadline.Value.ToShortDateString() : string.Empty;
        tableLine.Supervisor = task.Supervisor != null ? task.Supervisor.ToString() : string.Empty;
        
        tableLine.NewPlanDate = string.Format(Reports.Resources.AssistantCEOReport.NewPlanDateTemplate,
                                              task.InitialDeadline.HasValue ? task.InitialDeadline.Value.ToShortDateString() : task.Deadline.HasValue ? task.Deadline.Value.ToShortDateString() : string.Empty,
                                              Environment.NewLine,
                                              task.Deadline.HasValue ? task.Deadline.Value.ToShortDateString() : string.Empty);
        
        tableLine.Status = string.Empty;
        if (task.ExecutionState != null)
          tableLine.Status = Sungero.RecordManagement.ActionItemExecutionTasks.Info.Properties.ExecutionState.GetLocalizedValue(task.ExecutionState.Value);
        
        dataTable.Add(tableLine);
      }
      Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.AssistantCEOReport.SourceTableName, dataTable);
    }
  }
}