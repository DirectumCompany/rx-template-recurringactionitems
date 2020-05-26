using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.DocumentReviewTask;

namespace DirRX.Solution
{
	partial class DocumentReviewTaskServerHandlers
	{

		public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
		{
			base.BeforeStart(e);
			
			if (_obj.SubcribersDirRX.Count > 0)
			{
				Functions.DocumentReviewTask.SubscribersGrantAccessRights(_obj);
				Functions.DocumentReviewTask.SendNotificationToSubcribersOnStart(_obj);
			}
			
			FillFieldOnIncomingDoc();
			
		}
		
		/// <summary>
		/// Заполнение поля "На рассмотрение" в документе "Входящее письмо" во вложении.
		/// </summary>
		private void FillFieldOnIncomingDoc()
		{
			if (_obj.DocumentForReviewGroup.OfficialDocuments.Any())
			{
				var incomingDoc = DirRX.Solution.IncomingLetters.As(_obj.DocumentForReviewGroup.OfficialDocuments.First());
				
				if (incomingDoc != null)
				{
					incomingDoc.Addressee = DirRX.Solution.Employees.As(_obj.Addressee);
					incomingDoc.Save();
				}
			}
		}
	}


}