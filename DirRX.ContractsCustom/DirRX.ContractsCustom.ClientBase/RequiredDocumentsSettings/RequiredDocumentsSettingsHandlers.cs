using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.RequiredDocumentsSettings;

namespace DirRX.ContractsCustom
{
  partial class RequiredDocumentsSettingsClientHandlers
  {

    public virtual void TransactionAmountValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(DirRX.Solution.Contracts.Resources.PositiveAmount);
    }

    public virtual void DocumentValidityValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(DirRX.ContractsCustom.ContractSettingses.Resources.PositiveTerm);
    }

  }
}