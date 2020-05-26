using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.ShippingAddress;

namespace DirRX.PartiesControl
{
  partial class ShippingAddressServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      if (CallContext.CalledFrom(DirRX.Solution.Companies.Info))
      {
        var company = DirRX.Solution.Companies.GetAll(c => c.Id == CallContext.GetCallerEntityId(DirRX.Solution.Companies.Info)).FirstOrDefault();
        _obj.Counterparty = company;
      }
    }
  }

}