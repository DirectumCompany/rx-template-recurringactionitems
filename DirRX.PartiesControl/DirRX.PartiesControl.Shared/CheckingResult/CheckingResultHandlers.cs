using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.CheckingResult;

namespace DirRX.PartiesControl
{
  partial class CheckingResultSharedHandlers
  {

    public virtual void ForOneDealChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      Functions.CheckingResult.SetRequiredProperties(_obj);
    }

    public virtual void DecisionChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      Functions.CheckingResult.SetRequiredProperties(_obj);
    }

  }
}