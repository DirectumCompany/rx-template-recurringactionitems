using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PartiesControl.Structures.InternalInventoryReport
{
  /// <summary>
  /// Структура для хранения строки таблицы.
  /// </summary>
  partial class TableLine
  {
    public string ReportSessionId { get; set; }
    
    public int Id {get; set;}
    
    public string DocName { get; set; }
    
    public string Comment { get; set; }
    
    public string Format { get; set; }
    
    public string SendingDate { get; set; }
    
    public string RecievingDate { get; set; }
  }

}