using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Solution.Structures.Parties.Company
{
  /// <summary>
  /// Параметры запроса в КССС.
  /// </summary>
  partial class CounterpartyRequest
  {
    public string REQUEST_ID { get; set; }
    
    public string SYSTEM_ID { get; set; }
    
    public string CATALOG_NAME { get; set; }
    
    public List<DirRX.Solution.Structures.Parties.Company.Condition> CONDITION { get; set; }
  }
  
  /// <summary>
  /// Параметры поска данных в КССС.
  /// </summary>
  partial class Condition
  {
    public string FIELD_NAME { get; set; }
    
    public string OPTION { get; set; }
    
    public string FIELD_VALUE { get; set; }
  }
  
  partial class SendCountrparty
  {
    public string KSSSContragentId { get; set; }
    
    public string Name { get; set; }
    
    public string TIN { get; set; }
    
    public string Employee { get; set; }
    
    public string EmployeeEmail { get; set; }
    
    public string RequestID { get; set; }
    
    public string LegalName { get; set; }
    
    public string HeadCompany { get; set; }
    
    public string Nonresident { get; set; }
    
    public string Person { get; set; }
    
    public string LegalForm { get; set; }
    
    public string TRRC { get; set; }
    
    public string PSRN { get; set; }
    
    public string NCEO { get; set; }
    
    public string NCEA { get; set; }
    
    public string Country { get; set; }
    
    public string LegalAddress { get; set; }
    
    public string PostalAddress { get; set; }
    
    public string Phones { get; set; }
    
    public string Email { get; set; }
    
    public string Homepage { get; set; }
    
    public string Note { get; set; }
    
    public string Active { get; set; }
    
    public string ActiveCodeKSSS { get; set; }
    
    public string CloseDate { get; set; }
    
    public string ReasonReorg { get; set; }
  }
}