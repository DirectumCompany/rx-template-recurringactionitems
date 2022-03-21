using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PeriodicActionItemsTemplate.RepeatSetting;

namespace DirRX.PeriodicActionItemsTemplate
{
  partial class RepeatSettingActionItemsPartsClientHandlers
  {

    public virtual void ActionItemsPartsNumberValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      // Проверить число на положительность.
      if (e.NewValue < 1)
        e.AddError(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.NumberIsNotPositive);
    }

    public virtual void ActionItemsPartsActionItemPartValueInput(Sungero.Presentation.TextValueInputEventArgs e)
    {
      if (!string.IsNullOrEmpty(e.NewValue))
        e.NewValue = e.NewValue.Trim();
      
      var resolutionPoint = e.NewValue;
      var allowableResolutionLength = RepeatSettings.Info.Properties.ActionItem.Length;
      if (!string.IsNullOrEmpty(resolutionPoint) && resolutionPoint.Length > allowableResolutionLength)
        e.AddError(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.AllowableLengthAssignmentsCharactersFormat(allowableResolutionLength));
    }
  }

  partial class RepeatSettingClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
    	_obj.State.Pages.ActionItem.Activate();
    }

    public virtual void MonthTypeDayValueValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && (e.NewValue <= 0 || e.NewValue > 31))
        e.AddError(RepeatSettings.Resources.IncorrectDayValueFormat(31));
    }

    public virtual void YearTypeDayValueValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue)
      {
        if ((_obj.YearTypeMonth == RepeatSetting.YearTypeMonth.January ||
             _obj.YearTypeMonth == RepeatSetting.YearTypeMonth.March ||
             _obj.YearTypeMonth == RepeatSetting.YearTypeMonth.May ||
             _obj.YearTypeMonth == RepeatSetting.YearTypeMonth.July ||
             _obj.YearTypeMonth == RepeatSetting.YearTypeMonth.August ||
             _obj.YearTypeMonth == RepeatSetting.YearTypeMonth.October ||
             _obj.YearTypeMonth == RepeatSetting.YearTypeMonth.December) &&
            (e.NewValue <= 0 || e.NewValue > 31))
          e.AddError(RepeatSettings.Resources.IncorrectDayValueFormat(31));
        
        if ((_obj.YearTypeMonth == RepeatSetting.YearTypeMonth.April ||
             _obj.YearTypeMonth == RepeatSetting.YearTypeMonth.June ||
             _obj.YearTypeMonth == RepeatSetting.YearTypeMonth.September ||
             _obj.YearTypeMonth == RepeatSetting.YearTypeMonth.November) &&
            (e.NewValue <= 0 || e.NewValue > 30))
          e.AddError(RepeatSettings.Resources.IncorrectDayValueFormat(30));
        
      	// Игнорируем високосные годы
        if (_obj.YearTypeMonth == RepeatSetting.YearTypeMonth.February && (e.NewValue <= 0 || e.NewValue > 28))
          e.AddError(RepeatSettings.Resources.IncorrectDayValueFormat(28));
      }
    }

    public virtual void CreationDaysValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(RepeatSettings.Resources.IncorrectValue);
    }

    public virtual void RepeatValueValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue <= 0)
        e.AddError(RepeatSettings.Resources.IncorrectValue);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.RepeatSetting.SetStateProperties(_obj);

      if (_obj.Type == DirRX.PeriodicActionItemsTemplate.RepeatSetting.Type.Month && _obj.MonthTypeDayValue.HasValue && _obj.MonthTypeDayValue.Value > 28 && _obj.MonthTypeDayValue.Value <= 31)
        e.AddInformation(RepeatSettings.Resources.IncorrectDaysOfMonthFormat(_obj.MonthTypeDayValue.Value));
      
      #region Скопировано из стандартной.
      
      var isComponentResolution = _obj.IsCompoundActionItem ?? false;

      var properties = _obj.State.Properties;
      
      properties.ActionItemsParts.IsVisible = isComponentResolution;
      
      properties.Assignee.IsVisible = !isComponentResolution;
      properties.CoAssignees.IsVisible = !isComponentResolution;

      properties.Supervisor.IsEnabled = (_obj.IsUnderControl ?? false) && _obj.State.Properties.Assignee.IsEnabled;
      
      e.Title = (_obj.Subject == Sungero.Docflow.Resources.AutoformatTaskSubject) ? null : _obj.Subject;
      
      #endregion
    }

  }
}