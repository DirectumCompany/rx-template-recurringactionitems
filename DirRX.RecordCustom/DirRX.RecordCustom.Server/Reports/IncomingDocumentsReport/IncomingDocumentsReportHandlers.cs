using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution;

namespace DirRX.RecordCustom
{
  partial class IncomingDocumentsReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      // Удалить временные данные из таблицы.
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.IncomingDocumentsReport.IncomingDocumentsReportTableName, IncomingDocumentsReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var filter = new System.Text.StringBuilder();
      if (IncomingDocumentsReport.BeginDate.HasValue)
        filter.AppendLine(DirRX.RecordCustom.Reports.Resources.IncomingDocumentsReport.BeginDateFilterFormat(IncomingDocumentsReport.BeginDate.Value.ToShortDateString()));
      if (IncomingDocumentsReport.EndDate.HasValue)
        filter.AppendLine(DirRX.RecordCustom.Reports.Resources.IncomingDocumentsReport.EndDateFilterFormat(IncomingDocumentsReport.EndDate.Value.ToShortDateString()));
      if (IncomingDocumentsReport.BusinessUnit != null)
        filter.AppendLine(DirRX.RecordCustom.Reports.Resources.IncomingDocumentsReport.BusinessUnitFilterFormat(IncomingDocumentsReport.BusinessUnit));
      if (IncomingDocumentsReport.Department != null)
        filter.AppendLine(DirRX.RecordCustom.Reports.Resources.IncomingDocumentsReport.DepartmentFilterFormat(IncomingDocumentsReport.Department));
      if (IncomingDocumentsReport.Correspondent != null)
        filter.AppendLine(DirRX.RecordCustom.Reports.Resources.IncomingDocumentsReport.CorrespondentFilterFormat(IncomingDocumentsReport.Correspondent));
      if (IncomingDocumentsReport.CorrespondentDepartment != null)
        filter.AppendLine(DirRX.RecordCustom.Reports.Resources.IncomingDocumentsReport.CorrespondentDepartmentFilterFormat(IncomingDocumentsReport.CorrespondentDepartment));
      if (IncomingDocumentsReport.SignedBy != null)
        filter.AppendLine(DirRX.RecordCustom.Reports.Resources.IncomingDocumentsReport.SignedByFilterFormat(IncomingDocumentsReport.SignedBy));
      IncomingDocumentsReport.ParamsDescriprion = filter.ToString();
      
      IncomingDocumentsReport.ReportSessionId = Guid.NewGuid().ToString();
      IncomingDocumentsReport.ReportDate = Calendar.Now;
      
      var documents = DirRX.Solution.IncomingLetters.GetAll()
        .Where(d => d.LifeCycleState == DirRX.Solution.IncomingLetter.LifeCycleState.Draft || d.LifeCycleState == DirRX.Solution.IncomingLetter.LifeCycleState.Active)
        .Where(d => d.RegistrationDate.HasValue)
        .Where(d => d.RegistrationDate >= IncomingDocumentsReport.BeginDate.Value)
        .Where(d => !IncomingDocumentsReport.EndDate.HasValue || d.RegistrationDate <= IncomingDocumentsReport.EndDate.Value)
        .Where(d => IncomingDocumentsReport.BusinessUnit == null || DirRX.Solution.BusinessUnits.Equals(d.BusinessUnit, IncomingDocumentsReport.BusinessUnit))
        .Where(d => IncomingDocumentsReport.Department == null || DirRX.Solution.Departments.Equals(d.Department, IncomingDocumentsReport.Department))
        .Where(d => IncomingDocumentsReport.Correspondent == null || DirRX.Solution.Companies.Equals(d.Correspondent, IncomingDocumentsReport.Correspondent))
        .Where(d => IncomingDocumentsReport.SignedBy == null || DirRX.Solution.Contacts.Equals(d.SignedBy, IncomingDocumentsReport.SignedBy))
        .Where(d => IncomingDocumentsReport.CorrespondentDepartment == null || d.CorrespondentDepDirRX.Select(x => x.Department).Any(x => DirRX.IntegrationLLK.DepartCompanieses.Equals(x, IncomingDocumentsReport.CorrespondentDepartment)));
      
      Logger.DebugFormat("Всего документов: {0}", documents.Count());
      // Guid группы вложений для документа в поручении.
      var documentsGroupGuid = Sungero.Docflow.PublicConstants.Module.TaskMainGroup.ActionItemExecutionTask;
      
