using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace DirRX.ProcessSubstitutionModule.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      // Выдача прав всем пользователям.
      var allUsers = Roles.AllUsers;
      if (allUsers != null)
      {
        InitializationLogger.Debug("Init: Grant rights for all users.");
        GrantRightsOnDocumentsAndDatabooks(allUsers);
        GrantRightsOnFolders(allUsers);
      }
    }
    
    /// <summary>
    /// Назначить права на документы и справочники.
    /// </summary>
    public static void GrantRightsOnDocumentsAndDatabooks(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on databooks");
      
      ProcessSubstitutions.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Change);
      ProcessSubstitutions.AccessRights.Save();

      SubstituteConnections.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Change);
      SubstituteConnections.AccessRights.Save();
    }
    
    /// <summary>
    /// Назначить права на папки потока.
    /// </summary>
    public static void GrantRightsOnFolders(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on folders");
      
      SpecialFolders.ProcessSubstitutionFolder.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      SpecialFolders.ProcessSubstitutionFolder.AccessRights.Save();
    }
  }
}
