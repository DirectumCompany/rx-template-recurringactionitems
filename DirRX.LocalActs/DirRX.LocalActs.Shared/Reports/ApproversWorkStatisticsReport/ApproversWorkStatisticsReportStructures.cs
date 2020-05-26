using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;

namespace DirRX.LocalActs.Structures.ApproversWorkStatisticsReport
{

  /// <summary>
  /// Данные статистики по строчке отчета.
  /// </summary>
  partial class StatisticTableLine
  {
    
    public List<IOfficialDocument> Documents { get; set; }
    
    public int TotalAssignCount { get; set; }
    
    public int DoneAssignCount { get; set; }

    public int InWorkAssignCount { get; set; }
    
    public double ApproveTimeSum { get; set; }
    
    public int OverdueCount { get; set; }
    
    public double OverdueSum { get; set; }
    
  }

  /// <summary>
  /// Строчка отчета.
  /// </summary>
  partial class TableLine
  {
    
    public string Manager { get; set; }
    
    public string Approver { get; set; }

    public int DocumentCount { get; set; }
    
    public int TotalAssignCount { get; set; }
    
    public int DoneAssignCount { get; set; }

    public int InWorkAssignCount { get; set; }
    
    public double AverageApproveTime { get; set; }
    
    public int OverdueCount { get; set; }
    
    public double OverdueAverageValue { get; set; }
    
    public int Id { get; set; }
    
    public int Parent { get; set; }
    
    public bool IsDepartment { get; set; }

    public string ReportSessionId { get; set; }
  }
}