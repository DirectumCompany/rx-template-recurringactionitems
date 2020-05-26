using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ContractCondition;

namespace DirRX.Solution
{
  partial class ContractConditionClientHandlers
  {

    public virtual void BalancePercentageValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (e.NewValue < 0)
        e.AddError(Sungero.Docflow.ConditionBases.Resources.NegativeTotalAmount);
    }

    public virtual void YearCountValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue < 0)
        e.AddError(DirRX.Solution.ContractConditions.Resources.YearCountValueCheck);
    }

  }
}