using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Country;

namespace DirRX.Solution.Server
{
  partial class CountryFunctions
  {

    /// <summary>
    /// Получить дубли стран.
    /// </summary>
    /// <returns>Страны, дублирующие текущую.</returns>
    [Remote(IsPure = true)]
    public IQueryable<ICountry> GetDuplicatesByName()
    {
      return Countries.GetAll()
        .Where(c => c.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed && !Equals(c, _obj))
        .Where(c => c.EngName == _obj.EngName || c.LocalName == _obj.LocalName);
    }
    
    /// <summary>
    /// Получить страны, входящие в группу.
    /// </summary>
    /// <returns>Страны, входящие в группу.</returns>
    [Remote(IsPure = true)]
    public IQueryable<ICountry> GetGroupCountries()
    {
      return Countries.GetAll()
        .Where(c => c.Status != Sungero.CoreEntities.DatabookEntry.Status.Closed)
        .Where(c => !Equals(c, _obj))
        .Where(c => c.CountriesGroup.Select(g => g.GroupCountry).Any(x => DirRX.Solution.Countries.Equals(x, _obj)));
    }

  }
}