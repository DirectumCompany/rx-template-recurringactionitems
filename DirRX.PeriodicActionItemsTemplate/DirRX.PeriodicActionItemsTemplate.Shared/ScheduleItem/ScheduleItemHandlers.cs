using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PeriodicActionItemsTemplate.ScheduleItem;

namespace DirRX.PeriodicActionItemsTemplate
{
  partial class ScheduleItemSharedHandlers
  {

    public virtual void RepeatSettingChanged(DirRX.PeriodicActionItemsTemplate.Shared.ScheduleItemRepeatSettingChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      if (e.NewValue != null)
        _obj.HasIndefiniteDeadline = e.NewValue.HasIndefiniteDeadline == true;
      else
        _obj.HasIndefiniteDeadline = false;
      
    }

  }
}