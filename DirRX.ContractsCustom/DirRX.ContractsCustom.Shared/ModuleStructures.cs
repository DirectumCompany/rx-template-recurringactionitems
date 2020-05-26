using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ContractsCustom.Structures.Module
{
  /// <summary>
  /// Строчка отчета.
  /// </summary>
  partial class EnvelopeReportTableLine
  {
    public string ReportSessionId { get; set; }
    
    public int Id { get; set; }
    
    public string ToName { get; set; }
    
    public string FromName { get; set; }
    
    public string ToZipCode { get; set; }

    public string FromZipCode { get; set; }
    
    public string ToPlace { get; set; }
    
    public string FromPlace { get; set; }
    
    public string EmployeePhone { get; set; }
    
    public string ToContactName { get; set; }
    
    public string ContactPhone { get; set; }
  }

  /// <summary>
  /// Индекс и адрес без индекса.
  /// </summary>
  partial class ZipCodeAndAddress
  {
    public string ZipCode { get; set; }
    
    public string Address { get; set; }
  }
  
  /// <summary>
  /// Дата и курс валюты.
  /// </summary>
  [Public]
  partial class RateDate
  {
    public DateTime Date {get; set;}
    public double Rate {get; set;}
  }

  /// <summary>
  /// Валюта в RX и ее код с сайта ЦБ РФ.
  /// </summary>
  [Public]
  partial class CurrencyForQuery
  {
    public Sungero.Commons.ICurrency Currency {get; set;}
    public string Code {get; set;}
  }
  
  /// <summary>
  /// Сотрудник согласует документ в рамках задачи.
  /// </summary>
  [Public]
  partial class ApprovalStatus
  {
    /// <summary>
    /// Есть этап согласования, т.е. согласует.
    /// </summary>
    public bool HasApprovalStage { get; set; }
    
    /// <summary>
    /// Требуется строгая подпись.
    /// </summary>
    public bool NeedStrongSign { get; set; }
    
  }

}