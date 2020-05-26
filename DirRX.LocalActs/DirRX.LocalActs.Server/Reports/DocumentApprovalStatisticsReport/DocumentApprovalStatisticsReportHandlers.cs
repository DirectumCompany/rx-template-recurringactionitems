using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Workflow;

namespace DirRX.LocalActs
{
  partial class DocumentApprovalStatisticsReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      // Удалить временные данные из таблицы.
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.DocumentApprovalStatisticsReport.SourceTableName, DocumentApprovalStatisticsReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      DocumentApprovalStatisticsReport.ReportSessionId = Guid.NewGuid().ToString();
      #region Отбор задач и документов
      var beginDate = DocumentApprovalStatisticsReport.BeginDate ?? Calendar.SqlMinValue;
      var endDate = DocumentApprovalStatisticsReport.EndDate ?? Calendar.SqlMaxValue;
      var tasks = DirRX.Solution.ApprovalTasks.GetAll()
        .Where(t => t.Status == Sungero.Workflow.Task.Status.Completed || t.Status == Sungero.Workflow.Task.Status.InProcess)
        .Where(t => t.Started.Value >= beginDate)
        .Where(t => t.Started.Value <= endDate)
        .ToList();
      
      var documents = tasks
        .Where(t => t.DocumentGroup.OfficialDocuments.FirstOrDefault() != null)
        .Select(t => t.DocumentGroup.OfficialDocuments.FirstOrDefault())
        .Distinct()
        .Cast<IOfficialDocument>()
        .ToList();
      
      documents = documents
        .Where(d => d != null)
        .Where(d => DocumentApprovalStatisticsReport.BusinessUnit == null ||
               d.PreparedBy != null && DirRX.Solution.BusinessUnits.Equals(DirRX.Solution.Employees.As(d.PreparedBy).BusinessUnit,
                                                                           DocumentApprovalStatisticsReport.BusinessUnit))
        .Where(d => DocumentApprovalStatisticsReport.Department == null ||
               DirRX.Solution.Departments.Equals(d.Department, DocumentApprovalStatisticsReport.Department))
        .Where(d => DocumentApprovalStatisticsReport.PreparedBy == null ||
               d.PreparedBy != null && DirRX.Solution.Employees.Equals(d.PreparedBy, DocumentApprovalStatisticsReport.PreparedBy))
        .Where(d => !DocumentApprovalStatisticsReport.DocKinds.Any() || DocumentApprovalStatisticsReport.DocKinds.Contains(d.DocumentKind))
        .ToList();
      #endregion
      
      #region Расчет итогов
      var groupID = Sungero.Docflow.PublicConstants.Module.TaskMainGroup.ApprovalTask;
      DocumentApprovalStatisticsReport.TotalCount = documents.Count();
      DocumentApprovalStatisticsReport.OutOfTime = documents
        .Where(d => tasks.Any(t => t.AttachmentDetails.Any(a => a.GroupId == groupID && a.AttachmentId == d.Id) && IsOverdueTask(t)))
        .Count();
      DocumentApprovalStatisticsReport.Percent = 0.0;
      if (DocumentApprovalStatisticsReport.TotalCount > 0)
        DocumentApprovalStatisticsReport.Percent = (double)(DocumentApprovalStatisticsReport.TotalCount - DocumentApprovalStatisticsReport.OutOfTime) /
          (double)DocumentApprovalStatisticsReport.TotalCount * 100.0;
      
      var periodFrom = DocumentApprovalStatisticsReport.BeginDate.HasValue ?
        Reports.Resources.DocumentApprovalStatisticsReport.PeriodFromFormat(DocumentApprovalStatisticsReport.BeginDate.Value.ToShortDateString()) :
        string.Empty;
      var periodTill = DocumentApprovalStatisticsReport.EndDate.HasValue ?
        Reports.Resources.DocumentApprovalStatisticsReport.PeriodTillFormat(DocumentApprovalStatisticsReport.EndDate.Value.ToShortDateString()) :
        string.Empty;
      if (!string.IsNullOrEmpty(periodFrom) || !string.IsNullOrEmpty(periodTill))
        DocumentApprovalStatisticsReport.Period = Reports.Resources.DocumentApprovalStatisticsReport.PeriodFormat(periodFrom, periodTill);
      else
        DocumentApprovalStatisticsReport.Period = string.Empty;
      DocumentApprovalStatisticsReport.Print = Reports.Resources.DocumentApprovalStatisticsReport.PrintFormat(Users.Current, Calendar.UserNow.ToString("G"));
      #endregion
      
      var dataTable = new List<Structures.DocumentApprovalStatisticsReport.TableLine>();
      foreach (var document in documents)
      {
        var currentTasks = tasks.Where(t => t.AttachmentDetails.Any(a => a.GroupId == groupID && a.AttachmentId == document.Id));
        
        foreach (var task in currentTasks)
        {
          var tableLine = Structures.DocumentApprovalStatisticsReport.TableLine.Create();
          
          // ИД и ссылка.
          tableLine.Id = document.Id;
          tableLine.Hyperlink = Sungero.Core.Hyperlinks.Get(OfficialDocuments.Info, document.Id);
          
          // Документ.
          tableLine.DocumentInfo = document.Subject;
          
          // План. дата.
          var date = task.InitDeadline.HasValue ? task.InitDeadline : task.MaxDeadline;
          tableLine.PlanDate = date.HasValue ? date.Value.ToUserTime().ToShortDateString() : string.Empty;
          
          // Факт. дата.
          tableLine.ActualDate = task.Status == Sungero.Workflow.Task.Status.Completed ? task.MaxDeadline.Value.ToUserTime().ToShortDateString() : string.Empty;
          
          // Инициатор согласования.
          var author = DirRX.Solution.Employees.GetAll(emp => DirRX.Solution.Employees.Equals(task.Author, emp)).FirstOrDefault();
          if (author != null && author.Person != null)
            tableLine.Author = author.Person.ShortName;
          else
            tableLine.Author = task.Author.Name;
          
          // Просрок.
          tableLine.Overdue = Sungero.Docflow.PublicFunctions.Module.CalculateDelay(date, task.Status == Sungero.Workflow.Task.Status.Completed ? task.MaxDeadline.Value : Calendar.Today, Users.Current);
          
          tableLine.ReportSessionId = DocumentApprovalStatisticsReport.ReportSessionId;
          
          dataTable.Add(tableLine);
        }
      }
      
      Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.DocumentApprovalStatisticsReport.SourceTableName, dataTable);
    }
    
    /// <summary>
    /// Проверяет просроченна ли задача.
    /// </summary>
    /// <param name="task">Задача для проверки.</param>
    /// <returns>Признак.</returns>
    public bool IsOverdueTask(DirRX.Solution.IApprovalTask task)
    {
      if (task == null)
        return false;
      if (task.Status == Sungero.Workflow.Task.Status.Completed)
      {
        var completed = Assignments.GetAll()
          .Where(a => Tasks.Equals(task, a.Task))
          .Where(a => a.Completed.HasValue)
          .Max(a => a.Completed);
        if (completed.HasValue)
          return task.MaxDeadline.HasValue && task.MaxDeadline.Value < completed;
        else
          return false;
      }
      else
        return task.MaxDeadline.HasValue && task.MaxDeadline.Value < Calendar.Now;
    }
  }
}