using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ContractsCustom.Structures.ContractReport
{
	/// <summary>
	/// Строка отчета.
	/// </summary>
	partial class TableLine
	{
		public string   ReportSessionId { get; set; }
		public int      Number { get; set; }
		public string   CounterpartyName { get; set; }
		public string   ContractNumber { get; set; }
		public string   Supervisor { get; set; }
		public string   ManagementBoss { get; set; }
		public string   DivisionBoss { get; set; }
		public string   Executor { get; set; }
		public string   CoExecutor { get; set; }
		public double?  Amount { get; set; }
		public string   Currency { get; set; }
	}
	
}