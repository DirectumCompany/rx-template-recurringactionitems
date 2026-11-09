using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PeriodicActionItemsTemplate.Shared
{
  public partial class ModuleFunctions
  {
    /// <summary>
    /// Создать график отправки периодических поручений по поручению-основанию.
    /// </summary>
    /// <param name="actionItem">Поручение-основание.</param>
    /// <returns>График отправки периодических поручений по поручению-основанию.</returns>
    [Public]
    public virtual IRepeatSetting CreateScheduleFromActionItem(Sungero.RecordManagement.IActionItemExecutionTask actionItem)
    {
      var schedule = Functions.RepeatSetting.Remote.CreateRepeatSetting();
      Functions.RepeatSetting.CopyActionItemToRepeatSetting(schedule, actionItem);
      return schedule;
    }

    /// <summary>
    /// Создать график отправки периодических поручений по документу-основанию.
    /// </summary>
    /// <param name="document">Документ-основание.</param>
    /// <returns>График отправки периодических поручений по документу-основанию.</returns>
    [Public]
    public virtual IRepeatSetting CreateScheduleFromDocument(Sungero.Docflow.IOfficialDocument document)
    {
      var schedule = Functions.RepeatSetting.Remote.CreateRepeatSetting();
      schedule.MainDocument = document;
      return schedule;
    }
  }
}