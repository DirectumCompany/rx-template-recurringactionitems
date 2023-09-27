using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace DirRX.PeriodicActionItemsTemplate.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      var allUsers = Roles.AllUsers;
      if (allUsers != null)
      {
        GrantRightsOnDatabooks(allUsers);
      }
    }
    
    public void GrantRightsOnDatabooks(IRole allUsers)
    {
      ScheduleItems.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      ScheduleItems.AccessRights.Save();
      ScheduleItems.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      ScheduleItems.AccessRights.Save();
    }
  }

}
