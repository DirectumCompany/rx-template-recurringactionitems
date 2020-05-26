using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Solution
{
  partial class BrandRegistrationReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.BrandRegistrationReport.BrandRegistrationReportTableName, BrandRegistrationReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      BrandRegistrationReport.ProductKind = BrandRegistrationReport.Order.ProductKind.Name;
      
      
      var reportSessionId = System.Guid.NewGuid().ToString();
      BrandRegistrationReport.ReportSessionId = reportSessionId;
      
      var missBrands = Solution.Functions.Order.GetMissBrands(BrandRegistrationReport.Order).ToList();
      var reportData = new List<Structures.BrandRegistrationReport.BrandRegistrationReportTableLine>();
      
      foreach (var missBrand in missBrands)
      {
        foreach (var country in missBrand.Countries)
        {
          var newLine = new Structures.BrandRegistrationReport.BrandRegistrationReportTableLine();
          newLine.Brand = missBrand.WordMark.Name;
          newLine.Country = country.Country.DisplayValue;
          newLine.IsAppeal = missBrand.IsAppeal ? DirRX.Solution.Reports.Resources.BrandRegistrationReport.IsAppealDescription
            : DirRX.Solution.Reports.Resources.BrandRegistrationReport.IsNotAppealDescription;
          newLine.RegistrationNumber = country.RegistrationNumber != string.Empty ? country.RegistrationNumber : "-";
          newLine.ReportSessionId = reportSessionId;
          reportData.Add(newLine);
        }
      }
      Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.BrandRegistrationReport.BrandRegistrationReportTableName, reportData);
    }
  }
}