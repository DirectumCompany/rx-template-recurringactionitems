using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Solution.Module.Parties.Server
{
  partial class ArchiveCompletenessControlFolderHandlers
  {

    public virtual IQueryable<DirRX.PartiesControl.IRevisionRequest> ArchiveCompletenessControlDataQuery(IQueryable<DirRX.PartiesControl.IRevisionRequest> query)
    {
      query = query.Where(r => r.AllDocsReceived != true);
      
      if (_filter == null)
        return query;
      
      #region Фильтры по полям навигации
      
      // Фильтр "Контрагент".
      if (_filter.Counterparty != null)
        query = query.Where(r => Sungero.Parties.Counterparties.Equals(r.Counterparty, _filter.Counterparty));
      
      // Фильтр "Исполнитель".
      if (_filter.Performer != null)
        query = query.Where(r => DirRX.Solution.Employees.Equals(r.Assignee, _filter.Performer));
      #endregion
      
      #region Фильтр по датам      
      DateTime beginPeriod = Calendar.SqlMinValue;
      DateTime endPeriod = Calendar.UserToday.EndOfDay().FromUserTime();
      
      if (_filter.Last7DaysFlag)
        beginPeriod = Sungero.Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-7));
      
      if (_filter.Last30DaysFlag)
        beginPeriod = Sungero.Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-30));
      
      if (_filter.Last90DaysFlag)
        beginPeriod = Sungero.Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-90));
      
      if (_filter.CustomPeriodFlag)
      {
        if (_filter.DateRangeFrom.HasValue)
          beginPeriod = Sungero.Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(_filter.DateRangeFrom.Value);
        
        endPeriod = _filter.DateRangeTo.HasValue ? _filter.DateRangeTo.Value.EndOfDay().FromUserTime() : Calendar.SqlMaxValue;
      }
      
      query = query.Where(r => r.CheckingDate.HasValue && r.CheckingDate.Between(beginPeriod, endPeriod));
      #endregion
      
      return query;
    }
  }

  partial class PartiesHandlers
  {
  }
}