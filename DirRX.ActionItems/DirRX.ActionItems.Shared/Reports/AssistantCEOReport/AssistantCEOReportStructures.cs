using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ActionItems.Structures.AssistantCEOReport
{
  /// <summary>
  /// Строчка отчета.
  /// </summary>
  partial class TableLine
  {    
    public string Mark { get; set; }
    
    public int Id { get; set; }

    public string Hyperlink { get; set; }
    
    public string DocumentInfo { get; set; }

    public string ActionItemText { get; set; }

    public string PlanDate { get; set; }

    public string Status { get; set; }

    public string Supervisor { get; set; }

    public string NewPlanDate { get; set; }

    public string ReportSessionId { get; set; }
  }
}