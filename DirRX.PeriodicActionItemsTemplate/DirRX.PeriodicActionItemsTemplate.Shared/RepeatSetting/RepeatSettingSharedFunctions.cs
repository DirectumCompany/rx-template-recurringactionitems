using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PeriodicActionItemsTemplate.RepeatSetting;

namespace DirRX.PeriodicActionItemsTemplate.Shared
{
  partial class RepeatSettingFunctions
  {
    public void SetStateProperties()
    {
      #region Ежегодно.
      
      var isYear = _obj.Type == PeriodicActionItemsTemplate.RepeatSetting.Type.Year;
      
      _obj.State.Properties.YearTypeDay.IsVisible = isYear;
      _obj.State.Properties.YearTypeDayOfWeek.IsVisible = isYear;
      _obj.State.Properties.YearTypeDayOfWeekNumber.IsVisible = isYear;
      _obj.State.Properties.YearTypeDayValue.IsVisible = isYear;
      _obj.State.Properties.YearTypeMonth.IsVisible = isYear;
      _obj.State.Properties.BeginningYear.IsVisible = isYear;
      _obj.State.Properties.EndYear.IsVisible = isYear;
      
      var isDateYearType = isYear && _obj.YearTypeDay == PeriodicActionItemsTemplate.RepeatSetting.YearTypeDay.Date;
      _obj.State.Properties.YearTypeDayValue.IsVisible = isDateYearType;
      
      var isDayOfWeekYearType = isYear && _obj.YearTypeDay == PeriodicActionItemsTemplate.RepeatSetting.YearTypeDay.DayOfWeek;
      _obj.State.Properties.YearTypeDayOfWeek.IsVisible = isDayOfWeekYearType;
      _obj.State.Properties.YearTypeDayOfWeekNumber.IsVisible = isDayOfWeekYearType;
      
      #endregion
      
      #region Ежемесячно.
      
      var isMonth = _obj.Type == PeriodicActionItemsTemplate.RepeatSetting.Type.Month;
      
      _obj.State.Properties.MonthTypeDay.IsVisible = isMonth;
      _obj.State.Properties.MonthTypeDayOfWeek.IsVisible = isMonth;
      _obj.State.Properties.MonthTypeDayOfWeekNumber.IsVisible = isMonth;
      _obj.State.Properties.MonthTypeDayValue.IsVisible = isMonth;
      _obj.State.Properties.BeginningMonth.IsVisible = isMonth;
      _obj.State.Properties.EndMonth.IsVisible = isMonth;
      
      var isDateMonthType = isMonth && _obj.MonthTypeDay == PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDay.Date;
      _obj.State.Properties.MonthTypeDayValue.IsVisible = isDateMonthType;
      _obj.State.Properties.LabelDayValue.IsVisible = isDateMonthType;
      
      var isDayOfWeekMonthType = isMonth && _obj.MonthTypeDay == PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDay.DayOfWeek;
      _obj.State.Properties.MonthTypeDayOfWeek.IsVisible = isDayOfWeekMonthType;
      _obj.State.Properties.MonthTypeDayOfWeekNumber.IsVisible = isDayOfWeekMonthType;
      
      #endregion
      
      #region Еженедельно.
      
      var isWeek = _obj.Type == PeriodicActionItemsTemplate.RepeatSetting.Type.Week;

      _obj.State.Properties.WeekTypeFriday.IsVisible = isWeek;
      _obj.State.Properties.WeekTypeMonday.IsVisible = isWeek;
      _obj.State.Properties.WeekTypeThursday.IsVisible = isWeek;
      _obj.State.Properties.WeekTypeTuesday.IsVisible = isWeek;
      _obj.State.Properties.WeekTypeWednesday.IsVisible = isWeek;
      
      #endregion
      
      #region Ежедневно.
      
      var isDay = _obj.Type == PeriodicActionItemsTemplate.RepeatSetting.Type.Day;

      #endregion
      
      #region Произвольный.
      
      var isArbitrary = _obj.Type == PeriodicActionItemsTemplate.RepeatSetting.Type.Arbitrary;
      
      _obj.State.Properties.RepeatValue.IsVisible = !isArbitrary;
      _obj.State.Properties.LabelType.IsVisible = !isArbitrary;
      
      #endregion
      
      _obj.State.Properties.BeginningDate.IsVisible = isWeek || isDay;
      _obj.State.Properties.EndDate.IsVisible = isWeek || isDay;
      
      _obj.State.Properties.CreationDays.IsVisible = !isDay && !isArbitrary;
      _obj.State.Properties.LabelCreationsDays.IsVisible = _obj.State.Properties.CreationDays.IsVisible;
      _obj.State.Properties.CreationDays.IsEnabled = !isDay || !(!_obj.RepeatValue.HasValue || _obj.RepeatValue.Value == 1);
      _obj.State.Properties.CreationDays.IsRequired = !isDay && !isArbitrary;
      
      var isComponentResolution = _obj.IsCompoundActionItem ?? false;

      _obj.State.Properties.Assignee.IsRequired = _obj.Info.Properties.Assignee.IsRequired || !isComponentResolution;
      _obj.State.Properties.ActionItem.IsRequired = _obj.Info.Properties.ActionItem.IsRequired || !isComponentResolution;
      
      // Проверить заполненность контролера, если поручение на контроле.
      _obj.State.Properties.Supervisor.IsRequired = _obj.Info.Properties.Supervisor.IsRequired || _obj.IsUnderControl == true;
      
      _obj.State.Pages.Schedule.IsVisible = !_obj.State.IsInserted;
    }
    
    /// <summary>
    /// Получить тему поручения.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="beginningSubject">Изначальная тема.</param>
    /// <returns>Сформированная тема поручения.</returns>
    public string GetActionItemExecutionSubject(CommonLibrary.LocalizedString beginningSubject)
    {
      var autoSubject = Sungero.Docflow.Resources.AutoformatTaskSubject;
      
      using (TenantInfo.Culture.SwitchTo())
      {
        var subject = beginningSubject.ToString();
        var actionItem = _obj.ActionItem;
        
        if (!string.IsNullOrWhiteSpace(actionItem))
        {
          var formattedResolution = Sungero.RecordManagement.PublicFunctions.ActionItemExecutionTask.FormatActionItemForSubject(actionItem, false);
          subject += string.Format(" {0}", formattedResolution);
        }
        
        subject = Sungero.Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
        
        if (subject != beginningSubject)
          return subject;
      }
      
      return autoSubject;
    }
    
  }
}