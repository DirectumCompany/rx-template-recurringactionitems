using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PeriodicActionItemsTemplate.Client
{
  public partial class ModuleFunctions
  {
    
    /// <summary>
    /// Показать график периодических поручений по поручению-основанию.
    /// </summary>
    /// <param name="actionItem">Поручение-основание.</param>
    [Public]
    public virtual void ShowScheduleForActionItem(Sungero.RecordManagement.IActionItemExecutionTask actionItem)
    {
      var schedule = Functions.RepeatSetting.Remote.GetSettingByActionItem(actionItem);
      if (schedule != null)
        schedule.ShowModal();
    }
    
    /// <summary>
    /// Показать графики периодических поручений по документу-основанию.
    /// </summary>
    /// <param name="document">Документ-основание.</param>
    [Public]
    public virtual void ShowSchedulesForDocument(Sungero.Docflow.IOfficialDocument document)
    {
      var schedules = Functions.RepeatSetting.Remote.GetSettingsByDocument(document);
      if (schedules.Any())
      {
        if (schedules.Count() > 1)
          schedules.ShowModal();
        else
          schedules.First().ShowModal();
      }
    }
    
    /// <summary>
    /// Проверить возможность показа графиков периодических поручений по документу-основанию.
    /// </summary>
    /// <param name="document">Документ-основание.</param>
    /// <returns>True, если должна быть возможность, false - нет.</returns>
    [Public]
    public virtual bool CanShowPeriodicScheduleForDocument(Sungero.Docflow.IOfficialDocument document)
    {
      return !document.State.IsInserted;
    }
    
    /// <summary>
    /// Проверить возможность создания графика периодических поручений по документу-основанию.
    /// </summary>
    /// <param name="document">Документ-основание.</param>
    /// <returns>True, если должна быть возможность, false - нет.</returns>
    [Public]
    public virtual bool CanCreatePeriodicScheduleForDocument(Sungero.Docflow.IOfficialDocument document)
    {
      return !document.State.IsChanged &&
        document.AccessRights.CanUpdate() &&
        DirRX.PeriodicActionItemsTemplate.RepeatSettings.AccessRights.CanCreate() &&
        !Locks.GetLockInfo(document).IsLockedByOther;
    }
  }
}