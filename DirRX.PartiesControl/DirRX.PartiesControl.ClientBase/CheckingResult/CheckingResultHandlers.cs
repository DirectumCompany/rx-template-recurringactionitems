using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.CheckingResult;

namespace DirRX.PartiesControl
{
  partial class CheckingResultClientHandlers
  {

    public virtual void ValidPeriodValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue.Value <= 0)
			{
				e.AddError(ActionItems.Resources.ValueMustBePositive);
			}
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.CheckingResult.SetRequiredProperties(_obj);
    }

  }
}