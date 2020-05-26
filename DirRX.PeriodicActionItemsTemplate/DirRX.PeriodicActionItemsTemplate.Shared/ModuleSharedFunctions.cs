using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PeriodicActionItemsTemplate.Shared
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Определить возможность пользователя менять настройки по поручениям.
    /// </summary>
    /// <param name="recipient">Сотрудник.</param>
    /// <returns>True если входит в соответствующие роли.</returns>
    [Public]
    public static bool CanChangedSettings(Sungero.CoreEntities.IRecipient recipient)
    {
      var currentUser = Users.Current;
      var settingResponsiblesRole = PublicFunctions.Module.Remote.GetAssignmentSettingResponsiblesRole();
      return currentUser.IncludedIn(settingResponsiblesRole);
    }
  }
}