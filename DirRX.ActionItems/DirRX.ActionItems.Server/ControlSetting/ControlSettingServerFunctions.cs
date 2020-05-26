using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.ControlSetting;

namespace DirRX.ActionItems.Server
{
	partial class ControlSettingFunctions
	{
		/// <summary>
		/// Получить дубли валюты.
		/// </summary>
		/// <returns>Валюты, дублирующие текущую.</returns>
		[Remote(IsPure = true)]
		public IQueryable<IControlSetting> GetDuplicates()
		{
			return ControlSettings.GetAll()
				.Where(s => s.Status != Sungero.Commons.Currency.Status.Closed)
				.Where(s => DirRX.ActionItems.Categories.Equals(s.Category, _obj.Category))
				.Where(s => DirRX.ActionItems.ActionItemsRoles.Equals(s.Initiator, _obj.Initiator))
				.Where(s => !DirRX.ActionItems.ControlSettings.Equals(s, _obj));
		}
	}
}