using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ReviewManagerAssignment;

namespace DirRX.Solution.Client
{
	partial class ReviewManagerAssignmentActions
	{
		public virtual void AddSubscriberDirRX(Sungero.Domain.Client.ExecuteActionArgs e)
		{
			
			var subscribers = DirRX.Solution.PublicFunctions.Module.GetSelectedEmployees(DirRX.Solution.Functions.DocumentReviewTask.Remote.GetCurrentSubscribers(DirRX.Solution.DocumentReviewTasks.As(_obj.Task))).ToList();
			
			if (subscribers.Any())
			{
				DirRX.ActionItems.PublicFunctions.SendNoticeQueueItem.Remote.CreateSendNoticeQueueItem(subscribers, null, _obj);
			}
		}

		public virtual bool CanAddSubscriberDirRX(Sungero.Domain.Client.CanExecuteActionArgs e)
		{
			return true;
		}

	}

}