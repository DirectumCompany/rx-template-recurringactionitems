using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.ControlSetting;

namespace DirRX.ActionItems
{
	partial class ControlSettingServerHandlers
	{

		public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
		{
			if (Functions.ControlSetting.HaveDuplicates(_obj))
				e.AddError(Sungero.Commons.Resources.DuplicateDetected, _obj.Info.Actions.ShowDuplicates);			
		}
	}

	partial class ControlSettingSupervisorPropertyFilteringServerHandler<T>
	{

		public virtual IQueryable<T> SupervisorFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
		{
			var possibleRoles = ActionItems.PublicFunctions.ActionItemsRole.GetPossibleRoles(_obj.Info);
			
			return query.Where(r => possibleRoles.Contains(r.Type) && r.Type != ActionItemsRole.Type.Secretary);
		}
	}

	partial class ControlSettingInitiatorPropertyFilteringServerHandler<T>
	{

		public virtual IQueryable<T> InitiatorFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
		{
			var possibleRoles = ActionItems.PublicFunctions.ActionItemsRole.GetPossibleRoles(_obj.Info);
			
			return query.Where(r => possibleRoles.Contains(r.Type) &&
			                   r.Type != ActionItemsRole.Type.InitManager &&
			                   r.Type != ActionItemsRole.Type.InitCEOManager);
		}
	}

}