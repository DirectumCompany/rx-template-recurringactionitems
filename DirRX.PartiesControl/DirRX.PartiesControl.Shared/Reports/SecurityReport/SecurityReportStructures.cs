using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PartiesControl.Structures.SecurityReport
{
  
  /// <summary>
  /// Строка отчета.
  /// </summary>
  [Public]
  partial class SecurityReportTableLine
  {
    public int RequestId { get; set; }
    
    public string CounterpartyName { get; set; }
    
    public string CounterpartyReqs { get; set; }
    
    public string MainDocumentName { get; set; }
    
    public string CheckingDate { get; set; }
    
    public string CheckingResult { get; set; }
    
    public string Comment { get; set; }
    
    public string Note { get; set; }
    
    public string ReportSessionId { get; set; }
  }
}