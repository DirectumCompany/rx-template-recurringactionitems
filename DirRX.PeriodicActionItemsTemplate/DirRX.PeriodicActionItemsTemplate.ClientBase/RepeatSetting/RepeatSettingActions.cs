using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PeriodicActionItemsTemplate.RepeatSetting;

namespace DirRX.PeriodicActionItemsTemplate.Client
{
	partial class RepeatSettingActions
	{
		public virtual void AddPerformer(Sungero.Domain.Client.ExecuteActionArgs e)
		{
			var recipients = Sungero.CoreEntities.Recipients.GetAll().Where(x => x.Status == Sungero.CoreEntities.DatabookEntry.Status.Active &&
			                                                                x.Sid != Sungero.Domain.Shared.SystemRoleSid.Administrators &&
			                                                                x.Sid != Sungero.Domain.Shared.SystemRoleSid.Auditors &&
			                                                                x.Sid != Sungero.Domain.Shared.SystemRoleSid.ConfigurationManagers &&
			                                                                x.Sid != Sungero.Domain.Shared.SystemRoleSid.ServiceUsers &&
			                                                                x.Sid != Sungero.Domain.Shared.SystemRoleSid.SoloUsers &&
			                                                                x.Sid != Sungero.Domain.Shared.SystemRoleSid.DeliveryUsersSid &&
			                                                                x.Sid != Sungero.Domain.Shared.SystemRoleSid.AllUsers &&
			                                                                x.Sid != Sungero.Projects.PublicConstants.Module.RoleGuid.ParentProjectTeam &&
			                                                                Groups.Is(x));
			var performer = recipients.ShowSelect();
			if (performer != null)
			{
				var error = DirRX.PeriodicActionItemsTemplate.PublicFunctions.RepeatSetting.Remote.SetRecipientsToAssignees(_obj, performer);
				if (error == Sungero.RecordManagement.ActionItemExecutionTasks.Resources.BigGroupWarningFormat(Sungero.RecordManagement.PublicConstants.ActionItemExecutionTask.MaxCompoundGroup))
					Dialogs.NotifyMessage(error);
			}
		}

		public virtual bool CanAddPerformer(Sungero.Domain.Client.CanExecuteActionArgs e)
		{
			return _obj.IsCompoundActionItem == true;
		}

		public virtual void ChangeCompoundMode(Sungero.Domain.Client.ExecuteActionArgs e)
		{
			if (_obj.IsCompoundActionItem == true)
			{
				if (_obj.ActionItemsParts.Count(a => a.Assignee != null) > 1 || _obj.ActionItemsParts.Any(a => a.Deadline != null || !string.IsNullOrEmpty(a.ActionItemPart)))
				{
					var dialog = Dialogs.CreateTaskDialog(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ChangeCompoundModeQuestion,
					                                      Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ChangeCompoundModeDescription,
					                                      MessageType.Question);
					dialog.Buttons.AddYesNo();
					dialog.Buttons.Default = DialogButtons.No;
					var yesResult = dialog.Show() == DialogButtons.Yes;
					if (yesResult)
						_obj.IsCompoundActionItem = false;
				}
				else
					_obj.IsCompoundActionItem = false;
			}
			else
				_obj.IsCompoundActionItem = true;
		}

		public virtual bool CanChangeCompoundMode(Sungero.Domain.Client.CanExecuteActionArgs e)
		{
			return true;
		}

	}

}