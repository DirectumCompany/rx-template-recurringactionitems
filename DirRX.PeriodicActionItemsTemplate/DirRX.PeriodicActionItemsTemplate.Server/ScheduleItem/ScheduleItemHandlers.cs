using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PeriodicActionItemsTemplate.ScheduleItem;

namespace DirRX.PeriodicActionItemsTemplate
{
  partial class ScheduleItemServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // FIXME: Предсусмотреть актуализацию имени при изменении темы в записи Графика.
      Functions.ScheduleItem.FillName(_obj);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.HasIndefiniteDeadline = false;
    }
  }

}