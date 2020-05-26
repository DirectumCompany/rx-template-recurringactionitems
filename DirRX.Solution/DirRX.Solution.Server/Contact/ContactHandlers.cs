using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Contact;

namespace DirRX.Solution
{

  partial class ContactSubdivisionPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> SubdivisionFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      // Фильтровать подразделение по Организации.
      if (_obj.Company != null)
        query = query.Where(q => q.Counterparty.Equals(_obj.Company));
      
      return query;
    }
  }

}