using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.ShippingAddress;

namespace DirRX.PartiesControl.Server
{
  partial class ShippingAddressFunctions
  {

    /// <summary>
    /// Получить адреса отправки текущего контрагента.
    /// </summary>
    [Remote(IsPure = true), Public]
    public static IQueryable<IShippingAddress> GetShippingAdresses(DirRX.Solution.ICompany company)
    {
      return ShippingAddresses.GetAll(a => a.Counterparty.Equals(company));
    }

  }
}