using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProcessSubstitutionModule.ProcessSubstitution;

namespace DirRX.ProcessSubstitutionModule.Shared
{
  partial class ProcessSubstitutionFunctions
  {
    /// <summary>
    /// Установить доступность свойств.
    /// </summary>
    public void SetPropertiesAvailabilityAndVisibility()
    {
      if (_obj.Employee != null)
      {
        bool isAdminOrResponsible = (Users.Current.IncludedIn(Roles.Administrators) ||
                                     Users.Equals(Users.Current, _obj.Employee)||
                                     Users.Equals(Users.Current, _obj.Employee.Manager));
        
        // Проверить является ли текущий пользователь руководителем.
        if (!isAdminOrResponsible && _obj.Employee.Department != null)
        {
          isAdminOrResponsible = Users.Equals(Users.Current, _obj.Employee.Department.Manager);
        }
        
        _obj.State.IsEnabled = isAdminOrResponsible;
      }
    }
  }
}