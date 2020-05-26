using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace DirRX.Solution.Module.RecordManagement.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      base.Initializing(e);
      
      GrantRightsToReports(Roles.AllUsers);
      CreateReportTables();
      GrantRightOnFolder(Roles.AllUsers);
    }
    
    public static void CreateReportTables()
    {
      var brandRegistrationReportTableName = DirRX.Solution.Constants.BrandRegistrationReport.BrandRegistrationReportTableName;
      Sungero.Docflow.PublicFunctions.Module.DropReportTempTable(brandRegistrationReportTableName);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(DirRX.Solution.Queries.BrandRegistrationReport.CreateBrandRegistrationTable, new[] { brandRegistrationReportTableName });
    }
    
    /// <summary>
    /// Выдать права на отчет проверки наличия регистрации товарных знаков.
    /// </summary>
    public static void GrantRightsToReports(IRole allUsers)
    {
      Reports.AccessRights.Grant(DirRX.Solution.Reports.GetBrandRegistrationReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
    }
    
    public static void GrantRightOnFolder(IRole allUsers)
    {
    	
    	RecordManagement.SpecialFolders.OnResolutionProcessing.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
    	RecordManagement.SpecialFolders.OnResolutionProcessing.AccessRights.Save();
    }
  }
}
