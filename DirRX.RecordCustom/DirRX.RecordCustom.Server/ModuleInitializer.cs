using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace DirRX.RecordCustom.Server
{
  public partial class ModuleInitializer
  {
    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      // Выдача прав всем пользователям.
      var allUsers = Roles.AllUsers;
      if (allUsers != null)
      {
        // Отчеты.
        InitializationLogger.Debug("Init: Grant right on reports to all users.");
        Reports.AccessRights.Grant(Reports.GetIncomingDocumentsReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
        
        InitializationLogger.Debug("Init: Create table for report \"Documents tax group\"");
        Reports.AccessRights.Grant(Reports.GetDocumentationsForTax().Info,  allUsers, DefaultReportAccessRightsTypes.Execute);
      }
      
      CreateReportsTables();
      CreateTableReportDocTaxGroup();
      CreateApprovalRole(RecordCustom.RecordCustomRole.Type.InitCEOManager, DirRX.RecordCustom.Resources.InitCEOManagerRoleName);
      CreateApprovalRole(RecordCustom.RecordCustomRole.Type.MemoAddressee, DirRX.RecordCustom.Resources.MemoAddresseeRoleName);
      CreateApprovalRole(RecordCustom.RecordCustomRole.Type.MemoAssignee, DirRX.RecordCustom.Resources.MemoAssigneeRoleName);
      CreateApprovalRole(RecordCustom.RecordCustomRole.Type.AssigneeManager, DirRX.RecordCustom.Resources.AssigneeManagerRoleName);      
      CreateApprovalRole(RecordCustom.RecordCustomRole.Type.ApproverPrvSt, DirRX.RecordCustom.Resources.ApproberPrvStRoleName);
    }
    
    #region Отчеты
    
    /// <summary>
    /// Создать таблицы для отчетов.
    /// </summary>
    public static void CreateReportsTables()
    {
      var incomingDocumentsReportTableName = Constants.IncomingDocumentsReport.IncomingDocumentsReportTableName;
      Sungero.Docflow.PublicFunctions.Module.DropReportTempTables(new[] { incomingDocumentsReportTableName });

      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.IncomingDocumentsReport.CreateIncomingDocumentsSourceTable, new[] { incomingDocumentsReportTableName });
    }
    
    private static void CreateTableReportDocTaxGroup()
    {
      var tableName = Constants.DocumentationsForTax.OrderOnIncomingLettersTableName;
      
      Sungero.Docflow.PublicFunctions.Module.DropReportTempTable(tableName);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.DocumentationsForTax.CreateOrderTable, new [] {tableName});
    }
    #endregion
    
    /// <summary>
    /// Создание роли.
    /// </summary>
    public static void CreateApprovalRole(Enumeration roleType, string description)
    {
      var role = RecordCustomRoles.GetAll().Where(r => Equals(r.Type, roleType)).FirstOrDefault();
      // Проверяет наличие роли.
      if (role == null)
      {
        role = RecordCustomRoles.Create();
        role.Type = roleType;
      }
      role.Description = description;
      role.Save();
    }
  }
}
