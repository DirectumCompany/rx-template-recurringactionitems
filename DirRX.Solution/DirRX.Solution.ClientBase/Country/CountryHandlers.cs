using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Country;

namespace DirRX.Solution
{
  partial class CountryClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      if (Users.Current.IncludedIn(DirRX.Brands.PublicConstants.Module.CountriesManagersRoleGuid))
      {
        _obj.State.Properties.GroupFlag.IsEnabled = false;
        _obj.State.IsEnabled = _obj.GroupFlag.HasValue ? !_obj.GroupFlag.Value : true;
      }
      Functions.Country.SetPropertiesAvailability(_obj);
    }

  }
}