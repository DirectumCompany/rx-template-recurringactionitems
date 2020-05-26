using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;
using Sungero.Metadata;
using DirRX.Solution.AcquaintanceTask;

namespace DirRX.Solution.Server
{
  partial class AcquaintanceTaskFunctions
  {
    /// <summary>
    /// Найти задачи на согласование по документу.
    /// </summary>
    /// <returns>Список задач.</returns>
    [Public]
    public static IQueryable<Sungero.Docflow.IApprovalTask> GetApprovalTasks(Sungero.Docflow.IOfficialDocument document)
    {
      var docGuid = document.GetEntityMetadata().GetOriginal().NameGuid;
      var approvalTaskDocumentGroupGuid = Sungero.Docflow.Constants.Module.TaskMainGroup.ApprovalTask;
      return ApprovalTasks.GetAll()
        .Where(t => t.Status == Sungero.Workflow.Task.Status.InProcess ||
               t.Status == Sungero.Workflow.Task.Status.Completed ||
               t.Status == Sungero.Workflow.Task.Status.UnderReview)
        .Where(t => t.AttachmentDetails
               .Any(att => att.AttachmentId == document.Id && att.EntityTypeGuid == docGuid &&
                    att.GroupId == approvalTaskDocumentGroupGuid));
    }
    
    /// <summary>
    /// Получить всех сотрудников, которые участвовали в согласовании.
    /// </summary>
    /// <returns>Список пользователей.</returns>
    [Public]
    public static List<IUser> GetAllApproversAndSignatories(IApprovalTask task)
    {
      var approvalBlocks = 6;
      return Sungero.Workflow.Assignments.GetAll()
        .Where(a => Equals(a.Task, task))
        .Where(a => a.Status == Sungero.Workflow.AssignmentBase.Status.Completed)
        .Where(a => approvalBlocks == a.BlockId)
        .Select(a => a.Performer)
        .Distinct().ToList();
    }
    
    /// <summary>
    /// Получить несистемные активные записи исполнителей.
    /// </summary>
    /// <returns>Несистемные активные записи исполнителей.</returns>
    [Remote(IsPure = true), Public]
    public List<Sungero.Company.IEmployee> GetActivePerformers()
    {
      var recipients = _obj.Performers.Select(x => x.Performer).ToList();
      var activeRecipients = recipients.Where(x => x != null && x.Status == Sungero.CoreEntities.DatabookEntry.Status.Active).ToList();
      var performers = Sungero.Docflow.PublicFunctions.Module.Remote.GetEmployeesFromRecipients(activeRecipients)
        .Where(e => e.IsSystem != true && e.Status == Sungero.CoreEntities.DatabookEntry.Status.Active && e.Login != null && e.Login.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
        .Distinct()
        .ToList();
      
      return performers;
    }
  }
}