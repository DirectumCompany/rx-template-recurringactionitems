using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace DirRX.LocalActs.Server
{
  public partial class ModuleInitializer
  {
    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      // Создание типов документов.
      CreateDocumentTypes();
      
      // Выдача прав всем пользователям.
      var allUsers = Roles.AllUsers;
      if (allUsers != null)
      {
        InitializationLogger.Debug("Init: Grant rights for all users.");
        GrantRightsOnDocumentsAndDatabooks(allUsers);
        GrantRightsOnRequestInitiatorTask(allUsers);
        GrantRightsOnReports(allUsers);
      }
      
      // Создание ролей.
      InitializationLogger.Debug("Init: Create roles.");
      CreateRoles();
      CreateApprovalRole(DirRX.LocalActs.LocalActsRole.Type.Subscribers, DirRX.LocalActs.Resources.SubscribersRoleDescription);
      CreateApprovalRole(DirRX.LocalActs.LocalActsRole.Type.Supervisor, DirRX.LocalActs.Resources.SupervisorRoleDescription);
      CreateApprovalRole(DirRX.LocalActs.LocalActsRole.Type.RegDocManagers, DirRX.LocalActs.Resources.RegDocManagers);
      CreateApprovalRole(DirRX.LocalActs.LocalActsRole.Type.CRegDocManagers, DirRX.LocalActs.Resources.CRegDocManagers);
      CreateApprovalRole(DirRX.LocalActs.LocalActsRole.Type.SprvisorManager, DirRX.LocalActs.Resources.SupervisorManager);
      CreateApprovalRole(DirRX.LocalActs.LocalActsRole.Type.RiskManagers, DirRX.LocalActs.Resources.RiskManagers);
      
      // Выдача прав для роли Ответственные за настройку модуля «ЛНА».
      InitializationLogger.Debug("Init: Grant rights to role.");
      GrantRightsForLocalActsRole();
      
      CreateReportsTables();
      RevokeRightsOnCountry();
    }
    
    /// <summary>
    /// Создать предопределенные роли.
    /// </summary>
    public static void CreateRoles()
    {
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.LocalActs.Resources.LocalActsRoleName,
                                                                      DirRX.LocalActs.Resources.LocalActsRoleName,
                                                                      Constants.Module.RoleGuid.LocalActsRoleGuid);
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.LocalActs.Resources.NameTaxMonitoringRole,
                                                                      DirRX.LocalActs.Resources.NameTaxMonitoringRole,
                                                                      Constants.Module.RoleGuid.TaxMonitoringGuid);
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.LocalActs.Resources.RegulatoryDocumentsUpdaterRoleName,
                                                                      DirRX.LocalActs.Resources.RegulatoryDocumentsUpdaterRoleName,
                                                                      Constants.Module.RoleGuid.RegulatoryDocumentsUpdaterRoleGuid);
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.LocalActs.Resources.AddApproversRoleName,
                                                                      DirRX.LocalActs.Resources.AddApproversRoleName,
                                                                      Constants.Module.RoleGuid.AddApproversRoleGuid);
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.LocalActs.Resources.ExcludeFromAcquaintanceTaskRoleName,
                                                                      DirRX.LocalActs.Resources.ExcludeFromAcquaintanceTaskRoleName,
                                                                      Constants.Module.RoleGuid.ExcludeFromAcquaintanceTaskRole);
    }
    
    /// <summary>
    /// Создание роли согласования.
    /// </summary>
    /// <param name="roleType">Тип роли.</param>
    /// <param name="description">Описание.</param>
    public static void CreateApprovalRole(Enumeration roleType, string description)
    {
      var role = LocalActsRoles.GetAll().Where(r => Equals(r.Type, roleType)).FirstOrDefault();
      if (role == null)
      {
        role = LocalActsRoles.Create();
        role.Type = roleType;
      }
      role.Description = description;
      role.Save();
    }
    
    /// <summary>
    /// Выдать права на справочники для роли Ответственные за настройку модуля «ЛНА».
    /// </summary>
    public static void GrantRightsForLocalActsRole()
    {
      var role = GetLocalActsRole();
      if (role == null)
        return;
      
      BusinessProcessGroups.AccessRights.Grant(role, DefaultAccessRightsTypes.Read, DefaultAccessRightsTypes.Create, DefaultAccessRightsTypes.Change);
      OrderSubjects.AccessRights.Grant(role, DefaultAccessRightsTypes.Read, DefaultAccessRightsTypes.Create, DefaultAccessRightsTypes.Change);
      StandardForms.AccessRights.Grant(role, DefaultAccessRightsTypes.Read, DefaultAccessRightsTypes.Create, DefaultAccessRightsTypes.Change);
      
      BusinessProcessGroups.AccessRights.Save();
      OrderSubjects.AccessRights.Save();
      StandardForms.AccessRights.Save();
    }
    
    /// <summary>
    /// Получить роль "Ответственные за настройку модуля "ЛНА"".
    /// </summary>
    /// <returns>Роль "Ответственные за настройку модуля "ЛНА"".</returns>
    [Public]
    public static IRole GetLocalActsRole()
    {
      return Roles.GetAll(r => r.Sid == Constants.Module.RoleGuid.LocalActsRoleGuid).FirstOrDefault();
    }
    
    /// <summary>
    /// Создать типы документов.
    /// </summary>
    public static void CreateDocumentTypes()
    {
      InitializationLogger.Debug("Init: Create document types");
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateDocumentType(Resources.RegulatoryDocumentTypeName, RegulatoryDocument.ClassTypeGuid, Sungero.Docflow.DocumentType.DocumentFlow.Inner, false);
    }
    
    /// <summary>
    /// Назначить права на документы и справочники.
    /// </summary>
    public static void GrantRightsOnDocumentsAndDatabooks(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on documents and databooks");
      
      RegulatoryDocuments.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      RegulatoryDocuments.AccessRights.Save();
      
      BusinessProcessGroups.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      OrderSubjects.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      StandardForms.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      RiskLevels.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      RiskStatuses.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      Risks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      BusinessProcessGroups.AccessRights.Save();
      OrderSubjects.AccessRights.Save();
      StandardForms.AccessRights.Save();
      RiskLevels.AccessRights.Save();
      RiskStatuses.AccessRights.Save();
      Risks.AccessRights.Save();
    }
    
    public static void GrantRightsOnRequestInitiatorTask(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on task");
      
      RequestInitiatorTasks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      RequestInitiatorTasks.AccessRights.Save();
    }
    
    /// <summary>
    /// Для роли Ответственные за контрагентов оставить права только на чтение.
    /// </summary>
    public static void RevokeRightsOnCountry()
    {
      var role = Roles.GetAll(r => r.Sid == Constants.Module.RoleGuid.CounterpartiesResponsibleRole).FirstOrDefault();
      if (role == null)
        return;
      
      if (Solution.Countries.AccessRights.CanUpdate(role))
      {
        Solution.Countries.AccessRights.RevokeAll(role);
        Solution.Countries.AccessRights.Grant(role, DefaultAccessRightsTypes.Read);
        Solution.Countries.AccessRights.Save();
      }
    }
    
    /// <summary>
    /// Выдать права на отчеты.
    /// </summary>
    /// <param name="allUsers">Роль, для которой нужно выдать права.</param>
    public static void GrantRightsOnReports(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on reports");
      
      Reports.AccessRights.Grant(Reports.GetDocumentApprovalStatisticsReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
      Reports.AccessRights.Grant(Reports.GetApprovalSheetReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
      Reports.AccessRights.Grant(Reports.GetApproversWorkStatisticsReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
    }
    
    /// <summary>
    /// Создать таблицы для отчетов.
    /// </summary>
    public static void CreateReportsTables()
    {
      var sourceTableName = Constants.DocumentApprovalStatisticsReport.SourceTableName;
      Sungero.Docflow.PublicFunctions.Module.DropReportTempTable(sourceTableName);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.DocumentApprovalStatisticsReport.CreateReportSourceTable, new[] { sourceTableName });
			
      var customApprovalSheetReportTableName = Constants.Module.ApprovalSheetReport.SourceTableName;
      Sungero.Docflow.PublicFunctions.Module.DropReportTempTable(customApprovalSheetReportTableName);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.ApprovalSheetReport.CreateReportTable, new[] { customApprovalSheetReportTableName });

      var approversWorkStatisticsReportTableName = Constants.ApproversWorkStatisticsReport.SourceTableName;
      Sungero.Docflow.PublicFunctions.Module.DropReportTempTable(approversWorkStatisticsReportTableName);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.ApproversWorkStatisticsReport.CreateReportSourceTable, new[] { approversWorkStatisticsReportTableName });
    }
  }
}