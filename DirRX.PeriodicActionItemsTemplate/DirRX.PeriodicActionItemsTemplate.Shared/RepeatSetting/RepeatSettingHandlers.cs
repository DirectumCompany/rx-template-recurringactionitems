using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PeriodicActionItemsTemplate.RepeatSetting;

namespace DirRX.PeriodicActionItemsTemplate
{
  partial class RepeatSettingActionItemsPartsSharedCollectionHandlers
  {

    public virtual void ActionItemsPartsDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e)
    {
      var partsCoAssignees = _obj.PartsCoAssignees.Where(p => p.PartGuid == _deleted.PartGuid).ToList();
      
      foreach (var partCoAssignees in partsCoAssignees)
      {
        _obj.PartsCoAssignees.Remove(partCoAssignees);
      }
    }

    public virtual void ActionItemsPartsAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      // Задать порядковый номер для пункта поручения.
      var lastNumber = _obj.ActionItemsParts.OrderBy(j => j.Number).LastOrDefault();
      if (lastNumber != null && lastNumber.Number.HasValue)
        _added.Number = lastNumber.Number + 1;
      else
        _added.Number = 1;
      
      _added.PartGuid = Guid.NewGuid().ToString();
    }
  }

  partial class RepeatSettingSharedHandlers
  {

    public virtual void MonthTypeDayChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
      {
        _obj.MonthTypeDay = RepeatSetting.MonthTypeDay.Date;
      }
      else
      {
        if (e.NewValue != e.OldValue)
        {
          if (e.NewValue == RepeatSetting.MonthTypeDay.DayOfWeek)
          {
            _obj.MonthTypeDayOfWeek = RepeatSetting.MonthTypeDayOfWeek.Monday;
            _obj.MonthTypeDayOfWeekNumber = RepeatSetting.MonthTypeDayOfWeekNumber.First;
          }
          else
          {
            _obj.MonthTypeDayOfWeek = null;
            _obj.MonthTypeDayOfWeekNumber = null;
          }
          
          if (e.NewValue == RepeatSetting.MonthTypeDay.Date)
            _obj.MonthTypeDayValue = 1;
          else
            _obj.MonthTypeDayValue = null;
          
          Functions.RepeatSetting.SetStateProperties(_obj);
        }
      }
    }

    public virtual void IsUnderControlChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        _obj.State.Properties.Supervisor.IsEnabled = e.NewValue.Value;
        _obj.State.Properties.Supervisor.IsRequired = e.NewValue.Value;
      }
    }

    public virtual void BeginningMonthChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
        _obj.BeginningMonth = Calendar.Today;
    }

    public virtual void BeginningYearChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
        _obj.BeginningYear = Calendar.Today;
    }

    public virtual void RepeatValueChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      if ((e.NewValue == null || e.NewValue == 1) && _obj.Type.HasValue && _obj.Type.Value == DirRX.PeriodicActionItemsTemplate.RepeatSetting.Type.Day)
        _obj.CreationDays = 0;
      
      Functions.RepeatSetting.SetStateProperties(_obj);
    }

    public virtual void BeginningDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
        _obj.BeginningDate = Calendar.Today;
    }

    public virtual void YearTypeDayOfWeekNumberChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
        _obj.YearTypeDayOfWeekNumber = RepeatSetting.YearTypeDayOfWeekNumber.First;
    }

    public virtual void YearTypeDayOfWeekChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
        _obj.YearTypeDayOfWeek = _obj.YearTypeDayOfWeek = RepeatSetting.YearTypeDayOfWeek.Monday;
    }

    public virtual void YearTypeDayValueChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
        _obj.YearTypeDayValue = 1;
    }

    public virtual void YearTypeMonthChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
        _obj.YearTypeMonth = RepeatSetting.YearTypeMonth.January;
    }

    public virtual void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue.Length > RepeatSettings.Info.Properties.Subject.Length)
        _obj.Subject = e.NewValue.Substring(0, RepeatSettings.Info.Properties.Subject.Length);
    }

    public virtual void IsCompoundActionItemChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.OldValue != e.NewValue)
      {
        // Заполнить данные из составного поручения в обычное и наоборот.
        if (e.NewValue.Value)
        {
          // Составное поручение.
          _obj.ActionItemsParts.Clear();
          _obj.FinalDeadline = _obj.Deadline;
          
          if (_obj.Assignee != null)
          {
            var newJob = _obj.ActionItemsParts.AddNew();
            newJob.Assignee = _obj.Assignee;
          }
          
          foreach (var job in _obj.CoAssignees)
          {
            var newJob = _obj.ActionItemsParts.AddNew();
            newJob.Assignee = job.Assignee;
          }
          _obj.CoAssignees.Clear();
        }
        else
        {
          // Не составное поручение.
          var actionItemPart = _obj.ActionItemsParts.OrderBy(x => x.Number).FirstOrDefault();
          if (_obj.FinalDeadline != null)
            _obj.Deadline = _obj.FinalDeadline;
          else if (actionItemPart != null)
            _obj.Deadline = actionItemPart.Deadline;
          else
            _obj.Deadline = null;
          
          if (actionItemPart != null)
            _obj.Assignee = actionItemPart.Assignee;
          else
            _obj.Assignee = null;
          
          _obj.CoAssignees.Clear();
          
          foreach (var job in _obj.ActionItemsParts.OrderBy(x => x.Number).Skip(1))
          {
            if (job.Assignee != null && !_obj.CoAssignees.Select(z => z.Assignee).Contains(job.Assignee))
              _obj.CoAssignees.AddNew().Assignee = job.Assignee;
          }
          
          if (string.IsNullOrEmpty(_obj.ActionItem) && actionItemPart != null)
          {
            _obj.ActionItem = actionItemPart.ActionItemPart;
          }

          // Чистим грид в составном, чтобы не мешать валидации.
          _obj.ActionItemsParts.Clear();
        }
        
        _obj.Subject = Functions.RepeatSetting.Remote.CreateSubject(_obj);;
      }
      Functions.RepeatSetting.SetStateProperties(_obj);
    }

    public virtual void ActionItemChanged(Sungero.Domain.Shared.TextPropertyChangedEventArgs e)
    {
      _obj.Subject = Functions.RepeatSetting.Remote.CreateSubject(_obj);
      
      // Заменить первый символ на прописной.
      var actionItem = _obj.ActionItem != null ? _obj.ActionItem.Trim() : string.Empty;
      actionItem = Sungero.Docflow.PublicFunctions.Module.ReplaceFirstSymbolToUpperCase(actionItem);
      if (_obj.ActionItem != actionItem)
        _obj.ActionItem = actionItem;
    }

    public virtual void YearTypeDayChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
      {
        _obj.YearTypeDay = RepeatSetting.YearTypeDay.Date;
      }
      else
      {
        if (e.NewValue != e.OldValue)
        {
          if (e.NewValue == RepeatSetting.YearTypeDay.DayOfWeek)
          {
            _obj.YearTypeDayOfWeek = RepeatSetting.YearTypeDayOfWeek.Monday;
            _obj.YearTypeDayOfWeekNumber = RepeatSetting.YearTypeDayOfWeekNumber.First;
          }
          else
          {
            _obj.YearTypeDayOfWeek = null;
            _obj.YearTypeDayOfWeekNumber = null;
          }
          
          if (e.NewValue == RepeatSetting.YearTypeDay.Date)
            _obj.YearTypeDayValue = 1;
          else
            _obj.YearTypeDayValue = null;
          
          Functions.RepeatSetting.SetStateProperties(_obj);
        }
      }
    }

    public virtual void TypeChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        _obj.RepeatValue = null;
        _obj.MonthTypeDay = null;
        _obj.WeekTypeFriday = false;
        _obj.WeekTypeMonday = false;
        _obj.WeekTypeThursday = false;
        _obj.WeekTypeTuesday = false;
        _obj.WeekTypeWednesday = false;
        
        if (e.NewValue == RepeatSetting.Type.Year)
        {
          _obj.LabelType = DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.LabelYear;
          _obj.BeginningYear = Calendar.Today.BeginningOfYear();
          _obj.YearTypeMonth = RepeatSetting.YearTypeMonth.January;
          _obj.YearTypeDay = RepeatSetting.YearTypeDay.Date;
          _obj.YearTypeDayValue = 1;
        }
        else
        {
          _obj.BeginningYear = null;
          _obj.YearTypeMonth = null;
          _obj.YearTypeDay = null;
          _obj.YearTypeDayValue = null;
        }
        
        if (e.NewValue == RepeatSetting.Type.Month)
        {
          _obj.LabelType = DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.LabelMonth;
          _obj.BeginningMonth = Calendar.Today.BeginningOfYear();
          _obj.MonthTypeDay = RepeatSetting.MonthTypeDay.Date;
          _obj.MonthTypeDayValue = 1;
        }
        else
        {
          _obj.BeginningMonth = null;
          _obj.MonthTypeDay = null;
          _obj.MonthTypeDayValue = null;
        }
        
        if (e.NewValue == RepeatSetting.Type.Arbitrary)
        {
          _obj.CreationDays = null;
          _obj.EndDate = null;
        }

        if (e.NewValue == RepeatSetting.Type.Day)
        {
          _obj.LabelType = DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.LabelDay;
          _obj.CreationDays = null;
        }
        
        if (e.NewValue == RepeatSetting.Type.Week)
        {
          _obj.LabelType = DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.LabelWeek;
          _obj.EndDate = null;
        }
        
        if (e.NewValue == RepeatSetting.Type.Week || e.NewValue == RepeatSetting.Type.Day)
        {
          _obj.BeginningDate = Calendar.Today;
        }
        else
        {
          _obj.BeginningDate = null;
        }
        
        Functions.RepeatSetting.SetStateProperties(_obj);
      }
    }

  }
}