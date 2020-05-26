using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.ActionItemRejectionAssignment;

namespace DirRX.ActionItems
{
	partial class ActionItemRejectionAssignmentServerHandlers
	{

		public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
		{
			if (_obj.ActionItemDeadline > _obj.ReportDeadline)
				e.AddError(_obj.Info.Properties.ActionItemDeadline, DirRX.Solution.ActionItemExecutionTasks.Resources.IncorrectValidFinalDeadlineDate, _obj.Info.Properties.ReportDeadline);
		}
	}

}