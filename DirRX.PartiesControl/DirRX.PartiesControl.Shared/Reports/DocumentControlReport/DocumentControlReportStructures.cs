using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PartiesControl.Structures.DocumentControlReport
{
  /// <summary>
  /// Строка отчета.
  /// </summary>
  [Public]
  partial class DocumentControlReportTableLine
  {
    public string ReportSessionId { get; set; }
    
    public string Responsible { get; set; }
    
    public string DepartmentResponsible { get; set; }
    
    public string Supervisor { get; set; }
    
    public string Counterparty { get; set; }
    
    public string Date { get; set; }
    
    public string Days { get; set; }
    
    public string Documents { get; set; }
  }
}