using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ContractCategory;

namespace DirRX.Solution
{
  partial class ContractCategorySharedHandlers
  {

    public virtual void IsSupervisorFunctionManagerChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      _obj.Supervisor = null;
      Functions.ContractCategory.SetPropertiesAvailabilityAndRequired(_obj);      
    }

  }
}