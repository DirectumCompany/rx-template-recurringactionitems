using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Company;

namespace DirRX.Solution
{
  partial class CompanyClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      Functions.Company.SetPropertiesAvailability(_obj);
      if (_obj.IsCardReadOnly == true)
      {
        _obj.State.Properties.KSSSContragentId.IsEnabled = true;
        _obj.State.Properties.CounterpartyType.IsEnabled = true;
      }
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);
      
      _obj.State.Properties.CounterpartyType.IsRequired = true;
    }

  }
}