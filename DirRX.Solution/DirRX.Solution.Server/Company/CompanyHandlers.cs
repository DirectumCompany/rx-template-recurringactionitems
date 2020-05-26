using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Company;

namespace DirRX.Solution
{
  partial class CompanyPostalShippingAddressPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> PostalShippingAddressFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(a => DirRX.Solution.Companies.Equals(_obj, a.Counterparty));
    }
  }

  partial class CompanyLegalShippingAddressPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> LegalShippingAddressFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(a => DirRX.Solution.Companies.Equals(_obj, a.Counterparty));
    }
  }

  partial class CompanyFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      query = base.Filtering(query, e);
      
      if (_filter == null)
        return query;
      
      if (_filter.Entrepreneur || _filter.Individual || _filter.Legal)
        query = query.Where(x => (_filter.Entrepreneur && x.CounterpartyType == CounterpartyType.Entrepreneur) ||
                            (_filter.Individual && x.CounterpartyType == CounterpartyType.Individual) ||
                            (_filter.Legal && x.CounterpartyType == CounterpartyType.Legal));
      
      if (_filter.CounterpartyStatus != null)
        query = query.Where(x => x.CounterpartyStatus.Equals(_filter.CounterpartyStatus));
      
      if (_filter.Resigent || _filter.NonResident)
        query = query.Where(x => (_filter.Resigent && x.Nonresident == false) ||
                            (_filter.NonResident && x.Nonresident == true));
      
      if (_filter.StrategicFlag)
        query = query.Where(x =>  x.IsStrategicPartner == true);
      
      if (_filter.LukoilFlag)
        query = query.Where(x => x.IsLUKOILGroup == true);
      
      var today = Calendar.UserToday;
      var beginPeriod = _filter.PeriodRangeFrom ?? Calendar.SqlMinValue;
      var endPeriod = _filter.PeriodRangeTo ?? Calendar.SqlMaxValue;
      
      if (_filter.SevenDays)
      {
        beginPeriod = today;
        endPeriod = today.AddDays(7);
      }
      
      if (_filter.ThirtyDays)
      {
        beginPeriod = today;
        endPeriod = today.AddDays(30);
      }
      
      if (_filter.NinetyDays)
      {
        beginPeriod = today;
        endPeriod = today.AddDays(90);
      }
      
      query = query.Where(x => !x.CheckingValidDate.HasValue || 
                          (x.CheckingValidDate.HasValue &&
                          x.CheckingValidDate.Value >= beginPeriod &&
                          x.CheckingValidDate.Value <= endPeriod));
      
      return query;
    }
  }

  partial class CompanyServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      _obj.IsDocumentsProvided = false;
      _obj.IsLUKOILGroup = false;
      _obj.IsSanctions = false;
      _obj.IsStrategicPartner = false;
      
      _obj.DeliveryMethod = DirRX.Solution.MailDeliveryMethods.GetAll(m => m.Name == Sungero.Docflow.MailDeliveryMethods.Resources.MailMethod).FirstOrDefault();
      
      var fullCheckingType = PartiesControl.CheckingTypes.GetAll(t => t.DefaultChecking == true).FirstOrDefault();
      if (fullCheckingType != null)
        _obj.CheckingType = fullCheckingType;
      
      var checkingRequiredStatus = PartiesControl.CounterpartyStatuses.GetAll(s => s.Name == DirRX.PartiesControl.CounterpartyStatuses.Resources.DefaultStatusCheckingRequired).FirstOrDefault();
      if (checkingRequiredStatus != null)
        _obj.CounterpartyStatus = checkingRequiredStatus;
    }
  }

}