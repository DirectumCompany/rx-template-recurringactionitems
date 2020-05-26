using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.RecordCustom
{
  partial class IncomingDocumentsReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      var dialog = Dialogs.CreateInputDialog(DirRX.RecordCustom.Reports.Resources.IncomingDocumentsReport.IncomingDocumentsReport);
      var beginDate = dialog.AddDate(DirRX.RecordCustom.Reports.Resources.IncomingDocumentsReport.StartDate, true);
      var endDate = dialog.AddDate(DirRX.RecordCustom.Reports.Resources.IncomingDocumentsReport.EndDate, true);
      var businessUnit = dialog.AddSelect(DirRX.Solution.IncomingLetters.Info.Properties.BusinessUnit.LocalizedName, false, DirRX.Solution.BusinessUnits.Null);
      var department = dialog.AddSelect(DirRX.Solution.IncomingLetters.Info.Properties.Department.LocalizedName, false, DirRX.Solution.Departments.Null);
      var correspondent = dialog.AddSelect(DirRX.Solution.IncomingLetters.Info.Properties.Correspondent.LocalizedName, false, DirRX.Solution.Companies.Null);
      var correspondentDepartment = dialog.AddSelect(DirRX.RecordCustom.Reports.Resources.IncomingDocumentsReport.CorrespondentDepartment, false, DirRX.IntegrationLLK.DepartCompanieses.Null);
      var signedBy = dialog.AddSelect(DirRX.Solution.IncomingLetters.Info.Properties.SignedBy.LocalizedName, false, DirRX.Solution.Contacts.Null);
      correspondentDepartment.IsEnabled = false;
      signedBy.IsEnabled = false;
      
      correspondent.SetOnValueChanged(
        (sc) =>
        {
          if (sc.NewValue != sc.OldValue)
          {
            correspondentDepartment.Value = null;
            correspondentDepartment.IsEnabled = sc.NewValue != null;
            correspondentDepartment = correspondentDepartment.Where(d => DirRX.Solution.Companies.Equals(d.Counterparty, correspondent.Value));
            
            signedBy.Value = null;
            signedBy.IsEnabled = sc.NewValue != null;
            signedBy = signedBy.Where(x => DirRX.Solution.Companies.Equals(x.Company, correspondent.Value));
          }
        });
      
      correspondentDepartment.SetOnValueChanged(
        (sc) =>
        {
          if (sc.NewValue != sc.OldValue)
          {
            signedBy.Value = null;
            signedBy = signedBy.Where(x => x.Subdivision == null || DirRX.IntegrationLLK.DepartCompanieses.Equals(x.Subdivision, correspondentDepartment.Value));
          }
        });
      
      dialog.Buttons.AddOkCancel();
      dialog.Buttons.Default = DialogButtons.Ok;
      if (dialog.Show() == DialogButtons.Ok)
      {
        IncomingDocumentsReport.BeginDate = beginDate.Value;
        IncomingDocumentsReport.EndDate = endDate.Value;
        IncomingDocumentsReport.BusinessUnit = businessUnit.Value;
        IncomingDocumentsReport.Department = department.Value;
        IncomingDocumentsReport.Correspondent = correspondent.Value;
        IncomingDocumentsReport.CorrespondentDepartment = correspondentDepartment.Value;
        IncomingDocumentsReport.SignedBy = signedBy.Value;
      }
      else
        e.Cancel = true;
    }
  }
}