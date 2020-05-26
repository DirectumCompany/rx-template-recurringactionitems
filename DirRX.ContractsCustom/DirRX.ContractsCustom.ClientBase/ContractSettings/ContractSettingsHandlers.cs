using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ContractSettings;

namespace DirRX.ContractsCustom
{
  partial class ContractSettingsClientHandlers
  {

    public virtual void TransactionAmountAnalysisRequiredValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(DirRX.Solution.Contracts.Resources.PositiveAmount);
    }

    public virtual void ContractTermAnalysisRequiredValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(DirRX.ContractsCustom.ContractSettingses.Resources.PositiveTerm);
    }

    public virtual void TransactionAmountValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(DirRX.Solution.Contracts.Resources.PositiveAmount);
    }

    public virtual void ContractTermValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(DirRX.ContractsCustom.ContractSettingses.Resources.PositiveTerm);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.ContractSettings.SetStateProperties(_obj);
    }

  }
}