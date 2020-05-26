using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.NoticeSetting;

namespace DirRX.ActionItems.Client
{
	partial class NoticeSettingFunctions
	{

		/// <summary>
		/// Установить видимость признаков доступных ответственным за настройку поручений.
		/// </summary>
		/// <param name="isSettingsResponsible">Признак исполнителя роли ответственного за поручения.</param>
		public void SetVisibilitySettingRequiredFlags(bool isSettingsResponsible)
		{
			_obj.State.Properties.AllUsersFlag.IsVisible = isSettingsResponsible;

			_obj.State.Properties.IsAAbortsRequired.IsVisible     = isSettingsResponsible;
			_obj.State.Properties.IsAAcceptRequired.IsVisible     = isSettingsResponsible;
			_obj.State.Properties.IsAAddTimeRequired.IsVisible    = isSettingsResponsible;
			_obj.State.Properties.IsADeadlineRequired.IsVisible   = isSettingsResponsible;
			_obj.State.Properties.IsADeclinedRequired.IsVisible   = isSettingsResponsible;
			_obj.State.Properties.IsAEightyRequired.IsVisible     = isSettingsResponsible;
			_obj.State.Properties.IsAEscalatedRequired.IsVisible  = isSettingsResponsible;
			_obj.State.Properties.IsAExpiredRequired.IsVisible    = isSettingsResponsible;
			_obj.State.Properties.IsAFortyRequired.IsVisible      = isSettingsResponsible;
			_obj.State.Properties.IsANewSubjRequired.IsVisible    = isSettingsResponsible;
			_obj.State.Properties.IsAOnControlRequired.IsVisible  = isSettingsResponsible;
			_obj.State.Properties.IsAPerformRequired.IsVisible    = isSettingsResponsible;
			_obj.State.Properties.IsARevisionRequired.IsVisible   = isSettingsResponsible;
			_obj.State.Properties.IsAReworkRequired.IsVisible     = isSettingsResponsible;
			_obj.State.Properties.IsASixtyRequired.IsVisible      = isSettingsResponsible;
			_obj.State.Properties.IsAStartsRequired.IsVisible     = isSettingsResponsible;
			_obj.State.Properties.IsATimeAcceptRequired.IsVisible = isSettingsResponsible;
			_obj.State.Properties.IsATwentyRequired.IsVisible     = isSettingsResponsible;
		}
	}
}