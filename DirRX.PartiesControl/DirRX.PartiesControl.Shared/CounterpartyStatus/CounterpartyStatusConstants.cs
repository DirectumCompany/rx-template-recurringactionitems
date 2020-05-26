using System;
using Sungero.Core;

namespace DirRX.PartiesControl.Constants
{
  public static class CounterpartyStatus
  {
    
    /// <summary>
    /// GUID`ы статусов контрагентов по умолчанию.
    /// </summary>
    public static class DefaultStatus
    {
      [Public]
      public const string CheckingRequiredSid = "998D5EB6-4C79-4755-9073-D5AE54FE6FED";
      [Public]
      public const string StopListSid = "E2F45AA0-EA0D-4CB2-9410-7CE98A93C9FC";
    }
  }
}