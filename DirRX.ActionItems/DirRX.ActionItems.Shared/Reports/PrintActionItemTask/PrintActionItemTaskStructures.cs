using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ActionItems.Structures.PrintActionItemTask
{
  /// <summary>
  /// Строка отчета.
  /// </summary>
  [Public]
  partial class ResponsibilitiesReportTableLine
  {
    public string Assignee { get; set; }
    
    public string ActionItem { get; set; }
    
    public string Deadline { get; set; }
    
    public string Status { get; set; }
    
    public string ReportSessionId { get; set; }
  }
}