      var dataTable = new List<Structures.IncomingDocumentsReport.TableLine>();
      if (documents.Any())
      {
        foreach (var document in documents)
        {
          var tasks = DirRX.Solution.ActionItemExecutionTasks.GetAll()
            .Where(t => t.Status == Sungero.Workflow.Task.Status.Completed || t.Status == Sungero.Workflow.Task.Status.InProcess)
            .Where(t => t.AttachmentDetails.Any(a => a.GroupId == documentsGroupGuid && document.Id == a.AttachmentId))
            .Where(t => t.IsCompoundActionItem != true && t.ActionItemType != Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Additional)
            .ToList();
          if (tasks.Any())
          {
            foreach (var task in tasks)
            {
              var tableLine = FillDocData(document);
              
              var parentAssignment = task.ParentAssignment;
              if (parentAssignment != null && DirRX.Solution.ActionItemExecutionTasks.Is(parentAssignment.Task))
                continue;
              
              // Исполнители.
              tableLine.Assignee = task.Assignee.Person.ShortName;
              tableLine.CoAssignees = string.Join(", ", task.CoAssignees.Select(ca => ca.Assignee.Person.ShortName).ToList());
              tableLine.ActionItemText = task.ActionItem;
              tableLine.ReportingPeriod = task.ReportDeadline.HasValue ? task.ReportDeadline.Value.ToString("d") : string.Empty;
              
              // Сроки и даты.
              if (task.Deadline.HasValue)
                tableLine.Deadline = task.Deadline.Value;
              tableLine.PerformDate = string.Empty;
              if (task.ActualDate.HasValue)
                tableLine.PerformDate = task.ActualDate.Value.ToShortDateString();
              
              // Статус.
              tableLine.Status = string.Empty;
              if (task.ExecutionState != null)
                tableLine.Status = DirRX.Solution.ActionItemExecutionTasks.Info.Properties.ExecutionState.GetLocalizedValue(task.ExecutionState.Value);
              
              //Результат выполнения.
              var assignment = DirRX.Solution.ActionItemExecutionAssignments.GetAll().Where(a => a.Task.Id == task.Id).FirstOrDefault();
              
              tableLine.Result = string.Empty;
              if (assignment != null)
              {
                tableLine.Result = assignment.ActiveText;
                
                if (assignment.Status == Sungero.Workflow.Task.Status.Completed)
                {
                  var endDate = task.ActualDate.HasValue ? task.ActualDate.Value : Calendar.Now;
                  tableLine.Overdue = Sungero.Docflow.PublicFunctions.Module.CalculateDelay(assignment.Deadline, endDate, task.Assignee);
                }
                else
                  tableLine.Overdue = Sungero.Docflow.PublicFunctions.Module.CalculateDelay(assignment.Deadline, Calendar.Now, task.Assignee);
                
                
              }
              
              //Вложение "Результат исполнения"
              if (task.ResultGroup.OfficialDocuments.Any())
              {
                var outgoingLetter = task.ResultGroup.OfficialDocuments.First();
                
                tableLine.OutgoingDocID = outgoingLetter.Id;
                tableLine.OutgoingDocHyperlink = Hyperlinks.Get(outgoingLetter.Info, outgoingLetter.Id);
              }
              
              dataTable.Add(tableLine);
            }
          }
          else
            dataTable.Add(FillDocData(document));
        }
      }
      
      Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.IncomingDocumentsReport.IncomingDocumentsReportTableName, dataTable);
    }
    
    /// <summary>
    /// Заполнить строку данными по документу.
    /// </summary>
    /// <param name="document">Входящий документ.</param>
    /// <returns></returns>
    private Structures.IncomingDocumentsReport.TableLine FillDocData (DirRX.Solution.IIncomingLetter document)
    {
      var tableLine = Structures.IncomingDocumentsReport.TableLine.Create();
      tableLine.ReportSessionId = IncomingDocumentsReport.ReportSessionId;
      tableLine.DocID = document.Id;
      tableLine.Hyperlink = Sungero.Core.Hyperlinks.Get(IncomingLetters.Info, document.Id);
      tableLine.RegNumber = document.RegistrationNumber;
      tableLine.RegDate = document.RegistrationDate.HasValue ? document.RegistrationDate.Value : (DateTime?)null;
      tableLine.CorrespondentNumber = document.InNumber;
      tableLine.DocumentKind = document.DocumentKind != null ? document.DocumentKind.ShortName : string.Empty;
      tableLine.Addressee = document.Addressee != null ? document.Addressee.DisplayValue : string.Empty;
      
      if (document.Dated.HasValue)
        tableLine.CorrespondentDate = document.Dated.Value;
      
      tableLine.Correspondent = string.Empty;
      if (document.Correspondent != null)
        tableLine.Correspondent = document.Correspondent.DisplayValue;
      
      tableLine.CorrespondentDepartment = string.Empty;
      if (document.CorrespondentDepDirRX.Select(x => x.Department != null).Any())
        tableLine.CorrespondentDepartment = string.Join(", ", document.CorrespondentDepDirRX.Select(x => x.Department.Name).ToArray());
      
      tableLine.SignedBy = string.Empty;
      if (document.SignedBy != null)
        tableLine.SignedBy = document.SignedBy.DisplayValue;
      
      tableLine.Subject = document.Subject;
      
      return tableLine;
    }
  }
}