using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace DirRX.Brands.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      CreateRoles();
      
      // Выдача прав для ролей.
      InitializationLogger.Debug("Init: Grant rights to role.");
      GrantRightsIntellectualPropertySpecialist();
      GrantRightsBrandsManagers();
      GrantRightsProductNameManagers();
      GrantRightsProductKindAndGroupManagers();
      GrantRightsCountriesManagers();
      GrantDatabooksRightsForAll();
    }
    
    /// <summary>
    /// Создать предопределенные роли.
    /// </summary>
    public static void CreateRoles()
    {
      InitializationLogger.Debug("Init: Create Default Roles");
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(Resources.IntellectualPropertySpecialistRoleName,
                                                                      Resources.IntellectualPropertySpecialistRoleDescription,
                                                                      Constants.Module.IntellectualPropertySpecialistRoleGuid);
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.Brands.Resources.BrandsManagersRoleName,
                                                                      DirRX.Brands.Resources.BrandsManagersRoleName,
                                                                      Constants.Module.BrandsManagersRoleGuid);
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.Brands.Resources.ProductNameManagersRoleName,
                                                                      DirRX.Brands.Resources.ProductNameManagersRoleName,
                                                                      Constants.Module.ProductNameManagersRoleGuid);
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.Brands.Resources.ProductKindAndGroupManagersRoleName,
                                                                      DirRX.Brands.Resources.ProductKindAndGroupManagersRoleName,
                                                                      Constants.Module.ProductKindAndGroupManagersRoleGuid);
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.Brands.Resources.CountriesManagersRoleName,
                                                                      DirRX.Brands.Resources.CountriesManagersRoleName,
                                                                      Constants.Module.CountriesManagersRoleGuid);
    }
    
    /// <summary>
    /// Выдать права для роли "Специалист по интеллектуальной собственности".
    /// </summary>
    public static void GrantRightsIntellectualPropertySpecialist()
    {
      var role = Roles.GetAll(r => r.Sid == Constants.Module.IntellectualPropertySpecialistRoleGuid).FirstOrDefault();
      if (role == null)
        return;
      
      BrandsRegistrations.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      ProductGroups.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      ProductKinds.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      ProductNames.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      WordMarks.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      Solution.Countries.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      
      BrandsRegistrations.AccessRights.Save();
      ProductGroups.AccessRights.Save();
      ProductKinds.AccessRights.Save();
      ProductNames.AccessRights.Save();
      WordMarks.AccessRights.Save();
      Solution.Countries.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права для роли "Пользователи с доступом к реестру товарных знаков".
    /// </summary>
    public static void GrantRightsBrandsManagers()
    {
      var role = Roles.GetAll(r => r.Sid == Constants.Module.BrandsManagersRoleGuid).FirstOrDefault();
      if (role == null)
        return;
      
      BrandsRegistrations.AccessRights.Grant(role, DefaultAccessRightsTypes.Read);
      ProductGroups.AccessRights.Grant(role, DefaultAccessRightsTypes.Read);
      ProductKinds.AccessRights.Grant(role, DefaultAccessRightsTypes.Read);
      Solution.Countries.AccessRights.Grant(role, DefaultAccessRightsTypes.Read);
      
      ProductGroups.AccessRights.Save();
      ProductKinds.AccessRights.Save();
      Solution.Countries.AccessRights.Save();
      BrandsRegistrations.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права для роли "Ответственный за добавление наименований продуктов".
    /// </summary>
    public static void GrantRightsProductNameManagers()
    {
      var role = Roles.GetAll(r => r.Sid == Constants.Module.ProductNameManagersRoleGuid).FirstOrDefault();
      if (role == null)
        return;
      
      WordMarks.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      ProductNames.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      ProductGroups.AccessRights.Grant(role, DefaultAccessRightsTypes.Read);
      
      ProductGroups.AccessRights.Save();
      ProductNames.AccessRights.Save();
      WordMarks.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права для роли "Ответственные за настройку видов товаров и товарных групп".
    /// </summary>
    public static void GrantRightsProductKindAndGroupManagers()
    {
      var role = Roles.GetAll(r => r.Sid == Constants.Module.ProductKindAndGroupManagersRoleGuid).FirstOrDefault();
      if (role == null)
        return;
      
      ProductGroups.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      ProductKinds.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      
      ProductGroups.AccessRights.Save();
      ProductKinds.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права для роли "Ответственные за настройку стран".
    /// </summary>
    public static void GrantRightsCountriesManagers()
    {
      var role = Roles.GetAll(r => r.Sid == Constants.Module.CountriesManagersRoleGuid).FirstOrDefault();
      if (role == null)
        return;
      
      Solution.Countries.AccessRights.Grant(role, DefaultAccessRightsTypes.Create, DefaultAccessRightsTypes.Change);
      Solution.Countries.AccessRights.Save();
      ProductGroups.AccessRights.Grant(role, DefaultAccessRightsTypes.Read);
      ProductGroups.AccessRights.Save();
    }
    
    public static void GrantDatabooksRightsForAll()
    {
      var allUsers = Roles.AllUsers;
      ProductKinds.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      ProductNames.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      WordMarks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      
      ProductKinds.AccessRights.Save();
      ProductNames.AccessRights.Save();
      WordMarks.AccessRights.Save();
    }
  }
}
