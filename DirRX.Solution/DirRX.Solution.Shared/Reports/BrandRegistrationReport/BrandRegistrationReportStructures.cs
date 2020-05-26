using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Solution.Structures.BrandRegistrationReport
{
  /// <summary>
  /// Строка отчета.
  /// </summary>
  [Public]
  partial class BrandRegistrationReportTableLine
  {
    public string Brand { get; set; }
    
    public string RegistrationNumber { get; set; }
    
    public string Country { get; set; }
    
    public string IsAppeal { get; set; }
    
    public string ReportSessionId { get; set; }
  }
}