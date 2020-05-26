using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.CounterpartyStatus;

namespace DirRX.PartiesControl.Server
{
  partial class CounterpartyStatusFunctions
  {

    /// <summary>
    /// Создать статус контрагента.
    /// </summary>
    /// <param name="name">Наименование статуса контрагента.</param>
    [Public]
    public static ICounterpartyStatus CreateCounterpartyStatus(string sid, string name)
    {
      var counterpartyStatus = CounterpartyStatuses.GetAll(s => s.Sid == sid).FirstOrDefault();
      if (counterpartyStatus == null)
      {
        counterpartyStatus = CounterpartyStatuses.Create();
        counterpartyStatus.Sid = sid;
      }
      counterpartyStatus.Name = name;
      counterpartyStatus.Save();
      
      return counterpartyStatus;
    }
    
    /// <summary>
    /// Создать статус контрагента.
    /// </summary>
    [Public]
    public static ICounterpartyStatus CreateCounterpartyStatus(string name, bool forOneDeal)
    {
      var counterpartyStatus = CounterpartyStatuses.Create();
      counterpartyStatus.Name = name;
      counterpartyStatus.ForOneDeal = forOneDeal;
      counterpartyStatus.Save();
      
      return counterpartyStatus;
    }
    
    /// <summary>
    /// Получить статус контрагента.
    /// </summary>
    /// <param name="sid">Guid статуса контрагента.</param>
    /// <returns>Статуса контрагента.</returns>
    [Public, Remote(IsPure = true)]
    public static ICounterpartyStatus GetCounterpartyStatus(string sid)
    {
      return CounterpartyStatuses.GetAll(s => s.Sid == sid).FirstOrDefault();
    }

  }
}