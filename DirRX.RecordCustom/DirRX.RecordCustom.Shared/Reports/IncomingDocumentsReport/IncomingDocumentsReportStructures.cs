using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.RecordCustom.Structures.IncomingDocumentsReport
{

  /// <summary>
  /// Поручение.
  /// </summary>
  partial class ActionItem
  {
    public string Assignee { get; set; }

    public string CoAssignees { get; set; }

    public DateTime Deadline { get; set; }
    
    public DateTime PerformDate { get; set; }
    
    public string Status { get; set; }
    
    public string Result { get; set; }
  }

  /// <summary>
  /// Строка данных для отчета.
  /// </summary>
  partial class TableLine
  {
    public int DocID { get; set; }
	
    public string Hyperlink { get; set; }
	
    public string RegNumber { get; set; }

    public DateTime? RegDate { get; set; }

    public string Correspondent { get; set; }

    public string CorrespondentDepartment { get; set; }

    public DateTime? CorrespondentDate { get; set; }

    public string CorrespondentNumber { get; set; }
    
    public string SignedBy { get; set; }
    
    public string Subject { get; set; }

    public string Assignee { get; set; }

    public string CoAssignees { get; set; }

    public DateTime? Deadline { get; set; }
    
    public string PerformDate { get; set; }
    
    public string Status { get; set; }
    
    public string Result { get; set; }

    public string ReportSessionId { get; set; }
    
    public int Overdue { get; set; }

    public int OutgoingDocID { get; set; }
    
    public string OutgoingDocHyperlink { get; set; }

    public string DocumentKind { get; set; }
    
    public string Addressee { get; set; }
    
    public string ActionItemText { get; set; }
    
    public string ReportingPeriod { get; set; }
    
   }
}