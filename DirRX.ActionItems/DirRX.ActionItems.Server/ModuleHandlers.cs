using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ActionItems.Server
{
  partial class NoticeSettingsFolderHandlers
  {

    public virtual IQueryable<DirRX.ActionItems.INoticeSetting> NoticeSettingsDataQuery(IQueryable<DirRX.ActionItems.INoticeSetting> query)
    {            
      bool isSettingsResponsible = DirRX.ActionItems.PublicFunctions.Module.CanChangedSettings(Users.Current);
      
      if (isSettingsResponsible || Users.Current.IncludedIn(Roles.Administrators))
        return query;
      else
      {
        var currentEmployee = DirRX.Solution.Employees.As(Users.Current);
        return query.Where(s => DirRX.Solution.Employees.Equals(s.Employee, currentEmployee));
      }
    }
  }

  partial class EscalateFolderHandlers
  {

    public virtual bool IsEscalateVisible()
    {
      return Sungero.Docflow.PublicFunctions.Module.IncludedInBusinessUnitHeadsRole() ||
               Sungero.Docflow.PublicFunctions.Module.IncludedInDepartmentManagersRole();
    }
  }

  partial class ActionItemsHandlers
  {
  }
}