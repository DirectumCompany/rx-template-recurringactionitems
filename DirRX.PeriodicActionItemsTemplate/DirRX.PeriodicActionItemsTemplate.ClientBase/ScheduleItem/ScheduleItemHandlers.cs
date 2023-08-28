using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PeriodicActionItemsTemplate.ScheduleItem;

namespace DirRX.PeriodicActionItemsTemplate
{
  partial class ScheduleItemClientHandlers
  {

    public virtual void DeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (e.NewValue == null || e.NewValue == e.OldValue)
        return;
      
      if (_obj.StartDate.HasValue && e.NewValue < _obj.StartDate)
        e.AddError(DirRX.PeriodicActionItemsTemplate.ScheduleItems.Resources.StartDateGreaterThanDeadline);
    }

    public virtual void StartDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (e.NewValue == null || e.NewValue == e.OldValue)
        return;
      
      if (_obj.Deadline.HasValue && e.NewValue > _obj.Deadline)
        e.AddError(DirRX.PeriodicActionItemsTemplate.ScheduleItems.Resources.StartDateGreaterThanDeadline);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      var isEnabled = _obj.RepeatSetting.Type == RepeatSetting.Type.Arbitrary && _obj.ActionItemExecutionTask == null;
      _obj.State.Properties.StartDate.IsEnabled = isEnabled;
      _obj.State.Properties.Deadline.IsEnabled = isEnabled;
      
      _obj.State.Properties.Deadline.IsRequired = _obj.HasIndefiniteDeadline != true;
    }

  }
}