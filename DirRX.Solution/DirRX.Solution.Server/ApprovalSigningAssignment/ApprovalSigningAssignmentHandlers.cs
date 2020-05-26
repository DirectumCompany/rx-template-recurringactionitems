using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalSigningAssignment;

namespace DirRX.Solution
{
	partial class ApprovalSigningAssignmentSignedDirRXPropertyFilteringServerHandler<T>
	{

		public virtual IQueryable<T> SignedDirRXFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
		{
			var signEmployees = Functions.ApprovalSigningAssignment.GetSignatureEmployees(_obj);
			
			if (signEmployees != null)
				query = query.Where(x => signEmployees.Contains(x.Id));
			
			return query;
		}
	}

	partial class ApprovalSigningAssignmentForWhomDirRXPropertyFilteringServerHandler<T>
	{

		public virtual IQueryable<T> ForWhomDirRXFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
		{
			var signEmployees = Functions.ApprovalSigningAssignment.GetSignatureEmployees(_obj);
			
			if (signEmployees != null)
				query = query.Where(x => signEmployees.Contains(x.Id));
			
			return query;
		}
	}

	partial class ApprovalSigningAssignmentServerHandlers
	{

		public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
		{
			base.BeforeComplete(e);
			
			if (_obj.ForRecycle.GetValueOrDefault())
				e.Result = DirRX.Solution.ApprovalAssignments.Resources.ForRecycleResultName;
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
		}
	}

}