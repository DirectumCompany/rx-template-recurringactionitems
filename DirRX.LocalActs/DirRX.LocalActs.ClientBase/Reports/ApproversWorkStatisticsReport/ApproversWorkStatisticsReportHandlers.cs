using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.LocalActs
{
  partial class ApproversWorkStatisticsReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {
      var nullSubject = new List<LocalActs.IOrderSubject>();
      nullSubject.Add(LocalActs.OrderSubjects.Null);
      
      List<Enumeration> directions = new List<Enumeration>();
      directions.Add(Sungero.Docflow.DocumentKind.DocumentFlow.Incoming);
      directions.Add(Sungero.Docflow.DocumentKind.DocumentFlow.Inner);
      directions.Add(Sungero.Docflow.DocumentKind.DocumentFlow.Outgoing);
      
      var dialog = Dialogs.CreateInputDialog(DirRX.LocalActs.Reports.Resources.ApproversWorkStatisticsReport.ApproversWorkStatisticsReport);
      
      var beginDate = dialog.AddDate(DirRX.LocalActs.Reports.Resources.ApproversWorkStatisticsReport.BeginDate, true, Calendar.Today.AddMonths(-3));
      var endDate = dialog.AddDate(DirRX.LocalActs.Reports.Resources.ApproversWorkStatisticsReport.EndDate, true, Calendar.Today);
      var businessUnit = dialog.AddSelect(DirRX.LocalActs.Reports.Resources.ApproversWorkStatisticsReport.BusinessUnit, false, Sungero.Company.BusinessUnits.Null);
      
      var departments = dialog.AddSelectMany(DirRX.LocalActs.Reports.Resources.ApproversWorkStatisticsReport.Department, false, Sungero.Company.Departments.Null);
      var departmentsHyperlink = dialog.AddHyperlink(DirRX.LocalActs.Reports.Resources.ApproversWorkStatisticsReport.SelectDepartments);
      
      var approver = dialog.AddSelect(DirRX.LocalActs.Reports.Resources.ApproversWorkStatisticsReport.Approver, false, Sungero.Company.Employees.Null);
      var manager = dialog.AddSelect(DirRX.LocalActs.Reports.Resources.ApproversWorkStatisticsReport.Manager, false, Sungero.Company.Employees.Null);
      
      var docType = dialog.AddSelect(DirRX.LocalActs.Reports.Resources.ApproversWorkStatisticsReport.DocType, false, Sungero.Docflow.DocumentTypes.Null)
        .Where(d => d.DocumentTypeGuid != Constants.Module.RegulatoryDocumentTypeGuid.ToString() && d.DocumentFlow != Sungero.Docflow.DocumentType.DocumentFlow.Contracts);
      
      var subjects = dialog.AddSelectMany(DirRX.LocalActs.Reports.Resources.ApproversWorkStatisticsReport.Subject, false, LocalActs.OrderSubjects.Null);
      var standardForms = dialog.AddSelectMany(DirRX.LocalActs.Reports.Resources.ApproversWorkStatisticsReport.StandardForm, false, LocalActs.StandardForms.Null);
      
      var docKinds = dialog.AddSelectMany(DirRX.LocalActs.Reports.Resources.ApproversWorkStatisticsReport.DocKinds, false, Sungero.Docflow.DocumentKinds.Null).Where(d => d.DocumentFlow != Sungero.Docflow.DocumentKind.DocumentFlow.Contracts);
      
      var docKindsHyperlink = dialog.AddHyperlink(DirRX.LocalActs.Reports.Resources.ApproversWorkStatisticsReport.SelectDocKind);
      
      departments.IsEnabled = false;
      subjects.IsEnabled = false;
      standardForms.IsEnabled = false;
      docKinds.IsEnabled = false;
      
      businessUnit.SetOnValueChanged(
        arg =>
        {
          if (Equals(arg.NewValue, arg.OldValue))
            return;
          
          if (arg.NewValue != null && departments.Value.Any() && departments.Value.Any(d => !Sungero.Company.BusinessUnits.Equals(arg.NewValue, d.BusinessUnit)))
            departments = departments.Where(d => Sungero.Company.BusinessUnits.Equals(arg.NewValue, d.BusinessUnit));
          
          if (arg.NewValue != null && approver.Value != null && !Equals(DirRX.Solution.Employees.As(approver.Value).BusinessUnit, arg.NewValue))
            approver.Value = Sungero.Company.Employees.Null;
          
          departments.From(Functions.Module.Remote.GetFilteredDepartments(arg.NewValue));
          approver.From(Functions.Module.Remote.GetFilteredEmployees(arg.NewValue, departments.Value.ToList()));
        });
      
      departments.SetOnValueChanged(
        (sc) =>
        {
          if (sc.NewValue.Any() && approver.Value != null && !sc.NewValue.Contains(approver.Value.Department))
            approver.Value = Sungero.Company.Employees.Null;
          
          approver.From(Functions.Module.Remote.GetFilteredEmployees(businessUnit.Value, sc.NewValue.ToList()));
        });
      
      departmentsHyperlink.SetOnExecute(() =>
                                        {
                                          var selectedDepartments = Functions.Module.Remote.GetFilteredDepartments(businessUnit.Value).ShowSelectMany();
                                          if (selectedDepartments.Any())
                                            departments.Value = selectedDepartments;
                                        });
      
      docType.SetOnValueChanged(
        (sc) =>
        {
          if (Equals(sc.NewValue, sc.OldValue))
            return;
          
          var isOrder = false;
          if (sc.NewValue != null && sc.NewValue.DocumentTypeGuid == DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.Order)
          {
            isOrder = true;
            // Очистить поле Вид документа.
            var nullDocKind = new List<Sungero.Docflow.IDocumentKind>();
            nullDocKind.Add(Sungero.Docflow.DocumentKinds.Null);
            docKinds.Value = nullDocKind;
          }
          else
          {
            // Очистить поля Тема и Типовая форма.
            subjects.Value = nullSubject;
            
            var nullForm = new List<LocalActs.IStandardForm>();
            nullForm.Add(LocalActs.StandardForms.Null);
            standardForms.Value = nullForm;
            
            if (sc.NewValue != null && docKinds.Value.Any(d => !Sungero.Docflow.DocumentTypes.Equals(sc.NewValue, d.DocumentType)))
              docKinds = docKinds.Where(d => Sungero.Docflow.DocumentTypes.Equals(sc.NewValue, d.DocumentType));
            
            docKinds.From(Functions.Module.Remote.GetFilteredDocKinds(sc.NewValue, directions));
          }
          
          subjects.IsEnabled = isOrder;
          standardForms.IsEnabled = isOrder;
          docKindsHyperlink.IsEnabled = !isOrder;
        });
      
      subjects.SetOnValueChanged(
        (sc) =>
        {
          if (sc.NewValue.Any() && standardForms.Value.Any(d => !sc.NewValue.Contains(d.Subject)))
            standardForms = standardForms.Where(d => sc.NewValue.Contains(d.Subject));
          
          standardForms.From(Functions.Module.Remote.GetFilteredStandardForms(sc.NewValue.ToList()));
        });
      
      docKindsHyperlink.SetOnExecute(() =>
                                     {
                                       var selectedDocKinds = Functions.Module.Remote.GetFilteredDocKinds(docType.Value, directions).ShowSelectMany();
                                       if (selectedDocKinds.Any())
                                         docKinds.Value = selectedDocKinds;
                                     });
      
      dialog.SetOnButtonClick((args) =>
                              {
                                Sungero.Docflow.PublicFunctions.Module.CheckReportDialogPeriod(args, beginDate, endDate);
                              });
      
      dialog.Buttons.AddOkCancel();
      
      if (dialog.Show() == DialogButtons.Ok)
      {
        ApproversWorkStatisticsReport.BeginDate = beginDate.Value;
        ApproversWorkStatisticsReport.EndDate = endDate.Value;
        ApproversWorkStatisticsReport.BusinessUnit = businessUnit.Value;
        ApproversWorkStatisticsReport.Departments.AddRange(departments.Value.ToList());
        ApproversWorkStatisticsReport.Approver = approver.Value;
        ApproversWorkStatisticsReport.Manager = manager.Value;
        ApproversWorkStatisticsReport.DocType = docType.Value;
        ApproversWorkStatisticsReport.Subjects.AddRange(subjects.Value.ToList());
        ApproversWorkStatisticsReport.StandardForms.AddRange(standardForms.Value.ToList());
        ApproversWorkStatisticsReport.DocKinds.AddRange(docKinds.Value.ToList());
      }
      else
        e.Cancel = true;
    }

  }
}