using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Brands.BrandsRegistration;

namespace DirRX.Brands
{
  partial class BrandsRegistrationFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return query;
      
      if (_filter.Determine || _filter.Overdue || _filter.Refused || _filter.Registered || _filter.Request)
        query = query.Where(x => (_filter.Determine && x.Status == Status.Determine) ||
                            (_filter.Overdue && x.Status == Status.Overdue) ||
                            (_filter.Refused && x.Status == Status.Refused) ||
                            (_filter.Registered && x.Status == Status.Registered) ||
                            (_filter.Request && x.Status == Status.Request));
      if (_filter.IsAppeal)
        query = query.Where(x => x.IsAppeal == true);
      
      if (_filter.International || _filter.National || _filter.Regional)
        query = query.Where(x => (_filter.International && x.RegistrationKind == RegistrationKind.International) ||
                            (_filter.National && x.RegistrationKind == RegistrationKind.National) ||
                            (_filter.Regional && x.RegistrationKind == RegistrationKind.Regional));
      
      if (_filter.Country != null)
      {
        var countriesGroup = _filter.Country.CountriesGroup != null ? _filter.Country.CountriesGroup.Select(g => g.GroupCountry).ToList() : null;
        query = query.Where(x => Solution.Countries.Equals(x.Country, _filter.Country) || 
                            (countriesGroup != null && countriesGroup.Contains(x.Country)));
      }
      
      if (_filter.ProductKind != null)
      {
        var productGroup = _filter.ProductKind.ProductGroups.Select(g => g.ProductGroup).ToList();
        query = query.Where(x => productGroup.Contains(x.ProductGroup));
      }
      
      if (_filter.EndOfMonth)
      {
        var periodBegin = Calendar.UserToday;
        var periodEnd = Calendar.UserToday.EndOfMonth();
        query = query.Where(x => x.ValidUntil.Between(periodBegin, periodEnd));
      }
      
      if (_filter.EndOfYear)
      {
        var periodBegin = Calendar.UserToday;
        var periodEnd = Calendar.UserToday.EndOfYear();
        query = query.Where(x => x.ValidUntil.Between(periodBegin, periodEnd));
      }
      
      if (_filter.ManualPeriod)
      {
        if (_filter.DateRangeFrom.HasValue || _filter.DateRangeTo.HasValue)
        {
          var periodBegin = _filter.DateRangeFrom ?? Calendar.SqlMinValue;
          var periodEnd = _filter.DateRangeTo ?? Calendar.SqlMaxValue;
          query = query.Where(x => x.ValidUntil.Between(periodBegin, periodEnd));
        }
      }

      return query;
    }
  }

  partial class BrandsRegistrationServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.RegistrationDate > _obj.ValidUntil)
      {
        e.AddError(_obj.Info.Properties.RegistrationDate, DirRX.Brands.BrandsRegistrations.Resources.RegistrationDateValidError, _obj.Info.Properties.ValidUntil);
        e.AddError(_obj.Info.Properties.ValidUntil, DirRX.Brands.BrandsRegistrations.Resources.RegistrationDateValidError, _obj.Info.Properties.RegistrationDate);
      }
      _obj.Name = DirRX.Brands.BrandsRegistrations.Resources.AutogenerateNameFormat(_obj.Brand.Name, _obj.Country.Name, _obj.ProductGroup.Name);
      
      if (_obj.Name.Length > _obj.Info.Properties.Name.Length)
        _obj.Name = _obj.Name.Substring(0, _obj.Info.Properties.Name.Length);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.Status = Status.Determine;
      _obj.IsAppeal = false;
    }
  }

}