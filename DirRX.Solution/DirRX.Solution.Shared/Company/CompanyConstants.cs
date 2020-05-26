using System;
using Sungero.Core;

namespace DirRX.Solution.Constants.Parties
{
  public static class Company
  {
    /// <summary>
    /// Параметры запроса в КССС.
    /// </summary>
    public static class KSSSParams
    {
      public const string INNFieldName = "Catalog_CONTRAGENT_PS/INN";
      
      public const string CSCDIDFieldName = "Catalog_CONTRAGENT_PS/CSCD_ID";      
      
      public const string SystemID = "DIRECTUM";
      
      public const string EqualsOption = "EQUALS";
      
      public const string CatalogName = "Catalog_CONTRAGENT";
      
      public const string SendCounterpartyInfo = "SendCounterpartyInfo";
    }
    
    /// <summary>
    /// Параметры запроса в КСШ о включении/исключении контрагентов в стоп-лист.
    /// </summary>
    [Public]
    public static class CSBStoplistAction
    {
      [Public]
      public const string Include = "Include";
      [Public]
      public const string Exclude = "Exclude";
    }
    
    /// <summary>
    /// Параметры запроса в КСШ о статусе события включении/исключении контрагентов в стоп-лист.
    /// </summary>
    [Public]
    public static class CSBStoplistStatus
    {
      [Public]
      public const string Started = "Started";
      [Public]
      public const string Ended = "Ended";
    }
  }
}