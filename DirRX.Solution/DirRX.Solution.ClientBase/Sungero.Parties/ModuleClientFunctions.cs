using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Solution.Module.Parties.Client
{
  partial class ModuleFunctions
  {

    /// <summary>
    /// Построить специализированный отчёт для ГД.
    /// </summary>
    public virtual void BuildDocumentControlReport()
    {
      if (!Users.Current.IncludedIn(DirRX.PartiesControl.PublicConstants.Module.ArchiveResponsibleRole))
      {
        Dialogs.ShowMessage(DirRX.PartiesControl.Resources.NotRightsForReport, MessageType.Error);
        return;
      }
      
      var report = PartiesControl.Reports.GetDocumentControlReport();
      report.Open();
    }

    /// <summary>
    /// Построить отчет по проверкам СБ.
    /// </summary>
    public virtual void BuildSecurityReport()
    {
      if (!Users.Current.IncludedIn(DirRX.PartiesControl.PublicConstants.Module.SecurityServiceRole))
      {
        Dialogs.ShowMessage(DirRX.PartiesControl.Resources.NotRightsForReport, MessageType.Error);
        return;
      }
      
      var dialog = Dialogs.CreateInputDialog(PartiesControl.Resources.SecurityReportDialogTitle);
      var counterparty = dialog.AddSelect(PartiesControl.Resources.SecurityReportCounterpartyFieldTitle, false, DirRX.Solution.Companies.Null);
      var checkingResult = dialog.AddSelect(PartiesControl.Resources.SecurityReportCheckinkResultFieldTitle, false, PartiesControl.CheckingResults.Null);
      var periodFrom = dialog.AddDate(PartiesControl.Resources.SecurityReportPeriodFromFieldTitle, true);
      var periodTo = dialog.AddDate(PartiesControl.Resources.SecurityReportPeriodToFieldTitle, true);
      
      dialog.Buttons.AddOkCancel();
      dialog.Buttons.Default = DialogButtons.Ok;
      if (dialog.Show() == DialogButtons.Ok)
      {
        var report = PartiesControl.Reports.GetSecurityReport();
        report.Counterparty = counterparty.Value;
        report.CheckingResult = checkingResult.Value;
        report.PeriodFrom = periodFrom.Value;
        report.PeriodTo = periodTo.Value;
        report.Open();
      }
    }

  }
}