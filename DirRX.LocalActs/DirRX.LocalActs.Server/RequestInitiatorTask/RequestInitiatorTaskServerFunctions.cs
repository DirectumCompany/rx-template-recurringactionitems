using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.RequestInitiatorTask;

namespace DirRX.LocalActs.Server
{
  partial class RequestInitiatorTaskFunctions
  {

    /// <summary>
    /// Создать и запустить новую задачу
    /// </summary>
    [Remote(PackResultEntityEagerly = true), Public]
    public static IRequestInitiatorTask CreateNewRequestInitiatorTask(string subject, Sungero.Workflow.IAssignmentBase assignment)
    {
      var approvalTask = Solution.ApprovalTasks.As(assignment.MainTask);
      var task = DirRX.LocalActs.RequestInitiatorTasks.CreateAsSubtask(assignment);
      
      if (subject.Length > task.Info.Properties.Subject.Length)
        subject = subject.Substring(0, task.Info.Properties.Subject.Length);
      task.Subject = subject;
      
      task.Author = assignment.Performer;
      task.MainAssignmentDefaultDeadline = assignment.Deadline;
      task.MaxDeadline = assignment.Deadline;
      task.Assignee = Solution.Employees.As(assignment.MainTask.StartedBy);
      task.AttachmentGroup.OfficialDocuments.Add(approvalTask.DocumentGroup.OfficialDocuments.FirstOrDefault());
      foreach (var risk in approvalTask.RiskAttachmentGroup.Risks)
        task.RiskAttachmentGroup.Risks.Add(risk);
      foreach (var item in approvalTask.OtherGroup.All)
        task.OtherGroup.All.Add(item);
      foreach (var doc in approvalTask.AddendaGroup.OfficialDocuments)
        task.AddendaGroup.OfficialDocuments.Add(doc);
      return task;
    }

  }
}