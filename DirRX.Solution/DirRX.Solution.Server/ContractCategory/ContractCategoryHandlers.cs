using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ContractCategory;

namespace DirRX.Solution
{
  partial class ContractCategorySupervisorPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> SupervisorFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
    	// Только роли с единственным участником.
    	return query.Where(x => Roles.Is(x) && Roles.As(x).IsSingleUser == true);
    }
  }

  partial class ContractCategoryServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      if (!_obj.State.IsCopied)
      {
        _obj.IsSupervisorFunctionManager = false;
        _obj.IsMainActivity = false;
        _obj.WorkWithOriginals = ContractCategory.WorkWithOriginals.Standart;
        _obj.DestinationCountry = DestinationCountry.NotRequired;
      }
    }
  }

}