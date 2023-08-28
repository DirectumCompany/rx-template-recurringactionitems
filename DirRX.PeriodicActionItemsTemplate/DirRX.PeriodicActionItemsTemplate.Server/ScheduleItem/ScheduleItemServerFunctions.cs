using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PeriodicActionItemsTemplate.ScheduleItem;

namespace DirRX.PeriodicActionItemsTemplate.Server
{
  partial class ScheduleItemFunctions
  {
    
    /// <summary>
    /// Создать новую запись расписания отправки.
    /// </summary>
    /// <returns></returns>
    [Public, Remote(IsPure = true)]
    public static IScheduleItem CreateNew()
    {
      return ScheduleItems.Create();
    }
  }
}