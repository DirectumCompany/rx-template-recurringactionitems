using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain;

namespace DirRX.PeriodicActionItemsTemplate.Server
{
  public partial class ModuleFunctions
  {
    
    #region ФП по актуализации расписания
    
    public virtual void CheckAndUpdateDatesInActiveScheduleItems()
    {
      var logger = Logger.WithLogger("CheckAndUpdateDatesInActiveScheduleItems");
      logger.Debug("Start");
      
      var scheduleItemsIdsToProcess = ScheduleItems.GetAll(si => si.ActionItemExecutionTask == null &&
                                                           si.Status == DirRX.PeriodicActionItemsTemplate.ScheduleItem.Status.Active)
        .OrderBy(si => si.Id)
        .Select(si => si.Id);
      var totalCount = scheduleItemsIdsToProcess.Count();
      logger.Debug("Total count of schedule items to process - {totalCount}", totalCount);
      
      var scheduleItemsIds = new List<long>();
      
      var chunkSize = 1000;
      var processedCount = 0;
      
      while (processedCount < totalCount)
      {
        if (processedCount + chunkSize > totalCount)
          chunkSize = totalCount - processedCount;
        
        var portionOfIds = scheduleItemsIdsToProcess.Skip(processedCount).Take(chunkSize).ToList();
        scheduleItemsIds.AddRange(portionOfIds);
        
        processedCount += portionOfIds.Count;
      }
      
      foreach (var id in scheduleItemsIds)
      {
        var scheduleItem = ScheduleItems.GetAll(x => x.Id == id).FirstOrDefault();
        
        if (Locks.GetLockInfo(scheduleItem).IsLockedByOther)
        {
          logger.Debug("Schedule item with id = {id} skipped because of lock", scheduleItem.Id);
          continue;
        }
        
        if (scheduleItem != null)
        {
          // Ежедневные поручения с шагом в 1 день, переносить не надо, т.к. на соседних рабочих датах уже должны быть другие поручения. Их надо закрывать.
          if (scheduleItem.RepeatSetting.Type == DirRX.PeriodicActionItemsTemplate.RepeatSetting.Type.Day &&
              (scheduleItem.RepeatSetting.RepeatValue ?? 1) == 1)
          {
            if (!scheduleItem.StartDate.Value.IsWorkingDay())
            {
              logger.Debug("Daily schedule item with id = {id} closed", scheduleItem.Id);
              scheduleItem.Status = DirRX.PeriodicActionItemsTemplate.ScheduleItem.Status.Closed;
              scheduleItem.Save();
            }
            continue;
          }
          
          if (scheduleItem.RepeatSetting.TransferFromHoliday != DirRX.PeriodicActionItemsTemplate.RepeatSetting.TransferFromHoliday.No)
          {
            var startDate = scheduleItem.StartDate;
            var deadline = scheduleItem.Deadline;
            if (!scheduleItem.StartDate.Value.IsWorkingDay())
              scheduleItem.StartDate = TransferDateFromHolidays(scheduleItem.StartDate.Value, scheduleItem.RepeatSetting.TransferFromHoliday);
            
            if (scheduleItem.HasIndefiniteDeadline != true && !scheduleItem.Deadline.Value.IsWorkingDay())
              scheduleItem.Deadline = TransferDateFromHolidays(scheduleItem.Deadline.Value, scheduleItem.RepeatSetting.TransferFromHoliday);
            
            if (scheduleItem.State.IsChanged)
            {
              logger.Debug("Updating dates in schedule item with id = {id}. StartDate: {oldStartDate} -> {currentStartDate}. deadline: {oldDeadline} -> {currentDeadline}",
                           scheduleItem.Id,
                           startDate,
                           scheduleItem.StartDate,
                           deadline,
                           scheduleItem.Deadline);
              scheduleItem.Save();
            }
          }
        }
        
      }
      
      logger.Debug("Finish");
    }
    
    #endregion
    
    /// <summary>
    /// Получить ответственных за периодические поручения.
    /// </summary>
    /// <returns>Реципиент, ответственный за периодику.</returns>
    public virtual IRecipient GetResponsibleForPeriodic()
    {
      return Roles.Administrators;
    }
    
