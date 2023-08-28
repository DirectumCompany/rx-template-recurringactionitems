using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PeriodicActionItemsTemplate.RepeatSetting;

namespace DirRX.PeriodicActionItemsTemplate
{
  partial class RepeatSettingServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var isError = false;
      var isCompoundActionItem = _obj.IsCompoundActionItem == true;
      
      if ((_obj.Type == DirRX.PeriodicActionItemsTemplate.RepeatSetting.Type.Day || _obj.Type == DirRX.PeriodicActionItemsTemplate.RepeatSetting.Type.Week) && _obj.BeginningDate >= _obj.EndDate ||
          _obj.Type == DirRX.PeriodicActionItemsTemplate.RepeatSetting.Type.Month && _obj.BeginningMonth >= _obj.EndMonth ||
          _obj.Type == DirRX.PeriodicActionItemsTemplate.RepeatSetting.Type.Year && _obj.BeginningYear >= _obj.EndYear)
      {
        e.AddError(_obj.Info.Properties.BeginningDate, RepeatSettings.Resources.IncorrectValidDate, _obj.Info.Properties.EndDate);
        e.AddError(_obj.Info.Properties.EndDate, RepeatSettings.Resources.IncorrectValidDate, _obj.Info.Properties.BeginningDate);
        e.AddError(_obj.Info.Properties.BeginningMonth, RepeatSettings.Resources.IncorrectValidDate, _obj.Info.Properties.EndMonth);
        e.AddError(_obj.Info.Properties.EndMonth, RepeatSettings.Resources.IncorrectValidDate, _obj.Info.Properties.BeginningMonth);
        e.AddError(_obj.Info.Properties.BeginningYear, RepeatSettings.Resources.IncorrectValidDate, _obj.Info.Properties.EndYear);
        e.AddError(_obj.Info.Properties.EndYear, RepeatSettings.Resources.IncorrectValidDate, _obj.Info.Properties.BeginningYear);
        isError = true;
      }
      
      if (isCompoundActionItem && string.IsNullOrWhiteSpace(_obj.ActionItem) && _obj.ActionItemsParts.Any(i => string.IsNullOrEmpty(i.ActionItemPart)))
      {
        e.AddError(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.EmptyActionItem);
        isError = true;
      }
      
      if (_obj.Type == DirRX.PeriodicActionItemsTemplate.RepeatSetting.Type.Year)
      {
        _obj.BeginningDate = _obj.BeginningYear;
        _obj.EndDate = _obj.EndYear.HasValue ? _obj.EndYear.Value.EndOfYear() : _obj.EndYear;
      }
      
      if (_obj.Type == DirRX.PeriodicActionItemsTemplate.RepeatSetting.Type.Month)
      {
        _obj.BeginningDate = _obj.BeginningMonth;
        _obj.EndDate = _obj.EndMonth.HasValue ? _obj.EndMonth.Value.EndOfMonth() : _obj.EndMonth;
      }
      
      if (e.IsValid && !isError)
      {
        Functions.RepeatSetting.GrantRightsToParticipants(_obj);
        
        if (_obj.IsCompoundActionItem == true)
        {
          if (string.IsNullOrWhiteSpace(_obj.ActionItem) && !_obj.ActionItemsParts.Any(i => string.IsNullOrEmpty(i.ActionItemPart)))
            _obj.ActionItem = DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.DefaultActionItem;
        }
        
        if (!_obj.State.IsInserted)
          Functions.RepeatSetting.WriteChangeParticipantsActionToHistory(_obj);
        
        if (_obj.Status == Status.Closed && _obj.State.Properties.Status.OriginalValue == Status.Active)
        {
          // Проверить, есть ли активные записи расписания отправки.
          var activeScheduleItems = ScheduleItems.GetAll(si => Equals(si.RepeatSetting, _obj) && si.ActionItemExecutionTask == null && si.Status == ScheduleItem.Status.Active);
          foreach (var id in activeScheduleItems.Select(si => si.Id))
          {
            var asyncHandler = AsyncHandlers.CloseScheduleItem.Create();
            asyncHandler.ScheduleItemId = id;
            asyncHandler.ExecuteAsync();
          }
        }
        
      }
      
      _obj.State.Controls.ScheduleStateView.Refresh();
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.LabelCreationsDays = _obj.Info.Properties.LabelCreationsDays.LocalizedName;
      _obj.LabelDayValue = _obj.Info.Properties.LabelDayValue.LocalizedName;
      _obj.Type = DirRX.PeriodicActionItemsTemplate.RepeatSetting.Type.Day;
      _obj.TransferFromHoliday = DirRX.PeriodicActionItemsTemplate.RepeatSetting.TransferFromHoliday.No;
      _obj.RepeatValue = 1;
      
      if (!_obj.State.IsCopied)
      {
        // Настройки повторений
        _obj.WeekTypeMonday = false;
        _obj.WeekTypeFriday = false;
        _obj.WeekTypeThursday = false;
        _obj.WeekTypeTuesday = false;
        _obj.WeekTypeWednesday = false;
        
        // Настройки поручения.
        _obj.IsUnderControl = false;
        _obj.IsCompoundActionItem = false;
        _obj.HasIndefiniteDeadline = false;
      }

      _obj.Subject = Functions.RepeatSetting.CreateSubject(_obj);
    }
    
  }

}