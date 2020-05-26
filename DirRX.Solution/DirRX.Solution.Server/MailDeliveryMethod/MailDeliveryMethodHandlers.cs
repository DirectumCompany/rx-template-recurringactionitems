using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.MailDeliveryMethod;

namespace DirRX.Solution
{
	partial class MailDeliveryMethodServerHandlers
	{

		public override void Created(Sungero.Domain.CreatedEventArgs e)
		{
			base.Created(e);
			_obj.IsRequireContactInContract = false;
		}
	}

}