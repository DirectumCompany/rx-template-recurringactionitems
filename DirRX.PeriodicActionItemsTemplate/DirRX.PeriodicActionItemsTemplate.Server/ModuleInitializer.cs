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
      
      CreateDefaultActionItemsRoles();
      CreateRoles();
      GrantRightsToAssignmentServiceDatabooks();
      CreateDefaultAssignmentSettings();
      GrantRightsToNoticeSettingsDatabook();
      GrantRightOnFolder();
      GrantRightsOnTasks(allUsers);
      GrantRightsToReports(allUsers);
      GrantRightsOnAssistantCEOReport();
      CreateReportsTables();
    }
    
    #region Создание ролей и выдачи им прав
    
    /// <summary>
    /// Создать роли исполнения поручений.
    /// </summary>
    public static void CreateDefaultActionItemsRoles()
    {
      InitializationLogger.Debug("Init: Create default action items roles.");
      
      CreateActionItemsRole(ActionItems.ActionItemsRole.Type.CEO, ActionItems.Resources.RoleDescriptionCEO);
      CreateActionItemsRole(ActionItems.ActionItemsRole.Type.CEOAssistant, ActionItems.Resources.RoleDescriptionCEOAssistant);
      CreateActionItemsRole(ActionItems.ActionItemsRole.Type.InitCEOManager, ActionItems.Resources.RoleDescriptionInitCEOManager);
      CreateActionItemsRole(ActionItems.ActionItemsRole.Type.InitManager, ActionItems.Resources.RoleDescriptionInitManager);
      CreateActionItemsRole(ActionItems.ActionItemsRole.Type.Secretary, ActionItems.Resources.RoleDescriptionSecretary);
      CreateActionItemsRole(ActionItems.ActionItemsRole.Type.Commissioner, ActionItems.Resources.RoleDescriptionCommissioner);
      CreateActionItemsRole(ActionItems.ActionItemsRole.Type.Performer, ActionItems.Resources.RoleDescriptionPerformer);
      CreateActionItemsRole(ActionItems.ActionItemsRole.Type.Controler, ActionItems.Resources.RoleDescriptionControler);
      CreateActionItemsRole(ActionItems.ActionItemsRole.Type.Subscriber, ActionItems.Resources.RoleDescriptionSubscriber);
      CreateActionItemsRole(ActionItems.ActionItemsRole.Type.EscalateManager, ActionItems.Resources.RoleDescriptionEscManager);
    }
    
    /// <summary>
    /// Создать роль исполнения поручений.
    /// </summary>
    /// <param name="roleType">Тип роли.</param>
    /// <param name="description">Описание роли.</param>
    public static void CreateActionItemsRole(Enumeration roleType, string description)
    {
      InitializationLogger.DebugFormat("Init: Create contract action items role {0}", ActionItemsRoles.Info.Properties.Type.GetLocalizedValue(roleType));
      
      var role = ActionItemsRoles.GetAll().Where(r => Equals(r.Type, roleType)).FirstOrDefault();
      if (role == null)
        role = ActionItemsRoles.Create();
      
      role.Type = roleType;
      role.Description = description;
      role.Save();
    }
    
    /// <summary>
    /// Создать предопределённые роли.
    /// </summary>
    public static void CreateRoles()
    {
      InitializationLogger.Debug("Init: Create Default Roles");

      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(Resources.NameRoleSecretary, Resources.DescriptionRoleSecretary, Constants.Module.RoleSecretary);
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(Resources.RoleNameAssignmentSettingResponsibles,
                                                                      Resources.DescriptionAssignmentSettingResponsiblesRole,
                                                                      Constants.Module.AssignmentSettingResponsiblesRole);
      
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.ActionItems.Resources.CEOAssistantRoleName,
                                                                      DirRX.ActionItems.Resources.CEOAssistantRoleDescription,
                                                                      Constants.Module.CEOAssistant);
    }
    
    /// <summary>
    /// Назначить права роли ответственных за настройку поручений.
    /// </summary>
    public static void GrantRightsToAssignmentServiceDatabooks()
    {
      InitializationLogger.Debug("Init: Grant rights on documents and databooks to responsibles for assignments settings");
      
      var role = Functions.Module.GetAssignmentSettingResponsiblesRole();
      if (role == null)
        return;
      
      // Выдать полные права роли ответственных за настройку поручений.
      ActionItems.Categories.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      ActionItems.ControlSettings.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      ActionItems.Priorities.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      ActionItems.Marks.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      ActionItems.NoticeSettings.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      // Выдать права всем пользователям.
      var allUsers = Roles.AllUsers;
      ActionItems.Categories.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      ActionItems.ControlSettings.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      ActionItems.Priorities.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      ActionItems.Marks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      ActionItems.ActionItemsRoles.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      ActionItems.NoticeSettings.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Change);
      
      ActionItems.Categories.AccessRights.Save();
      ActionItems.ControlSettings.AccessRights.Save();
      ActionItems.Priorities.AccessRights.Save();
      ActionItems.Marks.AccessRights.Save();
      ActionItems.ActionItemsRoles.AccessRights.Save();
      ActionItems.NoticeSettings.AccessRights.Save();
    }
    #endregion
    
    public static void GrantRightOnFolder()
    {
      var allUsers = Roles.AllUsers;
      if (allUsers != null)
      {
        Logger.Debug("Init: Grant right on special folder Escalate to all users.");
        DirRX.ActionItems.SpecialFolders.Escalate.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
        DirRX.ActionItems.SpecialFolders.Escalate.AccessRights.Save();
        
        DirRX.ActionItems.SpecialFolders.NoticeSettings.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
        DirRX.ActionItems.SpecialFolders.NoticeSettings.AccessRights.Save();
      }
    }
    
    public static void GrantRightsOnTasks(IRole allUsers)
    {
      ActionItemRejectionTasks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      ActionItemRejectionTasks.AccessRights.Save();
    }

    #region Создание настроек уведомлений

    /// <summary>
    /// Создать общие настройки уведомлений по поручениям.
    /// </summary>
    public static void CreateDefaultAssignmentSettings()
    {
      InitializationLogger.Debug("Init: Create default assignment notice settings.");
      
      var settings = NoticeSettings.GetAll(s => s.AllUsersFlag.HasValue && s.AllUsersFlag.Value == true);
      
      foreach (IActionItemsRole role in PublicFunctions.ActionItemsRole.Remote.GetPossibleRolesForNotices())
      {
        if (!settings.Any(s => ActionItemsRoles.Equals(s.AssgnRole, role)))
          CreateAssignmentSetting(role);
      }
    }

    /// <summary>
    /// Создать настройку уведомлений по поручениям.
    /// </summary>
    /// <param name="role">Роль.</param>
    public static void CreateAssignmentSetting(IActionItemsRole role)
    {
      InitializationLogger.DebugFormat("Init: Create default assignment notice setting for role {0}", role.DisplayValue);
      
      var setting = NoticeSettings.Create();
      setting.State.Properties.Employee.IsRequired = false;
      setting.AssgnRole = role;
      setting.AllUsersFlag = true;
      setting.Employee = null;
      
      setting.Save();
    }

    /// <summary>
    /// Назначить права всем пользователям на справочник настроек уведомлений по поручениям.
    /// </summary>
    public static void GrantRightsToNoticeSettingsDatabook()
    {
      InitializationLogger.Debug("Init: Grant rights on databook for assignment notice settings.");
      
      ActionItems.NoticeSettings.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Change);
      ActionItems.NoticeSettings.AccessRights.Save();
    }

    #endregion
    
    /// <summary>
    /// Выдать права на отчеты.
    /// </summary>
    public static void GrantRightsToReports(IRole allUsers)
    {
      Reports.AccessRights.Grant(Reports.GetPrintActionItemTask().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
      Reports.AccessRights.Grant(Reports.GetCustomActionItemsExecutionReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
    }     
       
    /// <summary>
    /// Выдать права на отчёт для помощника ГД.
    /// </summary>
    public static void GrantRightsOnAssistantCEOReport()
    {
      var role = Roles.GetAll(r => r.Sid == Constants.Module.CEOAssistant).FirstOrDefault();
      if (role == null)
        return;
      
      Reports.AccessRights.Grant(Reports.GetAssistantCEOReport().Info, role, DefaultReportAccessRightsTypes.Execute);
    }
    
    public static void CreateReportsTables()
    {
      var actionItemTaskReportName = Constants.PrintActionItemTask.PrintActionItemTaskTableName;
      Sungero.Docflow.PublicFunctions.Module.DropReportTempTable(actionItemTaskReportName);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.PrintActionItemTask.CreateActionItemTaskTable, new[] { actionItemTaskReportName });      
      
      var AssistantCEOReportName = Constants.AssistantCEOReport.SourceTableName;
      Sungero.Docflow.PublicFunctions.Module.DropReportTempTable(AssistantCEOReportName);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.AssistantCEOReport.CreateAssistantCEOReportSourceTable, new[] { AssistantCEOReportName });
      
      var customActionItemsExecutionReportTableName = Constants.CustomActionItemsExecutionReport.SourceTableName;   
      Sungero.Docflow.PublicFunctions.Module.DropReportTempTable(customActionItemsExecutionReportTableName);     
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.CustomActionItemsExecutionReport.CreateActionItemExecutionReportSourceTable, new[] { customActionItemsExecutionReportTableName });
    }
  }
}
