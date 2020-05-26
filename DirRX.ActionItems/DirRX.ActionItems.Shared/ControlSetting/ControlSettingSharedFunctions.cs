using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.ControlSetting;

namespace DirRX.ActionItems.Shared
{
	partial class ControlSettingFunctions
	{
		/// <summary>
		/// Проверить дубли настроек контроля.
		/// </summary>
		/// <returns>True, если дубликаты имеются, иначе - false.</returns>
		public bool HaveDuplicates()
		{
			if (_obj.Status == Sungero.Commons.Currency.Status.Closed)
				return false;
			
			return Functions.ControlSetting.Remote.GetDuplicates(_obj).Any();
		}
	}
}