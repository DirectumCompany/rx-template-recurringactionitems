using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.NoticeSetting;

namespace DirRX.ActionItems
{
  partial class NoticeSettingAssgnRolePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> AssgnRoleFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var possibleRoles = DirRX.ActionItems.PublicFunctions.ActionItemsRole.Remote.GetPossibleRolesForNotices();
      
      return query.Where(r => possibleRoles.Contains(r));
    }
  }

	partial class NoticeSettingServerHandlers
	{

		public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
		{
			if (_obj.AssgnRole != null && Functions.NoticeSetting.IsSameRoleSettingExists(_obj, _obj.AssgnRole) && !_obj.AllUsersFlag.GetValueOrDefault())
			  e.AddError(NoticeSettings.Resources.SameSettingExists, _obj.Info.Actions.GetSameSetting);
			
			string employeeName = _obj.AllUsersFlag.GetValueOrDefault() ? NoticeSettings.Resources.NoticeSettingAllUsersName : _obj.Employee.DisplayValue;
			_obj.Name = NoticeSettings.Resources.NoticeSettingNameTemplateFormat(employeeName, _obj.AssgnRole.DisplayValue);
		}

		public override void Created(Sungero.Domain.CreatedEventArgs e)
		{
			_obj.Information = NoticeSettings.Resources.LabelInformation;
			_obj.AllUsersFlag = false;	
			
			#region Блок признаков с событиями.
      _obj.IsAssignmentAborts     = false;
      _obj.IsAssignmentAccept   	= false;
      _obj.IsAssignmentAddTime    = false;
      _obj.IsAssignmentDeadline   = false;
      _obj.IsAssignmentDeclined   = false;
      _obj.IsAssignmentEighty     = false;
      _obj.IsAssignmentEscalated  = false;
      _obj.IsAssignmentExpired    = false;
      _obj.IsAssignmentForty      = false;
      _obj.IsAssignmentNewSubj    = false;
      _obj.IsAssignmentOnControl  = false;
      _obj.IsAssignmentPerform    = false;
      _obj.IsAssignmentRevision   = false;
      _obj.IsAssignmentRework     = false;
      _obj.IsAssignmentSixty      = false;
      _obj.IsAssignmentStarts     = false;
      _obj.IsAssignmentTimeAccept = false;
      _obj.IsAssignmentTwenty     = false;
      #endregion
      
      #region Блок кнопок "Неотключаемый".
      _obj.IsAAbortsRequired     = false;
      _obj.IsAAcceptRequired     = false;
      _obj.IsAAddTimeRequired    = false;
      _obj.IsADeadlineRequired   = false;
      _obj.IsADeclinedRequired   = false;
      _obj.IsAEightyRequired     = false;
      _obj.IsAEscalatedRequired  = false;
      _obj.IsAExpiredRequired    = false;
      _obj.IsAFortyRequired      = false;
      _obj.IsANewSubjRequired    = false;
      _obj.IsAOnControlRequired  = false;
      _obj.IsAPerformRequired    = false;
      _obj.IsARevisionRequired   = false;
      _obj.IsAReworkRequired     = false;
      _obj.IsASixtyRequired      = false;
      _obj.IsAStartsRequired     = false;
      _obj.IsATimeAcceptRequired = false;
      _obj.IsATwentyRequired     = false;
      #endregion
		}
	}

}