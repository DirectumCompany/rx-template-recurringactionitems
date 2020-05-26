using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;
using DirRX.LocalActs.RequestInitiatorTask;
using DirRX.Solution;

namespace DirRX.LocalActs.Server
{
  partial class RequestInitiatorTaskRouteHandlers
  {

    public virtual void StartAssignment3(DirRX.LocalActs.IRequestInitiatorAssingment assignment, DirRX.LocalActs.Server.RequestInitiatorAssingmentArguments e)
    {
      assignment.Deadline = _obj.MaxDeadline;
    }

    public virtual void CompleteAssignment7(DirRX.LocalActs.IRequestInitiatorCheckAssignment assignment, DirRX.LocalActs.Server.RequestInitiatorCheckAssignmentArguments e)
    {
      if (assignment.Result == DirRX.LocalActs.RequestInitiatorCheckAssignment.Result.Complete)
      {
        var accountingDocuments = Solution.ApprovalTasks.As(assignment.MainTask).OtherGroup.All;
        var documentsToAdd = assignment.OtherGroup.All.Where(d => !accountingDocuments.Contains(d)).ToList();
        foreach (var document in documentsToAdd)
          Solution.ApprovalTasks.As(assignment.MainTask).OtherGroup.All.Add(document);
      }
    }

    public virtual void StartBlock7(DirRX.LocalActs.Server.RequestInitiatorCheckAssignmentArguments e)
    {
      var document = _obj.AttachmentGroup.OfficialDocuments.FirstOrDefault();
      e.Block.Subject = RequestInitiatorTasks.Resources.ThemeInitiatorCheckAssingmentFormat(document.Name);
      e.Block.Performers.Add(_obj.Author);
    }

    public virtual void StartBlock3(DirRX.LocalActs.Server.RequestInitiatorAssingmentArguments e)
    {
      var document = _obj.AttachmentGroup.OfficialDocuments.FirstOrDefault();
      e.Block.Subject = RequestInitiatorTasks.Resources.ThemeInitiatorAssingmentFormat(document.Name);
      e.Block.Performers.Add(_obj.Assignee);
    }

  }
}