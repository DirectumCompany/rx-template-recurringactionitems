using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ActionItems
{
  partial class CustomActionItemsExecutionReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      var personalSettings = Sungero.Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(null);
      var dialog = Dialogs.CreateInputDialog(DirRX.ActionItems.Reports.Resources.CustomActionItemsExecutionReport.CustomActionItemsExecutionReport);
      dialog.HelpCode = DirRX.ActionItems.Constants.CustomActionItemsExecutionReport.HelpCode;
      // Период.
      var settingsStartDate = Sungero.Docflow.PublicFunctions.PersonalSetting.GetStartDate(personalSettings);
      var beginDate = dialog.AddDate(Sungero.RecordManagement.Resources.PeriodFrom, true, settingsStartDate);
      var settingsEndDate = Sungero.Docflow.PublicFunctions.PersonalSetting.GetEndDate(personalSettings);
      var endDate = dialog.AddDate(Sungero.RecordManagement.Resources.PeriodTo, true, settingsEndDate);
      
      var author = dialog.AddSelect(DirRX.ActionItems.Reports.Resources.CustomActionItemsExecutionReport.AssignedBy, false, Solution.Employees.Null);
      var businessUnit = dialog.AddSelect(Sungero.Docflow.Resources.BusinessUnit, false, Solution.BusinessUnits.Null);
      var department = dialog.AddSelect(Sungero.RecordManagement.Resources.Department, false, Solution.Departments.Null);
      var performer = dialog.AddSelect(DirRX.ActionItems.Reports.Resources.CustomActionItemsExecutionReport.Performer, false, Solution.Employees.Null);
      var initiator = dialog.AddSelect(DirRX.ActionItems.Reports.Resources.CustomActionItemsExecutionReport.Initiator, false, Solution.Employees.Null);
      var priority = dialog.AddSelect(DirRX.ActionItems.Reports.Resources.CustomActionItemsExecutionReport.Priority, false, DirRX.ActionItems.Priorities.Null);
      var category = dialog.AddSelect(DirRX.ActionItems.Reports.Resources.CustomActionItemsExecutionReport.Category, false, DirRX.ActionItems.Categories.Null);
      var isEscalated = dialog.AddBoolean("Эскалировано", false);
      
      // проверить даты диалога
      dialog.SetOnButtonClick((args) =>
                              {
                                Sungero.Docflow.PublicFunctions.Module.CheckReportDialogPeriod(args, beginDate, endDate);
                              });
      
      dialog.Buttons.AddOkCancel();
      
      if (dialog.Show() == DialogButtons.Ok)
      {
        CustomActionItemsExecutionReport.BeginDate = beginDate.Value.Value;
        CustomActionItemsExecutionReport.ClientEndDate = endDate.Value.Value;
        CustomActionItemsExecutionReport.EndDate = endDate.Value.Value.EndOfDay();
        CustomActionItemsExecutionReport.Author = author.Value;
        CustomActionItemsExecutionReport.BusinessUnit = businessUnit.Value;
        CustomActionItemsExecutionReport.Department = department.Value;
        CustomActionItemsExecutionReport.Performer = performer.Value;
        CustomActionItemsExecutionReport.Initiator = initiator.Value;
        CustomActionItemsExecutionReport.Priority = priority.Value;
        CustomActionItemsExecutionReport.Category = category.Value;
        CustomActionItemsExecutionReport.IsEscalated = isEscalated.Value;
        
      }
      else
      {
        e.Cancel = true;
      }
    }

  }
}