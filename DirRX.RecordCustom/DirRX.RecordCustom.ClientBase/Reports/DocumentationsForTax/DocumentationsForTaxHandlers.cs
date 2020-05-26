using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.RecordCustom
{
	partial class DocumentationsForTaxClientHandlers
	{

		public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
		{
			var dialog =  Dialogs.CreateInputDialog(RecordCustom.Reports.Resources.DocumentationsForTax.DialogTitle);
			var beginDate = dialog.AddDate(Reports.Resources.DocumentationsForTax.BeginDate,true);
			var endDate = dialog.AddDate(Reports.Resources.DocumentationsForTax.EndDate, true);
			
			if (dialog.Show() != DialogButtons.Ok)
				e.Cancel = true;
			else
			{
				DocumentationsForTax.BeginDate = beginDate.Value.Value.ToString("d");
				DocumentationsForTax.EndDate = endDate.Value.Value.ToString("d");
			}
		}
	}
}