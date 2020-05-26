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

    public virtual void ActionItemsPartsAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      // Задать порядковый номер для пункта поручения.
      var lastNumber = _obj.ActionItemsParts.OrderBy(j => j.Number).LastOrDefault();
      if (lastNumber.Number.HasValue)
        _added.Number = lastNumber.Number + 1;
      else
        _added.Number = 1;
    }
  }

  partial class RepeatSettingSharedHandlers
  {

    public virtual void AssigneeChanged(DirRX.PeriodicActionItemsTemplate.Shared.RepeatSettingAssigneeChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue &&
          _obj.Initiator != null && _obj.IsUnderControl.GetValueOrDefault() == true)
      {
        if (_obj.Category != null)
        {
          if (_obj.IsCompoundActionItem == false)
            _obj.Supervisor = DirRX.PeriodicActionItemsTemplate.PublicFunctions.Module.Remote.GetSupervisor(_obj.Initiator, _obj.Category,
                                                                                            Solution.Employees.As(e.NewValue));
          else
            _obj.Supervisor = DirRX.PeriodicActionItemsTemplate.PublicFunctions.Module.Remote.GetSupervisor(_obj.Initiator, _obj.Category, null);
        }
        if (!_obj.State.Properties.Supervisor.IsChanged || _obj.Supervisor == null)
          _obj.Supervisor = _obj.Initiator;
      }
    }

    public virtual void MonthTypeDayValueChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {

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
      
      Functions.RepeatSetting.SetStateproperties(_obj);
    }

    public virtual void BeginningDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
        _obj.BeginningDate = Calendar.Today;
    }

    public virtual void YearTypeDayOfWeekNumberChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
        _obj.YearTypeDayOfWeekNumber = ActionItems.RepeatSetting.YearTypeDayOfWeekNumber.First;
    }

    public virtual void YearTypeDayOfWeekChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
        _obj.YearTypeDayOfWeek = _obj.YearTypeDayOfWeek = ActionItems.RepeatSetting.YearTypeDayOfWeek.Monday;
    }

    public virtual void YearTypeDayValueChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
        _obj.YearTypeDayValue = 1;
    }

    public virtual void YearTypeMonthChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
        _obj.YearTypeMonth = ActionItems.RepeatSetting.YearTypeMonth.January;
    }

    public virtual void IsUnderControlChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      _obj.State.Properties.Supervisor.IsEnabled = (e.NewValue ?? false) && _obj.State.Properties.Assignee.IsEnabled;
      
      if (e.NewValue != e.OldValue && e.NewValue.GetValueOrDefault() == true &&
          _obj.Initiator != null)
      {
        if (_obj.Category != null)
        {
          if (_obj.IsCompoundActionItem == false && _obj.Assignee != null)
            _obj.Supervisor = DirRX.PeriodicActionItemsTemplate.PublicFunctions.Module.Remote.GetSupervisor(_obj.Initiator, _obj.Category,
                                                                                            Solution.Employees.As(_obj.Assignee));
          else
            _obj.Supervisor = DirRX.PeriodicActionItemsTemplate.PublicFunctions.Module.Remote.GetSupervisor(_obj.Initiator, _obj.Category, null);
        }
        if (!_obj.State.Properties.Supervisor.IsChanged || _obj.Supervisor == null)
          _obj.Supervisor = _obj.Initiator;
      }
      
      if (e.NewValue.GetValueOrDefault() == false)
        _obj.Supervisor = null;
    }

    public virtual void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue.Length > RepeatSettings.Info.Properties.Subject.Length)
        _obj.Subject = e.NewValue.Substring(0, RepeatSettings.Info.Properties.Subject.Length);
    }

    public virtual void InitiatorChanged(DirRX.PeriodicActionItemsTemplate.Shared.RepeatSettingInitiatorChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue && _obj.IsUnderControl.GetValueOrDefault() == true)
      {
        if (_obj.Category != null)
        {
          if (_obj.Assignee != null && _obj.IsCompoundActionItem == false)
            _obj.Supervisor = DirRX.PeriodicActionItemsTemplate.PublicFunctions.Module.Remote.GetSupervisor(e.NewValue, _obj.Category,
                                                                                            Solution.Employees.As(_obj.Assignee));
          else
            _obj.Supervisor = DirRX.PeriodicActionItemsTemplate.PublicFunctions.Module.Remote.GetSupervisor(e.NewValue, _obj.Category, null);
        }
        
        if (!_obj.State.Properties.Supervisor.IsChanged || _obj.Supervisor == null)
          _obj.Supervisor = _obj.Initiator;
      }
    }

    public virtual void PriorityChanged(DirRX.PeriodicActionItemsTemplate.Shared.RepeatSettingPriorityChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue)
      {
        if (_obj.IsUnderControl == false)
          _obj.IsUnderControl = e.NewValue.NeedsControl.GetValueOrDefault();
      }
    }

    public virtual void CategoryChanged(DirRX.PeriodicActionItemsTemplate.Shared.RepeatSettingCategoryChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        if (e.NewValue == null)
        {
          _obj.Priority = null;
          _obj.Supervisor = null;
        }
        else
        {
          _obj.Priority = e.NewValue.Priority;
          
          if (!e.NewValue.NeedsReportDeadline.GetValueOrDefault())
            _obj.ReportDeadline = null;
          
          if (_obj.Initiator != null && _obj.IsUnderControl == true)
          {
            if (_obj.Priority != null)
            {
              if (_obj.Assignee != null && _obj.IsCompoundActionItem == false)
                _obj.Supervisor = DirRX.PeriodicActionItemsTemplate.PublicFunctions.Module.Remote.GetSupervisor(_obj.Initiator, e.NewValue,
                                                                                                Solution.Employees.As(_obj.Assignee));
              else
                _obj.Supervisor = DirRX.PeriodicActionItemsTemplate.PublicFunctions.Module.Remote.GetSupervisor(_obj.Initiator, e.NewValue, null);
            }
            
            if (!_obj.State.Properties.Supervisor.IsChanged || _obj.Supervisor == null)
              _obj.Supervisor = _obj.Initiator;
          }
        }
      }
      
      Functions.RepeatSetting.SetStateproperties(_obj);
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
        
        // Установить тему.
        var subjectTemplate = _obj.IsCompoundActionItem == true ?
          Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject :
          Sungero.RecordManagement.ActionItemExecutionTasks.Resources.TaskSubject;
        _obj.Subject = Functions.RepeatSetting.GetActionItemExecutionSubject(_obj, subjectTemplate);
      }
      Functions.RepeatSetting.SetStateproperties(_obj);
    }

    public virtual void ActionItemChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (!Equals(e.NewValue, e.OldValue))
      {
        // Установить тему.
        var subjectTemplate = _obj.IsCompoundActionItem == true ?
          Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject :
          Sungero.RecordManagement.ActionItemExecutionTasks.Resources.TaskSubject;
        _obj.Subject = Functions.RepeatSetting.GetActionItemExecutionSubject(_obj, subjectTemplate);
        
        // Заменить первый символ на прописной.
        _obj.ActionItem = _obj.ActionItem != null ? _obj.ActionItem.Trim() : string.Empty;
        _obj.ActionItem = Sungero.Docflow.PublicFunctions.Module.ReplaceFirstSymbolToUpperCase(_obj.ActionItem);
      }
    }

    public virtual void MonthTypeDayChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
      {
        _obj.MonthTypeDay = ActionItems.RepeatSetting.MonthTypeDay.Date;
      }
      else
      {
        if (e.NewValue != e.OldValue)
        {
          if (e.NewValue == ActionItems.RepeatSetting.MonthTypeDay.DayOfWeek)
          {
            _obj.MonthTypeDayOfWeek = ActionItems.RepeatSetting.MonthTypeDayOfWeek.Monday;
            _obj.MonthTypeDayOfWeekNumber = ActionItems.RepeatSetting.MonthTypeDayOfWeekNumber.First;
          }
          else
          {
            _obj.MonthTypeDayOfWeek = null;
            _obj.MonthTypeDayOfWeekNumber = null;
          }
          
          if (e.NewValue == ActionItems.RepeatSetting.MonthTypeDay.Date)
            _obj.MonthTypeDayValue = 1;
          else
            _obj.MonthTypeDayValue = null;
          
          Functions.RepeatSetting.SetStateproperties(_obj);
        }
      }
    }

    public virtual void YearTypeDayChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
      {
        _obj.YearTypeDay = ActionItems.RepeatSetting.YearTypeDay.Date;
      }
      else
      {
        if (e.NewValue != e.OldValue)
        {
          if (e.NewValue == ActionItems.RepeatSetting.YearTypeDay.DayOfWeek)
          {
            _obj.YearTypeDayOfWeek = ActionItems.RepeatSetting.YearTypeDayOfWeek.Monday;
            _obj.YearTypeDayOfWeekNumber = ActionItems.RepeatSetting.YearTypeDayOfWeekNumber.First;
          }
          else
          {
            _obj.YearTypeDayOfWeek = null;
            _obj.YearTypeDayOfWeekNumber = null;
          }
          
          if (e.NewValue == ActionItems.RepeatSetting.YearTypeDay.Date)
            _obj.YearTypeDayValue = 1;
          else
            _obj.YearTypeDayValue = null;
          
          Functions.RepeatSetting.SetStateproperties(_obj);
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
        
        if (e.NewValue == ActionItems.RepeatSetting.Type.Year)
        {
          _obj.LabelType = DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.LabelYear;
          _obj.BeginningYear = Calendar.Today.BeginningOfYear();
          _obj.YearTypeMonth = ActionItems.RepeatSetting.YearTypeMonth.January;
          _obj.YearTypeDay = ActionItems.RepeatSetting.YearTypeDay.Date;
          _obj.YearTypeDayValue = 1;
        }
        else
        {
          _obj.BeginningYear = null;
          _obj.YearTypeMonth = null;
          _obj.YearTypeDay = null;
          _obj.YearTypeDayValue = null;
        }
        
        if (e.NewValue == ActionItems.RepeatSetting.Type.Month)
        {
          _obj.LabelType = DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.LabelMonth;
          _obj.BeginningMonth = Calendar.Today.BeginningOfYear();
          _obj.MonthTypeDay = ActionItems.RepeatSetting.MonthTypeDay.Date;
          _obj.MonthTypeDayValue = 1;
        }
        else
        {
          _obj.BeginningMonth = null;
          _obj.MonthTypeDay = null;
          _obj.MonthTypeDayValue = null;
        }

        if (e.NewValue == ActionItems.RepeatSetting.Type.Day)
          _obj.LabelType = DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.LabelDay;
        
        if (e.NewValue == ActionItems.RepeatSetting.Type.Week)
          _obj.LabelType = DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.LabelWeek;
        
        if (e.NewValue == ActionItems.RepeatSetting.Type.Week || e.NewValue == ActionItems.RepeatSetting.Type.Day)
        {
          _obj.BeginningDate = Calendar.Today;
        }
        else
        {
          _obj.BeginningDate = null;
        }
        
        Functions.RepeatSetting.SetStateproperties(_obj);
      }
    }

  }
}