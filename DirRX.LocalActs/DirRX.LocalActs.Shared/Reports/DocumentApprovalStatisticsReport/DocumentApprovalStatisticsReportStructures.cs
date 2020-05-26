using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.LocalActs.Structures.DocumentApprovalStatisticsReport
{
  /// <summary>
  /// Строчка отчета.
  /// </summary>
  partial class TableLine
  {
    public int Id { get; set; }

    public string Hyperlink { get; set; }
    
    public string DocumentInfo { get; set; }

    public string PlanDate { get; set; }

    public string ActualDate { get; set; }

    public string Author { get; set; }

    public int Overdue { get; set; }

    public string ReportSessionId { get; set; }
  }
}