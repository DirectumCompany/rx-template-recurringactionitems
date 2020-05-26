using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ContractsCustom
{
  partial class ContractReportClientHandlers
  {

    public override void BeforeExecute(Sungero.Reporting.Client.BeforeExecuteEventArgs e)
    {		
    	var dialog = Dialogs.CreateInputDialog(DirRX.ContractsCustom.Reports.Resources.ContractReport.ReportName);
			
			var pr = DirRX.Solution.Contracts.Info.Properties;
			
			var supervisor = dialog.AddSelect(pr.Supervisor.LocalizedName, false, DirRX.Solution.Employees.Null);
			var counterpaty = dialog.AddSelect(pr.Counterparty.LocalizedName, false, Sungero.Parties.Counterparties.Null);
			var managements = Functions.Module.Remote.GetDepartmentsForReport(true).ToList();
			var management = dialog.AddSelect(DirRX.ContractsCustom.Reports.Resources.ContractReport.Management, false, DirRX.Solution.Departments.Null).From(managements);
			var division = dialog.AddSelect(DirRX.ContractsCustom.Reports.Resources.ContractReport.Division, false, DirRX.Solution.Departments.Null).From(Functions.Module.Remote.GetDepartmentsForReport(false));
			var dateBegin = dialog.AddDate(DirRX.ContractsCustom.Reports.Resources.ContractReport.DateFrom, true, Calendar.Today.BeginningOfYear());
			var dateEnd = dialog.AddDate(DirRX.ContractsCustom.Reports.Resources.ContractReport.DateTo, true, Calendar.Today.EndOfYear());
			
			management.SetOnValueChanged(a => { division.IsEnabled = a.NewValue == null; });
			division.SetOnValueChanged(n => { management.IsEnabled = n.NewValue == null; });
			dialog.SetOnRefresh(
				y => 
				{ 
					if (dateBegin.Value.HasValue && dateEnd.Value.HasValue && dateBegin.Value > dateEnd.Value)
					{
						y.AddError(Resources.DateErr, dateBegin);
						y.AddError(Resources.DateErr, dateEnd);
					}
				}
			);
			
			if (dialog.Show() == DialogButtons.Ok)
			{
				ContractReport.Supervisor = supervisor.Value;
				ContractReport.Counterpaty = counterpaty.Value;
				ContractReport.Management = management.Value;
				ContractReport.Division = division.Value;
				ContractReport.DateBegin = dateBegin.Value;
				ContractReport.DateEnd = dateEnd.Value;
				ContractReport.Title = DirRX.ContractsCustom.Reports.Resources.ContractReport.TitleFormat(dateBegin.Value, dateEnd.Value);
			} 
			else
				e.Cancel = true;
    }

  }
}