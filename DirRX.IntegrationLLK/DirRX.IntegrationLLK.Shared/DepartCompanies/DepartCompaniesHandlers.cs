using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.IntegrationLLK.DepartCompanies;

namespace DirRX.IntegrationLLK
{
  partial class DepartCompaniesSharedHandlers
  {

    public virtual void ManagerChanged(DirRX.IntegrationLLK.Shared.DepartCompaniesManagerChangedEventArgs e)
    {
      if (e.NewValue != null && (_obj.Counterparty == null || !Equals( _obj.Counterparty, e.NewValue.Company)))
        _obj.Counterparty = e.NewValue.Company;
    }

    public virtual void HeadOfficeChanged(DirRX.IntegrationLLK.Shared.DepartCompaniesHeadOfficeChangedEventArgs e)
    {
      if (e.NewValue != null && (_obj.Counterparty == null || !Equals( _obj.Counterparty, e.NewValue.Counterparty)))
        _obj.Counterparty = e.NewValue.Counterparty;
    }

    public virtual void CounterpartyChanged(DirRX.IntegrationLLK.Shared.DepartCompaniesCounterpartyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        if (_obj.HeadOffice != null && !Equals(_obj.HeadOffice.Counterparty, e.NewValue))
          _obj.HeadOffice = null;
        if (_obj.Manager != null && !Equals(_obj.Manager.Company, e.NewValue))
          _obj.Manager = null;
      }
    }

  }
}