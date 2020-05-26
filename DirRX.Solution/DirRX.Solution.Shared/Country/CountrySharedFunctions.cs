using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Country;

namespace DirRX.Solution.Shared
{
  partial class CountryFunctions
  {

    /// <summary>
    /// Проверить дубли стран.
    /// </summary>
    /// <returns>True, если дубликаты имеются, иначе - false.</returns>
    public bool HaveDuplicatesByName()
    {
      if (string.IsNullOrWhiteSpace(_obj.Name) || _obj.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed)
        return false;
      
      return Functions.Country.Remote.GetDuplicatesByName(_obj).Any();
    }

    /// <summary>
    /// Установить доступность свойств.
    /// </summary>       
    public void SetPropertiesAvailability()
    {
      _obj.State.Properties.CountriesGroup.IsEnabled = !_obj.GroupFlag ?? true;
    }

  }
}