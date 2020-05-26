using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalTask;

namespace DirRX.Solution
{
	partial class ApprovalTaskSharedHandlers
	{

		public override void DocumentGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
		{
			base.DocumentGroupAdded(e);
			var order = Solution.Orders.As(_obj.DocumentGroup.OfficialDocuments.FirstOrDefault());
			_obj.NeedPaperSigning = order != null ? order.StandardForm.NeedPaperSigning : false;
			// Признак "Согласование с ПАО ЛУКОЙЛ" оставить видимым только для ТД Приказы.
			_obj.State.Properties.NeedLUKOILApproval.IsVisible = order != null;
			var contract = Sungero.Docflow.ContractualDocumentBases.As(_obj.DocumentGroup.OfficialDocuments.FirstOrDefault());
			_obj.State.Properties.NeedPaperSigning.IsVisible = contract == null;
		}
	}
}