using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.NoticeSetting;

namespace DirRX.ActionItems
{
  partial class NoticeSettingClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      bool isAssignmentResponsible = DirRX.ActionItems.PublicFunctions.Module.CanChangedSettings(Users.Current) || Users.Current.IncludedIn(Roles.Administrators);
      
      _obj.State.Properties.Employee.IsRequired = !_obj.AllUsersFlag.GetValueOrDefault();
      _obj.State.Properties.Employee.IsEnabled = _obj.State.IsInserted && isAssignmentResponsible && _obj.State.Properties.Employee.IsEnabled;
      _obj.State.Properties.AssgnRole.IsEnabled = _obj.State.IsInserted && isAssignmentResponsible && _obj.State.Properties.AssgnRole.IsEnabled;
      
      Functions.NoticeSetting.SetAvailabilityNoticeSettingsEvents(_obj, isAssignmentResponsible);
      
      if (_obj.AssgnRole != null && Functions.NoticeSetting.Remote.IsSameRoleSettingExists(_obj, _obj.AssgnRole) && !_obj.AllUsersFlag.GetValueOrDefault())
        e.AddWarning(NoticeSettings.Resources.SameSettingExists, _obj.Info.Actions.GetSameSetting);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      if (_obj.State.IsInserted && _obj.AssgnRole != null)
        _obj.Employee = DirRX.Solution.Employees.As(Users.Current);
      
      bool isAssignmentResponsible = DirRX.ActionItems.PublicFunctions.Module.CanChangedSettings(Users.Current) || Users.Current.IncludedIn(Roles.Administrators);
      
      _obj.State.Properties.Employee.IsEnabled = _obj.Employee == null;
      _obj.State.Properties.AssgnRole.IsEnabled = _obj.AssgnRole == null;
      
      bool isSettingOwner = false;
      if (!isAssignmentResponsible)
      {
        // Вычислить владельца настройки.
        var employee = DirRX.Solution.Employees.As(Users.Current);
        isSettingOwner = DirRX.Solution.Employees.Equals(employee, _obj.Employee);
      }
      
      // Карточка доступна на редактирование только администратору, ответственному и владельцу настройки.
      _obj.State.IsEnabled = isAssignmentResponsible || isSettingOwner;
      
      Functions.NoticeSetting.SetVisibilitySettingRequiredFlags(_obj, isAssignmentResponsible);
      Functions.NoticeSetting.SetAvailabilityNoticeSettingsEvents(_obj, isAssignmentResponsible);
    }

  }
}