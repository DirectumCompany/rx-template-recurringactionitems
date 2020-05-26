using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PeriodicActionItemsTemplate.Category;

namespace DirRX.PeriodicActionItemsTemplate
{

	partial class CategoryServerHandlers
	{

		public override void Created(Sungero.Domain.CreatedEventArgs e)
		{
			if (!_obj.State.IsCopied)
			{
				_obj.NeedsReportDeadline = false;
				_obj.IsCEOActionItem = false;
			}
		}
	}

}