    /// <summary>
    /// Получить сотрудника или его руководителя.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Сотрудник либо руководитель его подразделения, если сотрудник закрыт.</returns>
    public Sungero.Company.IEmployee GetEmployeeOrManager(Sungero.Company.IEmployee employee)
    {
      if (employee == null)
        return employee;
      return employee.Status == Sungero.CoreEntities.DatabookEntry.Status.Active ? employee : employee.Department.Manager;
    }
    
    /// <summary>
    /// Создать и отправить поручение по расписанию отправки.
    /// </summary>
    /// <param name="scheduleItem">Расписание отправки.</param>
    public virtual void StartActionItemFromScheduleItem(IScheduleItem scheduleItem)
    {
      var setting = scheduleItem.RepeatSetting;
      
      var task = Sungero.RecordManagement.ActionItemExecutionTasks.Create();
      
      var mainDocument = scheduleItem.RepeatSetting.MainDocument;
      if (mainDocument != null)
        task.DocumentsGroup.OfficialDocuments.Add(mainDocument);
      
      task.AssignedBy = GetEmployeeOrManager(setting.AssignedBy);
      task.ActiveText = setting.ActionItem;
      task.IsUnderControl = setting.IsUnderControl == true;
      task.Supervisor = GetEmployeeOrManager(setting.Supervisor);
      
      // FIXME: На реальных проектах переделать на заполнение свойств из перекрытий.
      ((Sungero.Domain.Shared.IExtendedEntity)task).Params.Add("CreatedAsPeriodic", true);

      var coAssigneesDeadlineOffset = Sungero.RecordManagement.PublicFunctions.Module.GetSettings()?.ControlRelativeDeadlineInDays ?? 1;
      if (setting.IsCompoundActionItem == true)
      {
        task.IsCompoundActionItem = true;
        
        foreach (var actionItemPartsSetting in setting.ActionItemsParts)
        {
          var actionItemParts = task.ActionItemParts.AddNew();
          actionItemParts.Assignee = GetEmployeeOrManager(actionItemPartsSetting.Assignee);
          actionItemParts.ActionItemPart = actionItemPartsSetting.ActionItemPart;
          actionItemParts.Supervisor = GetEmployeeOrManager(actionItemPartsSetting.Supervisor);
          
          foreach (var partsCoAssigneeSetting in setting.PartsCoAssignees.Where(pca => pca.PartGuid == actionItemPartsSetting.PartGuid))
          {
            var partCoAssignee = task.PartsCoAssignees.AddNew();
            partCoAssignee.PartGuid = actionItemParts.PartGuid;
            partCoAssignee.CoAssignee = GetEmployeeOrManager(partsCoAssigneeSetting.CoAssignee);
          }
          
          if (setting.HasIndefiniteDeadline != true && task.PartsCoAssignees.Any(pca => pca.PartGuid == actionItemParts.PartGuid))
            actionItemParts.CoAssigneesDeadline = scheduleItem.Deadline.Value.AddWorkingDays(-coAssigneesDeadlineOffset).Date;
          
          actionItemParts.CoAssignees = string.Join("; ", task.PartsCoAssignees
                                                    .Where(pca => pca.PartGuid == actionItemParts.PartGuid)
                                                    .Select(x => x.CoAssignee.Person.ShortName));
        }
        
        if (setting.HasIndefiniteDeadline == true)
          task.HasIndefiniteDeadline = true;
        else
          task.FinalDeadline = scheduleItem.Deadline;
      }
      else
      {
        task.Assignee = GetEmployeeOrManager(setting.Assignee);
        
        foreach (var coAssigneeSetting in setting.CoAssignees)
        {
          var coAssignee = task.CoAssignees.AddNew();
          coAssignee.Assignee = GetEmployeeOrManager(coAssigneeSetting.Assignee);
        }
        
        if (setting.HasIndefiniteDeadline == true)
          task.HasIndefiniteDeadline = true;
        else
        {
          task.Deadline = scheduleItem.Deadline;
          if (task.CoAssignees.Any())
            task.CoAssigneesDeadline = scheduleItem.Deadline.Value.AddWorkingDays(-coAssigneesDeadlineOffset).Date;
        }
      }
      
      task.Save();
      task.Start();
      
      scheduleItem.ActionItemExecutionTask = task;
      scheduleItem.Save();
      
      // Если у графика нет даты окончания и нет других действующих записей расписания без заполненного поручения, то отправить уведомление администратору об актуализации графика.
      var endDate = setting.EndDate ?? setting.EndMonth ?? setting.EndYear;
      if (!endDate.HasValue && !ScheduleItems.GetAll(si => Equals(si.RepeatSetting, setting) &&
                                                     si.ActionItemExecutionTask == null &&
                                                     si.Status == DirRX.PeriodicActionItemsTemplate.ScheduleItem.Status.Active).Any())
      {
        var responsible = Recipients.Null;
        
        var assistant = Sungero.Company.PublicFunctions.Module.Remote.GetResolutionPreparers().Where(x => Equals(x.Manager, setting.AssignedBy)).FirstOrDefault();
        if (assistant != null)
          responsible = assistant.Assistant;
        else
          responsible = setting.AssignedBy.Status == Sungero.Company.Employee.Status.Active ? Recipients.As(setting.AssignedBy) : Functions.Module.GetResponsibleForPeriodic();
        var notice = Sungero.Workflow.SimpleTasks.CreateWithNotices(Resources.LastActionItemText, responsible);
        notice.Attachments.Add(setting);
        notice.Start();
      }
      
      // Отправка уведомления инициатору о запуске очередного периодического поручения по графику. 
      if (setting.NotifyInitiator == true)
      {
        var subject = DirRX.PeriodicActionItemsTemplate.Resources.InitiatorNotificationSubjectFormat(setting.DisplayValue);
        var notice = Sungero.Workflow.SimpleTasks.CreateWithNotices(subject, setting.AssignedBy);
        notice.Attachments.Add(task);
        notice.Attachments.Add(setting);
        notice.Start();
      }    
    }
    
