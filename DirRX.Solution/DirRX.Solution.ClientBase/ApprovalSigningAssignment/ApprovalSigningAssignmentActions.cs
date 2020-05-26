using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalSigningAssignment;

namespace DirRX.Solution.Client
{
	partial class ApprovalSigningAssignmentActions
	{
		public override void Sign(Sungero.Workflow.Client.ExecuteResultActionArgs e)
		{
			base.Sign(e);
			
			if (_obj.DocumentGroup.OfficialDocuments.Any())
				FillFieldsDoc();
		}

		public override bool CanSign(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
		{
			return base.CanSign(e);
		}

		public override void ConfirmSign(Sungero.Workflow.Client.ExecuteResultActionArgs e)
		{
			base.ConfirmSign(e);
			
			if (_obj.DocumentGroup.OfficialDocuments.Any())
				FillFieldsDoc();
			
		}

		public override bool CanConfirmSign(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
		{
			return base.CanConfirmSign(e);
		}

		public virtual void CreateInitiatorTaskAction(Sungero.Domain.Client.ExecuteActionArgs e)
		{
			var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
			string subject = document == null ? string.Empty : LocalActs.RequestInitiatorTasks.Resources.ThemeInitiatorTaskFormat(document.Name);
			var task = LocalActs.PublicFunctions.RequestInitiatorTask.Remote.CreateNewRequestInitiatorTask(subject, _obj);
			task.Show();
		}

		public virtual bool CanCreateInitiatorTaskAction(Sungero.Domain.Client.CanExecuteActionArgs e)
		{
			return _obj.Status == Status.InProcess;
		}

		public virtual void Recycling(Sungero.Domain.Client.ExecuteActionArgs e)
		{
			Functions.Module.Recycling(_obj, e, Sungero.Docflow.ApprovalSigningAssignment.Result.ForRevision);
		}

		public virtual bool CanRecycling(Sungero.Domain.Client.CanExecuteActionArgs e)
		{
			return _obj.DocumentGroup.OfficialDocuments.Any() && !_obj.Completed.HasValue;
		}

		public virtual void AddSubscribers(Sungero.Domain.Client.ExecuteActionArgs e)
		{
			var subscribers = DirRX.Solution.PublicFunctions.Module.GetSelectedEmployees(_obj.Subscribers.Select(s => s.Subscriber).ToList());
			if (subscribers.Any())
			{
				var isSend = DirRX.ActionItems.PublicFunctions.SendNoticeQueueItem.Remote.CreateSendNoticeQueueItem(subscribers.ToList(), null, _obj);
				if (isSend)
				{
					foreach (var subscriber in subscribers)
					{
						var newSubscriber = _obj.Subscribers.AddNew();
						newSubscriber.Subscriber = subscriber;
					}
					
					_obj.Save();
				}
				else
					e.AddError(DirRX.ActionItems.Resources.AddSubscriberError);
			}
		}

		public virtual bool CanAddSubscribers(Sungero.Domain.Client.CanExecuteActionArgs e)
		{
			return _obj.AccessRights.CanUpdate() && _obj.Status == Status.InProcess;
		}
		
		/// <summary>
		/// Заполнить поля "Подписал" и "За кого" в карточке документа во вложении.
		/// </summary>
		private void FillFieldsDoc()
		{
			var doc = _obj.DocumentGroup.OfficialDocuments.First();
			
			var outgoingLetter = DirRX.Solution.OutgoingLetters.As(doc);
			if (outgoingLetter != null)
			{
				outgoingLetter.OurSignatory = _obj.SignedDirRX;
				outgoingLetter.ForWhomDirRX = _obj.ForWhomDirRX;
			}
			else
				doc.OurSignatory = _obj.SignedDirRX;

			doc.Save();
		}
	}
}