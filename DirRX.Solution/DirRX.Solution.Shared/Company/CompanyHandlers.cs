using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Company;

namespace DirRX.Solution
{
  partial class CompanySharedHandlers
  {

    public virtual void CheckingResultChanged(DirRX.Solution.Shared.CompanyCheckingResultChangedEventArgs e)
    {
      _obj.CheckingValidDate = Functions.Company.CalcCheckingValidDate(_obj);
    }

    public virtual void CheckingDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      _obj.CheckingValidDate = Functions.Company.CalcCheckingValidDate(_obj);
    }

    public virtual void IsLUKOILGroupChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      var simpleCheckingType = DirRX.PartiesControl.PublicFunctions.CheckingType.Remote.GetLukoilCheckingType();
      if (simpleCheckingType != null)
        _obj.CheckingType = simpleCheckingType;
    }

  }
}