    /// <summary>
    /// Создать запись расписания отправки для ежедневного поручения.
    /// </summary>
    /// <param name="schedule">График.</param>
    /// <param name="maxDate">Максимально возможная дата отправки (без времени).</param>
    /// <param name="checkDuplicates">Проверять наличие дублей записей расписания при создании.</param>
    public virtual void CreateScheduleItemsForDailySchedule(IRepeatSetting schedule, DateTime maxDate, bool checkDuplicates)
    {
      var iterationDate = schedule.BeginningDate.Value.Date;
      
      while (iterationDate <= maxDate)
      {
        if (!checkDuplicates || !HasScheduleItemDuplicates(schedule, iterationDate, schedule.HasIndefiniteDeadline != true ? (DateTime?)iterationDate : null))
        {
          CreateScheduleItem(schedule, iterationDate.Date, iterationDate.Date);
        }
        
        try
        {
          iterationDate = iterationDate.AddWorkingDays(schedule.RepeatValue ?? 1).Date;
        }
        catch
        {
          break;
        }
      }
    }
    
    /// <summary>
    /// Создать запись расписания отправки для еженедельного поручения.
    /// </summary>
    /// <param name="schedule">График.</param>
    /// <param name="maxDate">Максимально возможная дата отправки (без времени).</param>
    /// <param name="checkDuplicates">Проверять наличие дублей записей расписания при создании.</param>
    public virtual void CreateScheduleItemsForWeeklySchedule(IRepeatSetting schedule, DateTime maxDate, bool checkDuplicates)
    {
      var iterationDate = schedule.BeginningDate.Value.BeginningOfWeek().Date;
      var maxDateStartWeek = maxDate.BeginningOfWeek().Date;
      
      var daysOfWeek = new List<int>();
      
      if (schedule.WeekTypeMonday == true)
        daysOfWeek.Add(0);
      if (schedule.WeekTypeTuesday == true)
        daysOfWeek.Add(1);
      if (schedule.WeekTypeWednesday == true)
        daysOfWeek.Add(2);
      if (schedule.WeekTypeThursday == true)
        daysOfWeek.Add(3);
      if (schedule.WeekTypeFriday == true)
        daysOfWeek.Add(4);
      
      while (iterationDate <= maxDateStartWeek)
      {
        foreach (var dayOfWeek in daysOfWeek)
        {
          var iterationDateWithWeekDay = iterationDate.AddDays(dayOfWeek);
          var deadline = TransferDateFromHolidays(iterationDateWithWeekDay, schedule.TransferFromHoliday).Date;
          var startDate = deadline.AddWorkingDays(-(schedule.CreationDays ?? 0)).Date;
          if (deadline < schedule.BeginningDate.Value.Date)
            continue;
          if (deadline <= maxDate || schedule.HasIndefiniteDeadline == true)
          {
            if (!checkDuplicates || !HasScheduleItemDuplicates(schedule, startDate, schedule.HasIndefiniteDeadline != true ? (DateTime?)deadline : null))
            {
              CreateScheduleItem(schedule, startDate, deadline);
            }
          }
        }
        
        try
        {
          iterationDate = iterationDate.AddDays((schedule.RepeatValue ?? 1) * 7).Date;
        }
        catch
        {
          break;
        }
      }
    }
    
