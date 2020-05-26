using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.IntegrationLLK.DepartCompanies;

namespace DirRX.IntegrationLLK
{
  partial class DepartCompaniesManagerPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ManagerFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      // Отображать контактные лица указанного контрагента.
      if (_obj.Counterparty != null)
        query = query.Where(q => q.Company.Equals(_obj.Counterparty));
      
      return query;
    }
  }

  partial class DepartCompaniesHeadOfficePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> HeadOfficeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      // Фильтровать головное подразделение по подразделениям указанного контрагента.
      if (_obj.Counterparty != null)
        query = query.Where(q => q.Counterparty.Equals(_obj.Counterparty));
      
      return query.Where(q => !Equals(q, _obj));
    }
  }

}