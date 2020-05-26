using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Country;

namespace DirRX.Solution
{
  partial class CountryCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      
      e.Without(_info.Properties.Name);
      e.Without(_info.Properties.EngName);
      e.Without(_info.Properties.LocalName);
    }
  }

  partial class CountryServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      _obj.Name = string.Format("{0}/{1}", _obj.LocalName, _obj.EngName);
      
      // Скопировано из стандартной разработки, т.к. необходимо отключить проверку заполненности кода.
      if (Functions.Country.HaveDuplicatesByName(_obj))
      {
        e.AddError(Sungero.Commons.Resources.DuplicateDetected, _obj.Info.Actions.ShowDuplicates);
        return;
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      if (!_obj.State.IsCopied)
      {
        _obj.GroupFlag = false;
        _obj.Name = Countries.Resources.CountryPrecursiveName;
        _obj.IsIncludedInDisputedTerritories = false;
      }
    }
  }

  partial class CountryCountriesGroupGroupCountryPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CountriesGroupGroupCountryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(g => g.GroupFlag == true);
    }
  }

}