    /// <summary>
    /// Создать запись расписания отправки для ежемесячного поручения.
    /// </summary>
    /// <param name="schedule">График.</param>
    /// <param name="maxDate">Максимально возможная дата отправки (без времени).</param>
    /// <param name="checkDuplicates">Проверять наличие дублей записей расписания при создании.</param>
    public virtual void CreateScheduleItemsForMonthlySchedule(IRepeatSetting schedule, DateTime maxDate, bool checkDuplicates)
    {
      var daysOfWeek = new Dictionary<Enumeration?, DayOfWeek>(5)
      {
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeek.Monday, DayOfWeek.Monday },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeek.Tuesday, DayOfWeek.Tuesday },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeek.Wednesday, DayOfWeek.Wednesday },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeek.Thursday, DayOfWeek.Thursday },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeek.Friday, DayOfWeek.Friday }
      };
      
      var weeksNumbers = new Dictionary<Enumeration?, int>(5)
      {
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeekNumber.First, 1 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeekNumber.Second, 2 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeekNumber.Third, 3 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeekNumber.Fourth, 4 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeekNumber.Last, 1 }
      };
      
      
      var iterationDate = schedule.BeginningMonth.Value.BeginningOfMonth().Date;
      var maxDateStartMonth = maxDate.BeginningOfMonth().Date;
      
      while (iterationDate <= maxDateStartMonth)
      {
        if (schedule.MonthTypeDay == DirRX.PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDay.Date)
        {
          var monthDay = (schedule.MonthTypeDayValue ?? 1) > iterationDate.EndOfMonth().Day ? iterationDate.EndOfMonth().Day : (schedule.MonthTypeDayValue ?? 1);
          var deadline = TransferDateFromHolidays(Calendar.GetDate(iterationDate.Year, iterationDate.Month, monthDay), schedule.TransferFromHoliday).Date;
          var startDate = deadline.AddWorkingDays(-(schedule.CreationDays ?? 0)).Date;
          if (deadline < schedule.BeginningMonth.Value.Date)
          {
            iterationDate = iterationDate.AddMonths(schedule.RepeatValue ?? 1).Date;
            continue;
          }
          
          if (deadline <= maxDate || schedule.HasIndefiniteDeadline == true)
          {
            if (!checkDuplicates || !HasScheduleItemDuplicates(schedule, startDate, schedule.HasIndefiniteDeadline != true ? (DateTime?)deadline : null))
            {
              CreateScheduleItem(schedule, startDate, deadline);
            }
          }
        }
        
        if (schedule.MonthTypeDay == DirRX.PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDay.DayOfWeek)
        {
          var deadline = TransferDateFromHolidays(GetNthWeekDayOfMonth(iterationDate.Year, iterationDate.Month,
                                                                       daysOfWeek[schedule.MonthTypeDayOfWeek],
                                                                       weeksNumbers[schedule.MonthTypeDayOfWeekNumber],
                                                                       schedule.MonthTypeDayOfWeekNumber != DirRX.PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeekNumber.Last),
                                                  schedule.TransferFromHoliday).Date;
          var startDate = deadline.AddWorkingDays(-(schedule.CreationDays ?? 0)).Date;
          if (deadline < schedule.BeginningMonth.Value.Date)
          {
            iterationDate = iterationDate.AddMonths(schedule.RepeatValue ?? 1).Date;
            continue;
          }
          if (deadline <= maxDate || schedule.HasIndefiniteDeadline == true)
          {
            if (!checkDuplicates || !HasScheduleItemDuplicates(schedule, startDate, schedule.HasIndefiniteDeadline != true ? (DateTime?)deadline : null))
            {
              CreateScheduleItem(schedule, startDate, deadline);
            }
          }
        }
        
        try
        {
          iterationDate = iterationDate.AddMonths(schedule.RepeatValue ?? 1).Date;
        }
        catch
        {
          break;
        }
      }
    }
    
    /// <summary>
    /// Создать запись расписания отправки для ежегодного поручения.
    /// </summary>
    /// <param name="schedule">График.</param>
    /// <param name="maxDate">Максимально возможная дата отправки (без времени).</param>
    /// <param name="checkDuplicates">Проверять наличие дублей записей расписания при создании.</param>
    public virtual void CreateScheduleItemsForAnnualSchedule(IRepeatSetting schedule, DateTime maxDate, bool checkDuplicates)
    {
      var monthsNumbers = new Dictionary<Enumeration?, int>(12)
      {
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.January, 1 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.February, 2 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.March, 3 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.April, 4 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.May, 5 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.June, 6 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.July, 7 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.August, 8 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.September, 9 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.October, 10 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.November, 11 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.December, 12 }
      };
      
      var weeksNumbers = new Dictionary<Enumeration?, int>(5)
      {
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeekNumber.First, 1 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeekNumber.Second, 2 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeekNumber.Third, 3 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeekNumber.Fourth, 4 },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeekNumber.Last, 1 }
      };
      
      var daysOfWeek = new Dictionary<Enumeration?, DayOfWeek>(5)
      {
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeek.Monday, DayOfWeek.Monday },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeek.Tuesday, DayOfWeek.Tuesday },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeek.Wednesday, DayOfWeek.Wednesday },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeek.Thursday, DayOfWeek.Thursday },
        { DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeek.Friday, DayOfWeek.Friday }
      };
      
      var iterationDate = schedule.BeginningYear.Value.BeginningOfYear().Date;
      var maxDateStartYear = maxDate.BeginningOfYear().Date;
      
      while (iterationDate <= maxDateStartYear)
      {
        if (schedule.YearTypeDay == DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeDay.Date)
        {
          var month = Calendar.GetDate(iterationDate.Year, monthsNumbers[schedule.YearTypeMonth], 1).Date;
          var monthDay = (schedule.YearTypeDayValue ?? 1) > month.EndOfMonth().Day ? month.EndOfMonth().Day : (schedule.YearTypeDayValue ?? 1);
          var deadline = TransferDateFromHolidays(Calendar.GetDate(iterationDate.Year, monthsNumbers[schedule.YearTypeMonth], monthDay), schedule.TransferFromHoliday).Date;
          var startDate = deadline.AddWorkingDays(-(schedule.CreationDays ?? 0)).Date;
          if (deadline < schedule.BeginningYear.Value.Date)
          {
            iterationDate = iterationDate.AddYears(schedule.RepeatValue ?? 1).Date;
            continue;
          }
          if (deadline <= maxDate || schedule.HasIndefiniteDeadline == true)
          {
            if (!checkDuplicates || !HasScheduleItemDuplicates(schedule, startDate, schedule.HasIndefiniteDeadline != true ? (DateTime?)deadline : null))
            {
              CreateScheduleItem(schedule, startDate, deadline);
            }
          }
        }
        
        if (schedule.YearTypeDay == DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeDay.DayOfWeek)
        {
          var deadline = TransferDateFromHolidays(GetNthWeekDayOfMonth(iterationDate.Year, monthsNumbers[schedule.YearTypeMonth],
                                                                       daysOfWeek[schedule.YearTypeDayOfWeek],
                                                                       weeksNumbers[schedule.YearTypeDayOfWeekNumber],
                                                                       schedule.YearTypeDayOfWeekNumber != DirRX.PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeekNumber.Last),
                                                  schedule.TransferFromHoliday).Date;
          var startDate = deadline.AddWorkingDays(-(schedule.CreationDays ?? 0)).Date;
          if (deadline < schedule.BeginningYear.Value.Date)
          {
            iterationDate = iterationDate.AddYears(schedule.RepeatValue ?? 1).Date;
            continue;
          }
          if (deadline <= maxDate || schedule.HasIndefiniteDeadline == true)
          {
            if (!checkDuplicates || !HasScheduleItemDuplicates(schedule, startDate, schedule.HasIndefiniteDeadline != true ? (DateTime?)deadline : null))
            {
              CreateScheduleItem(schedule, startDate, deadline);
            }
          }
        }
        
        try
        {
          iterationDate = iterationDate.AddYears(schedule.RepeatValue ?? 1).Date;
        }
        catch
        {
          break;
        }
      }
    }
    
    /// <summary>
    /// Получить n-ный день недели в месяце.
    /// </summary>
    /// <param name="year">Год.</param>
    /// <param name="month">Месяц.</param>
    /// <param name="dayOfWeek">День недели.</param>
    /// <param name="nthWeek">Номер дня недели в месяце (от 1 до 4).</param>
    /// <param name="fromBeginning">Считать с начала месяца (при значении false отсчет идет с конца).</param>
    /// <returns></returns>
    public virtual DateTime GetNthWeekDayOfMonth(int year, int month, System.DayOfWeek dayOfWeek, int nthWeek, bool fromBeginning = true)
    {
      var lastDay = 0;
      if (fromBeginning)
      {
        lastDay = 7 * nthWeek;
      }
      else
      {
        lastDay = Calendar.GetDate(year, month, 1).AddMonths(1).AddDays(-1).Day - (7 * (nthWeek - 1));
      }
      
      var sundayDayOfMonth = lastDay - (int)dayOfWeek;
      
      var offset = (int)(Calendar.GetDate(year, month, sundayDayOfMonth).DayOfWeek);
      
      return Calendar.GetDate(year, month, lastDay - offset).Date;
    }
    
    /// <summary>
    /// Перенести дату с выходного согласно направлению переноса.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <param name="transferDirection">Направление переноса (Нет/Вперед/Назад).</param>
    /// <returns>Дата, перенесенная с выходных.</returns>
    public virtual DateTime TransferDateFromHolidays(DateTime date, Enumeration? transferDirection)
    {
      // try на случай, если date - это последний день последнего календаря, а перенести мы должны вперед.
      try
      {
        if (!date.IsWorkingDay())
        {
          if (transferDirection == DirRX.PeriodicActionItemsTemplate.RepeatSetting.TransferFromHoliday.Back)
            return date.PreviousWorkingDay().Date;
          
          if (transferDirection == DirRX.PeriodicActionItemsTemplate.RepeatSetting.TransferFromHoliday.Ahead)
            return date.NextWorkingDay().Date;
        }
      }
      catch
      {
        return date;
      }
      
      return date;
    }
    
    /// <summary>
    /// Проверить наличие дублей записей расписания для указанных параметров.
    /// </summary>
    /// <param name="setting">График отправки.</param>
    /// <param name="startDate">Дата отправки.</param>
    /// <param name="deadline">Срок (при наличии).</param>
    /// <returns>True, если есть дубли, иначе - false.</returns>
    public virtual bool HasScheduleItemDuplicates(IRepeatSetting setting, DateTime startDate, DateTime? deadline)
    {
      if (deadline.HasValue)
        return ScheduleItems.GetAll(si => Equals(si.RepeatSetting, setting) &&
                                    si.StartDate == startDate &&
                                    si.Deadline == deadline &&
                                    si.Status == DirRX.PeriodicActionItemsTemplate.ScheduleItem.Status.Active).Any();
      return ScheduleItems.GetAll(si => Equals(si.RepeatSetting, setting) &&
                                  si.StartDate == startDate &&
                                  si.HasIndefiniteDeadline == true &&
                                  si.Status == DirRX.PeriodicActionItemsTemplate.ScheduleItem.Status.Active).Any();
    }
    
    /// <summary>
    /// Создать запись расписания отправки.
    /// </summary>
    /// <param name="schedule">График.</param>
    /// <param name="startDate">Дата старта.</param>
    /// <param name="deadline">Срок исполнения.</param>
    /// <remarks>Срок исполнения не заполняется, если в графике установлен чекбокс "Без срока".</remarks>
    public virtual void CreateScheduleItem(IRepeatSetting schedule, DateTime? startDate, DateTime? deadline)
    {
      var scheduleItem = ScheduleItems.Create();
      
      scheduleItem.RepeatSetting = schedule;
      
      scheduleItem.StartDate = startDate;
      
      scheduleItem.HasIndefiniteDeadline = schedule.HasIndefiniteDeadline == true;
      
      if (scheduleItem.HasIndefiniteDeadline != true)
        scheduleItem.Deadline = deadline;
      
      scheduleItem.Save();
    }
  }
}