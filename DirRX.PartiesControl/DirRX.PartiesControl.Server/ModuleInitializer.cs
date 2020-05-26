using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace DirRX.PartiesControl.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      CreateCounterpartyStatuses();
      CreateDocumentTypes();
      CreateDocumentKinds();
      CreateRoles();
      CreateCounterpartyCheckingTypes();
      CreateReportsTables();
      CreateConstants();
      
      GrantDatabooksRightsForModuleRole();
      GrantRightsForCounterpartiesResponsible();
      GrantRightsForContractsResponsible();
      GrantRightsForSecurityService();
      GrantRightsForArchiveResponsibleRole();
      
      // Выдача прав всем пользователям.
      var allUsers = Roles.AllUsers;
      if (allUsers != null)
      {
        InitializationLogger.Debug("Init: Grant rights for all users.");
        GrantDatabooksRightsForAll(allUsers);
        GrantRightsOnDocuments(allUsers);
      }
    }
    
    private void CreateCounterpartyStatuses()
    {
      InitializationLogger.Debug("Init: Create counterparty default statuses.");
      
      PartiesControl.PublicFunctions.CounterpartyStatus.CreateCounterpartyStatus(Constants.CounterpartyStatus.DefaultStatus.StopListSid,
                                                                                 CounterpartyStatuses.Resources.DefaultStatusStopList);
      PartiesControl.PublicFunctions.CounterpartyStatus.CreateCounterpartyStatus(Constants.CounterpartyStatus.DefaultStatus.CheckingRequiredSid,
                                                                                 CounterpartyStatuses.Resources.DefaultStatusCheckingRequired);
    }
    
    /// <summary>
    /// Создать типы документов.
    /// </summary>
    public static void CreateDocumentTypes()
    {
      InitializationLogger.Debug("Init: Create document types");
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateDocumentType(DirRX.PartiesControl.Resources.RevisionRequestTypeName, RevisionRequest.ClassTypeGuid, Sungero.Docflow.DocumentType.DocumentFlow.Inner, false);
    }
    
    /// <summary>
    /// Создать виды документов.
    /// </summary>
    public static void CreateDocumentKinds()
    {
      InitializationLogger.Debug("Init: Create document kinds.");
      var notNumerable = Sungero.Docflow.DocumentKind.NumberingType.NotNumerable;
      
      // Создать вид документа "Заявка на проверку контрагента".
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(DirRX.PartiesControl.Resources.RevisionRequestTypeName,
                                                                              DirRX.PartiesControl.Resources.RevisionRequestTypeName,
                                                                              notNumerable, Sungero.Docflow.DocumentType.DocumentFlow.Inner, true, false,
                                                                              RevisionRequest.ClassTypeGuid, null, PartiesControl.Constants.Module.RevisionRequestKind);
      
      // Создать вид документа "Анкета контрагента".
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(DirRX.PartiesControl.Resources.CounterpartyInformationKind,
                                                                              DirRX.PartiesControl.Resources.CounterpartyInformationKind,
                                                                              notNumerable, Sungero.Docflow.DocumentType.DocumentFlow.Inner, true, false,
                                                                              PartiesControl.Constants.Module.CounterpartyDocumentTypeGuid, null, PartiesControl.Constants.Module.CounterpartyInformationKind);
    }
    
    private void CreateCounterpartyCheckingTypes()
    {
      InitializationLogger.Debug("Init: Create counterparty default checking types.");
      PartiesControl.PublicFunctions.CheckingType.CreateCheckingType(CheckingTypes.Resources.DefaultTypeFullChecking,
                                                                     DirRX.PartiesControl.CheckingType.DocProvision.Necessarily);
      PartiesControl.PublicFunctions.CheckingType.CreateCheckingType(CheckingTypes.Resources.DefaultTypeSimpleChecking,
                                                                     DirRX.PartiesControl.CheckingType.DocProvision.Necessarily);
    }
    
    private void CreateRoles()
    {
      Logger.Debug("Init: Create Roles");
      
      Functions.Module.CreateSingleRole(DirRX.PartiesControl.Resources.ArchiveResponsibleRoleName,
                                        DirRX.PartiesControl.Resources.ArchiveResponsibleRoleDescription,
                                        Constants.Module.ArchiveResponsibleRole);
      Functions.Module.CreateSingleRole(DirRX.PartiesControl.Resources.ClientServiceManagerRoleName,
                                        DirRX.PartiesControl.Resources.ClientServiceManagerRoleDescription,
                                        Constants.Module.ClientServiceManagerRole);
      Functions.Module.CreateSingleRole(DirRX.PartiesControl.Resources.ComplianceSpecialistRoleName,
                                        DirRX.PartiesControl.Resources.ComplianceSpecialistRoleDescription,
                                        Constants.Module.ComplianceSpecialistRole);
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.PartiesControl.Resources.SecurityServiceRoleName,
                                                                      DirRX.PartiesControl.Resources.SecurityServiceRoleDescription,
                                                                      Constants.Module.SecurityServiceRole);
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.PartiesControl.Resources.KsssResponsibleRoleName,
                                                                      DirRX.PartiesControl.Resources.KsssResponsibleRoleDescription,
                                                                      Constants.Module.KsssResponsibleRole);
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.PartiesControl.Resources.CounterpartiesModuleRoleName,
                                                                      DirRX.PartiesControl.Resources.CounterpartiesModuleRoleDescription,
                                                                      Constants.Module.CounterpartiesModuleRole);
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.PartiesControl.Resources.ServiceECDRoleName,
                                                                      DirRX.PartiesControl.Resources.ServiceECDRoleDescription,
                                                                      Constants.Module.ServiceECDRole);
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.PartiesControl.Resources.ServiceTreasuryDepartmentRoleName,
                                                                      DirRX.PartiesControl.Resources.ServiceTreasuryDepartmentRoleDescription,
                                                                      Constants.Module.ServiceTreasuryDepartmentRole);
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.PartiesControl.Resources.SpecialFieldsRoleName,
                                                                      DirRX.PartiesControl.Resources.SpecialFieldsRoleDescription,
                                                                      Constants.Module.SpecialFieldsRole);
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.PartiesControl.Resources.StopListNoticeRoleName,
                                                                      DirRX.PartiesControl.Resources.StopListNoticeRoleDescription,
                                                                      Constants.Module.StopListNoticeRole);
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.PartiesControl.Resources.CEOReportAssigneeRoleName,
                                                                      DirRX.PartiesControl.Resources.CEOReportAssigneeRoleDescription,
                                                                      Constants.Module.CEOReportAssigneeRole);
    }
    
    /// <summary>
    /// Выдать права всем пользователям на чтение.
    /// </summary>
    public static void GrantDatabooksRightsForAll(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on databooks");
      
      CheckingTypes.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      CheckingReasons.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      CheckingResults.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      CheckingDocumentLists.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      CounterpartyStatuses.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      ShippingAddresses.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      StoplistIncludeReasons.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      
      CheckingTypes.AccessRights.Save();
      CheckingReasons.AccessRights.Save();
      CheckingResults.AccessRights.Save();
      CheckingDocumentLists.AccessRights.Save();
      CounterpartyStatuses.AccessRights.Save();
      ShippingAddresses.AccessRights.Save();
      StoplistIncludeReasons.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права ответственным за настройку модуля на изменение.
    /// </summary>
    public static void GrantDatabooksRightsForModuleRole()
    {
      InitializationLogger.Debug("Init: Grant rights on databooks for module roles");
      
      var role = Roles.GetAll(r => r.Sid == Constants.Module.CounterpartiesModuleRole).FirstOrDefault();
      CheckingTypes.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      CheckingReasons.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      CheckingResults.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      CheckingDocumentLists.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      CounterpartyStatuses.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      ShippingAddresses.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      StoplistIncludeReasons.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
      
      CheckingTypes.AccessRights.Save();
      CheckingReasons.AccessRights.Save();
      CheckingResults.AccessRights.Save();
      CheckingDocumentLists.AccessRights.Save();
      CounterpartyStatuses.AccessRights.Save();
      ShippingAddresses.AccessRights.Save();
      StoplistIncludeReasons.AccessRights.Save();
    }
    
    public static void GrantRightsForCounterpartiesResponsible()
    {
      var role = Roles.GetAll(r => r.Sid == LocalActs.PublicConstants.Module.RoleGuid.CounterpartiesResponsibleRole).FirstOrDefault();
      if (role == null)
        return;
      
      ShippingAddresses.AccessRights.Grant(role, DefaultAccessRightsTypes.Create);
      ShippingAddresses.AccessRights.Save();
    }
    
    public static void GrantRightsForContractsResponsible()
    {
      var role = Roles.GetAll(r => r.Sid == Sungero.Docflow.Constants.Module.RoleGuid.ContractsResponsible).FirstOrDefault();
      if (role == null)
        return;
      
      ShippingAddresses.AccessRights.Grant(role, DefaultAccessRightsTypes.Create);
      ShippingAddresses.AccessRights.Save();
    }
    
    public static void GrantRightsForSecurityService()
    {
      var role = Roles.GetAll(r => r.Sid == Constants.Module.SecurityServiceRole).FirstOrDefault();
      if (role == null)
        return;
      
      Reports.AccessRights.Grant(Reports.GetSecurityReport().Info, role, DefaultReportAccessRightsTypes.Execute);
    }
    
    public static void GrantRightsForArchiveResponsibleRole()
    {
      var role = Roles.GetAll(r => r.Sid == Constants.Module.ArchiveResponsibleRole).FirstOrDefault();
      if (role == null)
        return;
      
      Reports.AccessRights.Grant(Reports.GetDocumentControlReport().Info, role, DefaultReportAccessRightsTypes.Execute);
    }
    
    /// <summary>
    /// Назначить права на документы.
    /// </summary>
    public void GrantRightsOnDocuments(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on documents and databooks");
      
      RevisionRequests.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      RevisionRequests.AccessRights.Save();
    }
    
    /// <summary>
    /// Создать таблицы для отчетов.
    /// </summary>
    public static void CreateReportsTables()
    {
      var internalInventoryReportTableName = Constants.InternalInventoryReport.DataTable;
      Sungero.Docflow.PublicFunctions.Module.DropReportTempTables(new[] { internalInventoryReportTableName });
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.InternalInventoryReport.CreateDataTable, new[] { internalInventoryReportTableName });
      
      var securityReportTableName = Constants.SecurityReport.SecurityReportTableName;
      Sungero.Docflow.PublicFunctions.Module.DropReportTempTable(securityReportTableName);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.SecurityReport.CreateDataTable, new[] { securityReportTableName });
      
      var documentControlReportTableName = Constants.DocumentControlReport.DocumentControlReportTableName;
      Sungero.Docflow.PublicFunctions.Module.DropReportTempTable(documentControlReportTableName);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.DocumentControlReport.CreateDataTable, new[] { documentControlReportTableName });
    }
    
    /// <summary>
    /// Создать перечень констант.
    /// </summary>
    public static void CreateConstants()
    {
      DirRX.ContractsCustom.PublicInitializationFunctions.Module.CreateConstant(Constants.Module.InitiatorMonthCountGuid.ToString(),
                                                                                Constants.Module.InitiatorMonthCountName,
                                                                                ContractsCustom.ContractConstant.TypeConst.Period,
                                                                                ContractsCustom.ContractConstant.Unit.Month);
      
      DirRX.ContractsCustom.PublicInitializationFunctions.Module.CreateConstant(Constants.Module.SupervisorMonthCountGuid.ToString(),
                                                                                Constants.Module.SupervisorMonthCountName,
                                                                                ContractsCustom.ContractConstant.TypeConst.Period,
                                                                                ContractsCustom.ContractConstant.Unit.Month);
    }
  }
}

