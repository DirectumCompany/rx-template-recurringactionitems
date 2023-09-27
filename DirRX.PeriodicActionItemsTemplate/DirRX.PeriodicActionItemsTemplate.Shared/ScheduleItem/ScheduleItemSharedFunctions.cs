using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PeriodicActionItemsTemplate.ScheduleItem;

namespace DirRX.PeriodicActionItemsTemplate.Shared
{
  partial class ScheduleItemFunctions
  {

    /// <summary>
    /// Сформировать имя записи расписания.
    /// </summary>
    public virtual void FillName()
    {
      var name = _obj.Info.LocalizedName;
      var subject = _obj.RepeatSetting?.Subject ?? string.Empty;
      var startDate = _obj.StartDate;
      
      if (!string.IsNullOrEmpty(subject))
      {
        // 180 - примерное число символов темы, которое точно влезет в имя записи.
        if (subject.Length > 180)
          subject = string.Format("{0}...", subject.Substring(0, 180));
        
        name = string.Format("{0} \"{1}\"", name, subject);
      }
      
      if (startDate.HasValue)
        name = DirRX.PeriodicActionItemsTemplate.ScheduleItems.Resources.ScheduleActionItemFromDateFormat(name, startDate.Value.ToShortDateString());
      
      if (_obj.Name != name)
        _obj.Name = name;
    }
  }
}