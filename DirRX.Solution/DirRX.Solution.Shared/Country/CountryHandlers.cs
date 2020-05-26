using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Country;

namespace DirRX.Solution
{
  partial class CountrySharedHandlers
  {

    public virtual void GroupFlagChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      _obj.CountriesGroup.Clear();
      Functions.Country.SetPropertiesAvailability(_obj);
    }

  }
}