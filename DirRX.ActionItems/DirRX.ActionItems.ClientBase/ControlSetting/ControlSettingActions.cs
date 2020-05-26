using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.ControlSetting;

namespace DirRX.ActionItems.Client
{
	partial class ControlSettingActions
	{
		public virtual void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
		{
			var duplicates = Functions.ControlSetting.Remote.GetDuplicates(_obj);
			if (duplicates.Any())
				duplicates.Show();
			else
				Dialogs.NotifyMessage(Sungero.Commons.Resources.DuplicateNotFound);
		}

		public virtual bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
		{
			return true;
		}

	}

}