using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ContractsCustom
{
	partial class ContractReportServerHandlers
	{

		public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
		{
			Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.ContractReport.SourceTableName, ContractReport.ReportSessionId);
		}

		public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
		{
			var documents = DirRX.Solution.Contracts.GetAll(d => d.LifeCycleState == DirRX.Solution.Contract.LifeCycleState.Active);
			
			// Фильтр по куратору.
			if (ContractReport.Supervisor != null)
				documents = documents.Where(d => Equals(d.Supervisor, ContractReport.Supervisor));
			// Фильтр по контрагенту.
			if (ContractReport.Counterpaty != null)
				documents = documents.Where(d => Equals(d.Counterparty, ContractReport.Counterpaty));
			
			// Фильтр по управлению к договору.
			if (ContractReport.Management != null)
				documents = documents.Where(d => d.CoExecutor != null && Equals(d.CoExecutor.HeadOffice, ContractReport.Management) ||
			                              d.CoExecutor == null && d.Department != null && Equals(d.Department.HeadOffice, ContractReport.Management));
			
			// Фильтр по отделу по договору.
			if (ContractReport.Division != null)
			  documents = documents.Where(d => d.CoExecutor != null && Equals(d.CoExecutor.Department, ContractReport.Division) ||
			                              d.CoExecutor == null && Equals(d.Department, ContractReport.Division));
			
			// Фильтр по дате начала.
			if (ContractReport.DateBegin.HasValue)
				documents = documents.Where(d => d.ActualDate.HasValue && d.ActualDate.Value >= ContractReport.DateBegin.Value.BeginningOfDay());
			// Фильтр по дате завершения.
			if (ContractReport.DateEnd.HasValue)
				documents = documents.Where(d => d.ActualDate.HasValue && d.ActualDate.Value <= ContractReport.DateEnd.Value.EndOfDay());
			
			documents = documents.OrderBy(d => d.Id);
			
			var reportSessionId = System.Guid.NewGuid().ToString();
			ContractReport.ReportSessionId = reportSessionId;
			
			// Заполнить данные.
			var dataTable = new List<Structures.ContractReport.TableLine>();
			
			var index = 0;
			foreach (var document in documents)
			{
				var tableLine = Structures.ContractReport.TableLine.Create();
				
				tableLine.ReportSessionId = reportSessionId;
				tableLine.Number = ++index;
				tableLine.CounterpartyName = document.Counterparty != null ? document.Counterparty.Name : string.Empty;
				tableLine.ContractNumber = document.RegistrationNumber != null ? document.RegistrationNumber.ToString() : string.Empty;
				tableLine.Supervisor = document.Supervisor != null ? document.Supervisor.Name : string.Empty;
				tableLine.ManagementBoss = document.Department != null && document.Department.HeadOffice != null && document.Department.HeadOffice.Manager != null ? document.Department.HeadOffice.Manager.Name : string.Empty;
				tableLine.DivisionBoss = document.Department != null && document.Department.Manager != null ? document.Department.Manager.Name : string.Empty;
				tableLine.Executor = document.ResponsibleEmployee != null ? document.ResponsibleEmployee.Name : string.Empty;
				tableLine.CoExecutor = document.CoExecutor != null ? document.CoExecutor.Name : "-";
				tableLine.Amount = document.TransactionAmount;
				tableLine.Currency = document.Currency != null ? document.Currency.Name : string.Empty;
				
				dataTable.Add(tableLine);
			}
			
			Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.ContractReport.SourceTableName, dataTable);
		}

	}
}