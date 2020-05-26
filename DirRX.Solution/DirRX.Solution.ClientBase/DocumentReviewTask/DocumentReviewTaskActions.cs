using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.DocumentReviewTask;

namespace DirRX.Solution.Client
{
	partial class DocumentReviewTaskActions
	{

		public virtual void AddSubcribersDirRX(Sungero.Domain.Client.ExecuteActionArgs e)
		{
			var currentSubscribers = _obj.SubcribersDirRX.Select(s => s.Subcriber).Cast<DirRX.Solution.IEmployee>().ToList();
			
			var subscribers = DirRX.Solution.PublicFunctions.Module.GetSelectedEmployees(currentSubscribers).ToList();
			
			if (subscribers.Any())
			{
				if (Functions.DocumentReviewTask.Remote.SendNotificationToSubcribers(_obj, subscribers))
				{
					_obj.Save();
					e.AddInformation(DocumentReviewTasks.Resources.SendedSubcribersNotification);
				}
				else
					e.AddError(DocumentReviewTasks.Resources.ErrorOnSendingNotice);
			}
		}

		public virtual bool CanAddSubcribersDirRX(Sungero.Domain.Client.CanExecuteActionArgs e)
		{
			return _obj.AccessRights.CanUpdate() &&
				(_obj.Status == DocumentReviewTask.Status.InProcess ||
				 _obj.Status == DocumentReviewTask.Status.UnderReview);
		}

		public override void PasteAttachment(Sungero.Domain.Client.ExecuteActionArgs e)
		{
			base.PasteAttachment(e);
		}

		public override bool CanPasteAttachment(Sungero.Domain.Client.CanExecuteActionArgs e)
		{
			return base.CanPasteAttachment(e);
		}

	}

}