using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalCheckingAssignment;

namespace DirRX.Solution
{
  partial class ApprovalCheckingAssignmentSignatoryPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> SignatoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var signatories = Functions.ApprovalCheckingAssignment.GetSignatories(document).Select(s => s.Employee).Distinct().ToList();
      return query.Where(s => signatories.Contains(s));
    }
  }


  partial class ApprovalCheckingAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      base.BeforeComplete(e);
      
      if (_obj.ForRecycle.GetValueOrDefault())
        e.Result = DirRX.Solution.ApprovalAssignments.Resources.ForRecycleResultName;
      else if (_obj.Denied.GetValueOrDefault())
      {
        e.Result = DirRX.Solution.ApprovalCheckingAssignments.Resources.DeniedResultName;
        // Тема - Согласование прекращено по причине: <Комментарий>.
        var subject = DirRX.Solution.ApprovalCheckingAssignments.Resources.DeniedSubjectTextFormat(_obj.ActiveText);
        PublicFunctions.Module.Remote.CreateAuthorNotice(_obj, subject);
        PublicFunctions.Module.Remote.AbortTask(_obj.Task.Id);
      }
      else if (_obj.Result == Result.ForRework)
        e.Result = ApprovalTasks.Resources.ForRework;
      else if (_obj.Result == Result.Accept)
      {
        var stage = Functions.ApprovalTask.GetStage(Solution.ApprovalTasks.As(_obj.Task), ApprovalStage.StageType.SimpleAgr);
        if (stage.IsSubjectTransactionConfirmation.GetValueOrDefault() && _obj.SubjectTransaction.HasValue)
        {
          e.Result = DirRX.Solution.ApprovalCheckingAssignments.Resources.SubjectTransactionConfirmationResultFormat(
            DirRX.Solution.ApprovalCheckingAssignments.Info.Properties.SubjectTransaction.GetLocalizedValue(_obj.SubjectTransaction.Value));
        }
        else if (_obj.CompletedBy.IsSystem.HasValue && _obj.CompletedBy.IsSystem.Value)
          e.Result = DirRX.Solution.ApprovalSendingAssignments.Resources.JobExecutedAutomatically;
      }
    }

    public override void Saved(Sungero.Domain.SavedEventArgs e)
    {
      base.Saved(e);
      if (_obj.State.IsInserted)
      {
        // Создание нового задания может изменить срок задачи.
        _obj.Task.MaxDeadline = Functions.ApprovalTask.GetExpectedDate(ApprovalTasks.As(_obj.Task));
      }
    }
    
    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      _obj.ForRecycle = false;
      _obj.Denied = false;
      _obj.SubjectTransaction = DirRX.Solution.ApprovalCheckingAssignment.SubjectTransaction.Another;
    }
  }

}