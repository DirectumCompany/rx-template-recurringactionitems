using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.StatusReportRequestTask;

namespace DirRX.Solution.Server
{
  partial class StatusReportRequestTaskFunctions
  {
    /// <summary>
    /// Создать Запрос отчета.
    /// </summary>
    /// <param name="task">Поручение, для которого нужен отчет.</param>
    /// <returns>Задача "Запрос отчета по поручению".</returns>
    [Remote(PackResultEntityEagerly = true)]
    public static IStatusReportRequestTask CreateStatusReportRequest(IActionItemExecutionTask task)
    {
      #region Из коробки.
      var performers = Functions.ActionItemExecutionTask.GetActionItemsPerformersDir(task).ToList();
      if (!performers.Any())
        return null;
      
      var statusReportRequest = StatusReportRequestTasks.CreateAsSubtask(task);
      statusReportRequest.ActionItem = task.ActionItem;
      var document = task.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
        statusReportRequest.DocumentsGroup.OfficialDocuments.Add(document);
      statusReportRequest.ActiveText = StatusReportRequestTasks.Resources.ReportFromJob;
      #endregion

      if (task.IsCompoundActionItem ?? false)
      {
        // Всегда присваиваем, чтобы не сломался механизм приемки отчета об исполнении.
        statusReportRequest.Assignee = Sungero.Company.Employees.As(performers.First());
        if (performers.Count > 1)
        {
          statusReportRequest.Assignee = Sungero.Company.Employees.As(performers.First());
          statusReportRequest.IsManyAssignees = true;
          foreach (var performer in performers)
          {
            statusReportRequest.Assignees.AddNew().Assignee = Sungero.Company.Employees.As(performer);
          }
        }
        
        statusReportRequest.Subject = string.Format("{0} {1}", StatusReportRequestTasks.Resources.ReportRequestTaskSubject, task.Subject);
      }
      else
      {
        statusReportRequest.Assignee = task.Assignee;
        statusReportRequest.Subject = Functions.StatusReportRequestTask.GetStatusReportRequestSubjectDir(statusReportRequest, StatusReportRequestTasks.Resources.ReportRequestTaskSubject);
      }

      if (statusReportRequest.Subject.Length > statusReportRequest.Info.Properties.Subject.Length)
        statusReportRequest.Subject = statusReportRequest.Subject.Substring(0, statusReportRequest.Info.Properties.Subject.Length);
      
      return statusReportRequest;
    }
  }
}