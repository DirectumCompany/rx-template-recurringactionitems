using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace DirRX.Solution.Module.Parties.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      base.Initializing(e);
      
      // Выдача прав всем пользователям.
      var allUsers = Roles.AllUsers;
      if (allUsers != null)
      {
        GrantRightsOnFolders(allUsers);
      }
    }
    
    /// <summary>
    /// Выдать права на спец.папки модуля.
    /// </summary>
    /// <param name="allUsers">Группа "Все пользователи".</param>
    public static void GrantRightsOnFolders(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant right on contracts special folders to all users.");
      
      SpecialFolders.ArchiveCompletenessControl.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      SpecialFolders.ArchiveCompletenessControl.AccessRights.Save();
    }
  }
}
