using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.CheckingResult;

namespace DirRX.PartiesControl.Shared
{
  partial class CheckingResultFunctions
  {

    /// <summary>
    /// Установить обязательность свойств.
    /// </summary>
    public void SetRequiredProperties()
    {
      bool isApprovedDecision = _obj.Decision == DirRX.PartiesControl.CheckingResult.Decision.Approved;
      _obj.State.Properties.Reasons.IsRequired = isApprovedDecision;
      _obj.State.Properties.Types.IsRequired = isApprovedDecision;
      _obj.State.Properties.ValidPeriod.IsRequired = !_obj.ForOneDeal.GetValueOrDefault();
    }

  }
}