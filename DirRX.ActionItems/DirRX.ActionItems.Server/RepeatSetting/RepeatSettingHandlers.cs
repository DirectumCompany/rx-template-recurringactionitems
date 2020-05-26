using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.RepeatSetting;

namespace DirRX.ActionItems
{
  partial class RepeatSettingServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if ((_obj.Type == DirRX.ActionItems.RepeatSetting.Type.Day || _obj.Type == DirRX.ActionItems.RepeatSetting.Type.Week) && _obj.BeginningDate >= _obj.EndDate ||
          _obj.Type == DirRX.ActionItems.RepeatSetting.Type.Month && _obj.BeginningMonth >= _obj.EndMonth ||
          _obj.Type == DirRX.ActionItems.RepeatSetting.Type.Year && _obj.BeginningYear >= _obj.EndYear)
      {
        e.AddError(_obj.Info.Properties.BeginningDate, RepeatSettings.Resources.IncorrectValidDate, _obj.Info.Properties.EndDate);
        e.AddError(_obj.Info.Properties.EndDate, RepeatSettings.Resources.IncorrectValidDate, _obj.Info.Properties.BeginningDate);
        e.AddError(_obj.Info.Properties.BeginningMonth, RepeatSettings.Resources.IncorrectValidDate, _obj.Info.Properties.EndMonth);
        e.AddError(_obj.Info.Properties.EndMonth, RepeatSettings.Resources.IncorrectValidDate, _obj.Info.Properties.BeginningMonth);
        e.AddError(_obj.Info.Properties.BeginningYear, RepeatSettings.Resources.IncorrectValidDate, _obj.Info.Properties.EndYear);
        e.AddError(_obj.Info.Properties.EndYear, RepeatSettings.Resources.IncorrectValidDate, _obj.Info.Properties.BeginningYear);
      }
      
      if (_obj.Type == DirRX.ActionItems.RepeatSetting.Type.Year)
      {
        _obj.BeginningDate = _obj.BeginningYear;
        _obj.EndDate = _obj.EndYear.HasValue ? _obj.EndYear.Value.EndOfYear() : _obj.EndYear;
      }
      
      if (_obj.Type == DirRX.ActionItems.RepeatSetting.Type.Month)
      {
        _obj.BeginningDate = _obj.BeginningMonth;
        _obj.EndDate = _obj.EndMonth.HasValue ? _obj.EndMonth.Value.EndOfMonth() : _obj.EndMonth;
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      #region Настройки повторений.
      
      _obj.LabelCreationsDays = _obj.Info.Properties.LabelCreationsDays.LocalizedName;
      _obj.LabelDayValue = _obj.Info.Properties.LabelDayValue.LocalizedName;
      _obj.Type = DirRX.ActionItems.RepeatSetting.Type.Day;
      
      if (!_obj.State.IsCopied)
      {
        _obj.WeekTypeMonday = false;
        _obj.WeekTypeFriday = false;
        _obj.WeekTypeThursday = false;
        _obj.WeekTypeTuesday = false;
        _obj.WeekTypeWednesday = false;
      }
      
      #endregion
      
      #region Настройки поручения.
      
      if (!_obj.State.IsCopied)
      {
        _obj.IsUnderControl = false;
        _obj.IsCompoundActionItem = false;
        _obj.Subject = Sungero.Docflow.Resources.AutoformatTaskSubject;
      }
      
      var subjectTemplate = _obj.IsCompoundActionItem == true ?
        Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject :
        Sungero.RecordManagement.ActionItemExecutionTasks.Resources.TaskSubject;
      _obj.Subject = Functions.RepeatSetting.GetActionItemExecutionSubject(_obj, subjectTemplate);
      
      #endregion
    }
    
  }

}