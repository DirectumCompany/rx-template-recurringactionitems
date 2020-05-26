﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ActionItems.Structures.CustomActionItemsExecutionReport
{
  /// <summary>
  /// Строчка отчета.
  /// </summary>
  partial class TableLine
  {
    public int Id { get; set; }

    public string Hyperlink { get; set; }

    public string ActionItemText { get; set; }

    public string Author { get; set; }

    public string State { get; set; }

    public string PlanDate { get; set; }

    public DateTime PlanDateSort { get; set; }

    public string ActualDate { get; set; }

    public int TimeInWork { get; set; }

    public string Assignee { get; set; }

    public string CoAssignees { get; set; }

    public string DocumentInfo { get; set; }

    public string ReportSessionId { get; set; }
    
    public string Initiator { get; set; }
    
    public string Priority { get; set; }
    
    public string Category { get; set; }
    
    public bool IsOverdue { get; set; }   
    
  }


}