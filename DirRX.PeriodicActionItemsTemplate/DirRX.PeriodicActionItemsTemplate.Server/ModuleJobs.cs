using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Metadata.Services;
using Sungero.Domain.Shared;

namespace DirRX.PeriodicActionItemsTemplate.Server
{
  public class ModuleJobs
  {
    
    #region Скопировано из разработки платформы.
    /// <summary>
    /// Удалить старые ссылки на сущности из папки.
    /// </summary>
    /// <param name="specialFolderId">Идентификатор спецпапки.</param>
    /// <param name="folderContentTypeInfo">Инфошка типа содержимого спецпапки.</param>
    /// <param name="itemsFilter">Фильтр сущностей.</param>
    /// <param name="overdueDate">Дата начиная с которой ссылка считается старой.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Формирование запроса безопасное.")]
    private static void DeleteOldLinksFromFolder(Guid specialFolderId, IEntityInfo folderContentTypeInfo, string itemsFilter, DateTime overdueDate)
    {
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = string.Format(DirRX.ActionItems.Queries.Module.DeleteMarkedFolderItems, folderContentTypeInfo.DBTableName, specialFolderId, itemsFilter);
        SQL.AddParameter(command, "@date", overdueDate, DbType.DateTime);
        command.ExecuteNonQuery();
      }
      
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = string.Format(DirRX.ActionItems.Queries.Module.DeleteOldLinksFromFolder, folderContentTypeInfo.DBTableName, specialFolderId, itemsFilter);
        SQL.AddParameter(command, "@date", overdueDate, DbType.DateTime);
        command.ExecuteNonQuery();
      }
    }

    /// <summary>
    /// Выполнить очистку тегов спецпапки.
    /// </summary>
    /// <param name="specialFolderId">Идентификатор спецпапки.</param>
    /// <param name="folderContentTypeInfo">Инфошка типа содержимого спецпапки.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Формирование запроса безопасное.")]
    private static void CleanSpecialFolderTags(Guid specialFolderId, IEntityInfo folderContentTypeInfo)
    {
      var childFolders = MetadataService.Instance.AllSpecialFolders.Where(sf => sf.ParentFolderId == specialFolderId).ToList();
      if (!childFolders.Any())
        return;

      var childFoldersString = string.Join(", ", childFolders.Select(f => string.Format("'{0}'", f.NameGuid)));
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = string.Format(DirRX.ActionItems.Queries.Module.CleanSpecialFolderTags, folderContentTypeInfo.DBTableName, childFoldersString, specialFolderId);
        command.ExecuteNonQuery();
      }
    }
    
    /// <summary>
    /// Проверка заблокированности рисков задачи.
    /// </summary>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>True - есть заблокированные риски. False - нет заблокированных рисков.</returns>
    private bool CheckLockedRisk(Solution.IApprovalTask task)
    {
      foreach (var risk in task.RiskAttachmentGroup.Risks)
      {
        if (Locks.GetLockInfo(risk).IsLocked)
          return true;
      }
      return false;
    }
    
    private void SetResult(bool generate, ActionItems.ISendNoticeQueueItem queueItem)
    {
      // При выполнении операции удаляем элемент очереди, иначе увеличиваем счётчик попыток и записываем ошибку.
      if (generate)
      {
        Transactions.Execute(
          () =>
          {
            DirRX.ActionItems.SendNoticeQueueItems.Delete(queueItem);
          });
      }
      else
      {
        Transactions.Execute(
          () =>
          {
            Sungero.ExchangeCore.PublicFunctions.QueueItemBase.QueueItemOnError(queueItem, DirRX.ActionItems.Resources.AddSubscriberInJobError);
          });
        if (queueItem.Assignment != null)
          Logger.DebugFormat("{0} Id = '{1}'.", DirRX.ActionItems.Resources.AddSubscriberInJobError, queueItem.Assignment.Id);
        
        if (queueItem.Task != null)
          Logger.DebugFormat("{0} Id = '{1}'.", DirRX.ActionItems.Resources.AddSubscriberInJobError, queueItem.Task.Id);
      }
    }
    
    /// <summary>
    /// Отправка уведомления.
    /// </summary>
    /// <param name="queueItem">Элемент очереди.</param>
    private void SendNotice(ActionItems.ISendNoticeQueueItem queueItem)
    {
      var notice = Sungero.Workflow.SimpleTasks.Null;
      if (queueItem.SendAsSubTask.GetValueOrDefault() && queueItem.Task != null)
        notice = Sungero.Workflow.SimpleTasks.CreateAsSubtask(queueItem.Task);
      else
        notice = Sungero.Workflow.SimpleTasks.Create();

      var task = Sungero.Workflow.Tasks.Null;
      
      if (queueItem.Task != null)
        task = queueItem.Task;

      if (queueItem.Assignment != null)
        task = queueItem.Assignment.Task;
      
      notice.Attachments.Add(task);
      notice.Subject = string.Format(queueItem.Subject);
      notice.NeedsReview = false;

      foreach (var performer in queueItem.Assignees)
      {
        if (performer.Subscriber == null)
          continue;
        var routeStep = notice.RouteSteps.AddNew();
        routeStep.AssignmentType = Sungero.Workflow.SimpleTaskRouteSteps.AssignmentType.Notice;
        routeStep.Performer = performer.Subscriber;
        routeStep.Deadline = null;
        
        if (task != null)
        {
          task.AccessRights.Grant(performer.Subscriber, DefaultAccessRightsTypes.Read);
          task.AccessRights.Save();
        }
      }
      if (notice.RouteSteps.Count == 0)
        return;
      
      if (notice.Subject.Length > Sungero.Workflow.Tasks.Info.Properties.Subject.Length)
        notice.Subject = notice.Subject.Substring(0, Sungero.Workflow.Tasks.Info.Properties.Subject.Length);
      notice.Start();
      

    }
    
    #endregion

    #region Уведомления.

    /// <summary>
    /// Проверить время в работе в процентах.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="percent">Процент.</param>
    /// <returns>True, если прошло указанное количество времени в процентах.</returns>
    public static bool IsCorrectPercent(DirRX.Solution.IActionItemExecutionTask task, double percent)
    {
      var deadline = Sungero.Docflow.PublicFunctions.Module.GetDateWithTime(task.Deadline.Value, task.Initiator);
      var allHours = WorkingTime.GetDurationInWorkingHours(task.Started.Value, deadline, task.Initiator);
      var hoursLeft = WorkingTime.GetDurationInWorkingHours(Calendar.Now, deadline, task.Initiator);
      var leftPercent = (double)hoursLeft / allHours;

      Logger.DebugFormat("Is correct for {0} percent = {1}. All hours = {2}. Hours left = {3}. Left percent = {4}",
                         percent, leftPercent < percent, allHours, hoursLeft, leftPercent);
      
      return leftPercent < percent;
    }
    
    /// <summary>
    /// Проверить на наличие уведомлений.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="previousRunDate">Время предыдущего запуска.</param>
    /// <param name="percent">Процент.</param>
    /// <returns>True, если уведомление отправленно ранее.</returns>
    public static bool IsSend(DirRX.Solution.IActionItemExecutionTask task, DateTime previousRunDate, double percent)
    {
      var deadline = Sungero.Docflow.PublicFunctions.Module.GetDateWithTime(task.Deadline.Value, task.Initiator);
      var allHours = WorkingTime.GetDurationInWorkingHours(task.Started.Value, deadline, task.Initiator);
      var previousRunHours = WorkingTime.GetDurationInWorkingHours(previousRunDate, deadline, task.Initiator);
      var previousRunPercent = (double)previousRunHours / allHours;
      
      Logger.DebugFormat("Is send for {0} percent = {1}. All hours = {2}. Previous run hours = {3}. previous run percent = {4}",
                         percent, previousRunPercent < percent, allHours, previousRunHours, previousRunPercent);
      
      return  previousRunPercent < percent;
    }
    
    #region Скопировано из стандартной разработки рассылки о новых заданиях.

    /// <summary>
    /// Получить дату последней рассылки уведомлений.
    /// </summary>
    /// <returns>Дата последней рассылки.</returns>
    public static DateTime GetLastNotificationDate()
    {
      var key = Constants.Module.LastNotificationDateTimeDocflowParamName;
      var command = string.Format(Queries.Module.SelectDocflowParamsValue, key);
      try
      {
        var executionResult = Sungero.Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
        var date = string.Empty;
        if (!(executionResult is DBNull) && executionResult != null)
          date = executionResult.ToString();
        Logger.DebugFormat("Last notification by assignment date in DB is {0} (UTC)", date);
        
        DateTime result = Calendar.FromUtcTime(DateTime.Parse(date, null, System.Globalization.DateTimeStyles.AdjustToUniversal));

        if ((result - Calendar.Now).TotalDays > 1)
          return Calendar.Today;
        else
          return result;
      }
      catch (Exception ex)
      {
        Logger.Error("Error while getting last notification by assignment date", ex);
        return Calendar.Today;
      }
    }
    
    /// <summary>
    /// Обновить дату последней рассылки уведомлений.
    /// </summary>
    /// <param name="notificationDate">Дата рассылки уведомлений.</param>
    public static void UpdateLastNotificationDate(DateTime notificationDate)
    {
      var key = Constants.Module.LastNotificationDateTimeDocflowParamName;
      
      var newDate = notificationDate.Add(-Calendar.UtcOffset).ToString("yyyy-MM-ddTHH:mm:ss.ffff+0");
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.Module.InsertOrUpdateDocflowParamsValue, new[] { key, newDate });
      Logger.DebugFormat("Last notification by assignment date is set to {0} (UTC)", newDate);
    }
    
    #endregion
    
    #region Повторяющиеся поручения.
    
    /// <summary>
    /// Отправление повторяющихся поручений.
    /// </summary>
    public virtual void RepeatActionItemExecutionTasks()
    {
      var repeatSettings = DirRX.ActionItems.RepeatSettings.GetAll(s => s.Status == DirRX.ActionItems.RepeatSetting.Status.Active);
      var createdTask = DirRX.Solution.ActionItemExecutionTasks.GetAll(t => t.Started.HasValue && t.Started.Value.Date == Calendar.Today.Date);
      
      foreach (var setting in repeatSettings)
      {
        if (setting.Type == null || setting.CreationDays == null)
          continue;
        
        if (createdTask.Any(t => DirRX.ActionItems.RepeatSettings.Equals(t.RepeatSetting, setting)))
        {
          Logger.DebugFormat("Action item sent.");
          continue;
        }
        
        Logger.Debug(string.Format("Setting with id = {0} processed. Type = {1}", setting.Id, setting.Type.Value.Value));
        var date = Calendar.Today;
        var deadlineDate = Calendar.Today.AddWorkingDays(setting.Initiator, setting.CreationDays.Value);
        
        #region Ежегодно.
        
        if (setting.Type == DirRX.ActionItems.RepeatSetting.Type.Year)
        {
          var beginningDate = setting.BeginningYear.Value;
          var endDate = setting.EndYear.HasValue ? setting.EndYear.Value : Calendar.SqlMaxValue;
          
          if (!Calendar.Between(deadlineDate, beginningDate, endDate))
          {
            Logger.DebugFormat("Date misses the period. Deadline = {0}. Settings: begin = {1}, end = {2}", deadlineDate.Year, beginningDate.Year, endDate.Year);
            continue;
          }
          
          var period = setting.RepeatValue.HasValue ? setting.RepeatValue.Value : 1;
          
          // Проверка соответствия года.
          if (!IsCorrectYear(period, beginningDate.Year, deadlineDate.Year))
          {
            Logger.DebugFormat("Incorrect year. Current = {0}. Settings: begin = {1}, period = {2}", deadlineDate.Year, beginningDate.Year, period);
            continue;
          }
          
          // Проверка соответствия месяца.
          var month = GetMonthValue(setting.YearTypeMonth.GetValueOrDefault());
          if (deadlineDate.Month != month)
          {
            Logger.DebugFormat("Incorrect month. Current = {0}. In setting = {1}", deadlineDate.Month, month);
            continue;
          }
          
          if (setting.YearTypeDay == DirRX.ActionItems.RepeatSetting.YearTypeDay.Date)
          {
            try
            {
              date = Calendar.GetDate(deadlineDate.Year, month, setting.YearTypeDayValue.GetValueOrDefault());
              if (!date.IsWorkingDay(setting.Initiator))
                date = date.PreviousWorkingDay(setting.Initiator);
              
              if (deadlineDate.Day == date.Day)
              {
                SendActionItem(setting, deadlineDate);
                
                if (setting.EndYear.HasValue && deadlineDate.AddYears(period).Year > setting.EndYear.Value.Year)
                  SendNotice(setting);
              }
              else
                Logger.DebugFormat("Incorrect day. Current = {0}. Is setting (working day) = {1}", deadlineDate.Day, date.Day);
            }
            catch
            {
              Logger.ErrorFormat("Incorrect data for date. Year = {0}, month = {1}, day = {2}",
                                 deadlineDate.Year, month, setting.YearTypeDayValue.GetValueOrDefault());
            }
          }
          else
          {
            try
            {
              var beginningMonth = Calendar.GetDate(deadlineDate.Year, month, 1);
              date = GetDateTime(setting.YearTypeDayOfWeek.Value, setting.YearTypeDayOfWeekNumber.Value, beginningMonth);
              if (!date.IsWorkingDay(setting.Initiator))
                date = date.PreviousWorkingDay(setting.Initiator);
              
              if (date.Date == deadlineDate.Date)
              {
                SendActionItem(setting, deadlineDate);
                
                if (setting.EndYear.HasValue && deadlineDate.AddYears(period).Year > setting.EndYear.Value.Year)
                  SendNotice(setting);
              }
              else
                Logger.DebugFormat("Incorrect day. Current = {0}. Settings: Day of week = {1}, week number = {2}. working day = {3}",
                                   deadlineDate.Day, setting.YearTypeDayOfWeek.Value.Value, setting.YearTypeDayOfWeekNumber.Value.Value, date.Day);
            }
            catch
            {
              Logger.ErrorFormat("Incorrect data for date. Year = {0}, month = {1}, day of week = {2}, week number = {3}",
                                 deadlineDate.Year, month, setting.YearTypeDayOfWeek.Value.Value, setting.YearTypeDayOfWeekNumber.Value.Value);
            }
          }
        }
        
        #endregion
        
        #region Ежемесячно.
        
        if (setting.Type == DirRX.ActionItems.RepeatSetting.Type.Month)
        {
          var beginningDate = setting.BeginningMonth.Value;
          var period = setting.RepeatValue.HasValue ? setting.RepeatValue.Value : 1;
          var endDate = setting.EndMonth.HasValue ? setting.EndMonth.Value.EndOfMonth() : Calendar.SqlMaxValue;
          
          if (!Calendar.Between(deadlineDate, beginningDate, endDate))
          {
            Logger.DebugFormat("Date misses the period. Deadline = {0}. Settings: begin = {1}, end = {2}", deadlineDate, beginningDate, endDate);
            continue;
          }
          
          // Проверка соответствия месяца. Если year = null пропускаем вычисления.
          if (!IsCorrectMonth(period, beginningDate, deadlineDate))
          {
            Logger.DebugFormat("Incorrect month. Current = {0}. Settings: begin = {1}, period = {2}", deadlineDate.Month, beginningDate, period);
            continue;
          }
          
          if (setting.MonthTypeDay == DirRX.ActionItems.RepeatSetting.MonthTypeDay.Date)
          {
            try
            {
              var day = setting.MonthTypeDayValue.GetValueOrDefault();
              var dateString = string.Format("{0}/{1}/{2}", day.ToString(), deadlineDate.Month.ToString(), deadlineDate.Year.ToString());
              
              // Обработка несуществующего числа в месяце (например 30 число в феврале).
              while (!Calendar.TryParseDate(dateString, out date))
              {
                Logger.DebugFormat("Incorrect date. String = {0}", dateString);
                day--;
                dateString = string.Format("{0}/{1}/{2}", day.ToString(), deadlineDate.Month.ToString(), deadlineDate.Year.ToString());
              }
              
              if (!date.IsWorkingDay(setting.Initiator))
                date = date.PreviousWorkingDay(setting.Initiator);
              
              if (deadlineDate.Day == date.Day)
              {
                SendActionItem(setting, deadlineDate);
                
                var nextDeadline = deadlineDate.AddMonths(period);
                if (setting.EndMonth.HasValue && nextDeadline.EndOfMonth() > setting.EndMonth.Value)
                  SendNotice(setting);
              }
              else
                Logger.DebugFormat("Incorrect day. Current = {0}. Is setting (working day) = {1}", deadlineDate.Day, date.Day);
            }
            catch
            {
              Logger.ErrorFormat("Incorrect data for date. Year = {0}, month = {1}, day = {2}",
                                 deadlineDate.Year, deadlineDate.Month, setting.MonthTypeDayValue.GetValueOrDefault());
            }
          }
          else
          {
            try
            {
              var beginningMonth = Calendar.GetDate(deadlineDate.Year, deadlineDate.Month, 1);
              date = GetDateTime(setting.MonthTypeDayOfWeek.Value, setting.MonthTypeDayOfWeekNumber.Value, beginningMonth);
              if (!date.IsWorkingDay(setting.Initiator))
                date = date.PreviousWorkingDay(setting.Initiator);
              
              if (date.Date == deadlineDate.Date)
              {
                SendActionItem(setting, deadlineDate);
                
                var nextDeadline = deadlineDate.AddMonths(period);
                if (setting.EndMonth.HasValue && nextDeadline.EndOfMonth() > setting.EndMonth.Value)
                  SendNotice(setting);
              }
              else
                Logger.DebugFormat("Incorrect day. Current = {0}. Settings: Day of week = {1}, week number = {2}. working day = {3}",
                                   deadlineDate.Day, setting.MonthTypeDayOfWeek.Value.Value, setting.MonthTypeDayOfWeekNumber.Value.Value, date.Day);
            }
            catch
            {
              Logger.ErrorFormat("Incorrect data for date. Year = {0}, month = {1}, day of week = {2}, week number = {3}",
                                 deadlineDate.Year, deadlineDate.Month, setting.MonthTypeDayOfWeek.Value.Value, setting.MonthTypeDayOfWeekNumber.Value.Value);
            }
          }
        }
        
        #endregion
        
        #region Еженедельно.
        
        if (setting.Type == DirRX.ActionItems.RepeatSetting.Type.Week)
        {
          var beginningDate = setting.BeginningDate.Value;
          var endDate = setting.EndDate.HasValue ? setting.EndDate.Value.EndOfDay() : Calendar.SqlMaxValue;
          
          if (!Calendar.Between(deadlineDate, beginningDate, endDate))
          {
            Logger.DebugFormat("Date misses the period. Deadline = {0}. Settings: begin = {1}, end = {2}", deadlineDate, beginningDate, endDate);
            continue;
          }
          
          var daysOfWeek = new List<DayOfWeek>();
          
          if (setting.WeekTypeMonday.Value)
            daysOfWeek.Add(DayOfWeek.Monday);
          if (setting.WeekTypeTuesday.Value)
            daysOfWeek.Add(DayOfWeek.Tuesday);
          if (setting.WeekTypeWednesday.Value)
            daysOfWeek.Add(DayOfWeek.Wednesday);
          if (setting.WeekTypeThursday.Value)
            daysOfWeek.Add(DayOfWeek.Thursday);
          if (setting.WeekTypeFriday.Value)
            daysOfWeek.Add(DayOfWeek.Friday);
          
          // Вычисляем количество дней между неделями и вычитаем 6, чтобы учесть первую неделю периода.
          // Если количество дней целочисленно делится на период, то неделя в него попадает.
          TimeSpan timeSpan = deadlineDate.EndOfWeek() - beginningDate.BeginningOfWeek();
          var daysCount = timeSpan.Days - 6;
          var periodDays = 7 * (setting.RepeatValue.HasValue ? setting.RepeatValue.Value : 1);
          
          if (daysCount % periodDays == 0 && daysOfWeek.Contains(deadlineDate.DayOfWeek))
          {
            SendActionItem(setting, deadlineDate);
            
            if (setting.EndDate.HasValue && !daysOfWeek.Any(d => d > deadlineDate.DayOfWeek) &&
                deadlineDate.EndOfWeek().AddDays(periodDays) > setting.EndDate.Value)
              SendNotice(setting);
          }
        }
        
        #endregion
        
        #region Ежедневно.
        
        if (setting.Type == DirRX.ActionItems.RepeatSetting.Type.Day)
        {
          var beginningDate = setting.BeginningDate.Value;
          var endDate = setting.EndDate.HasValue ? setting.EndDate.Value.EndOfDay() : Calendar.SqlMaxValue;
          
          if (!Calendar.Between(deadlineDate, beginningDate, endDate))
          {
            Logger.DebugFormat("Date misses the period. Deadline = {0}. Settings: begin = {1}, end = {2}", deadlineDate, beginningDate, endDate);
            continue;
          }
          
          var periodValue = setting.RepeatValue.HasValue ? setting.RepeatValue.Value : 1;
          if (periodValue == 1)
          {
            SendActionItem(setting, deadlineDate);
            
            if (setting.EndDate.HasValue && deadlineDate.AddWorkingDays(setting.Initiator, periodValue) > setting.EndDate.Value)
              SendNotice(setting);
          }
          else
          {
            // Прибавляем 1, чтобы учесть первый день в периоде.
            var daysCount = WorkingTime.GetDurationInWorkingDays(beginningDate, deadlineDate, setting.Initiator) + 1;
            
            if (daysCount % periodValue == 0)
            {
              SendActionItem(setting, deadlineDate);
              
              if (setting.EndDate.HasValue && deadlineDate.AddWorkingDays(setting.Initiator, periodValue) > setting.EndDate.Value)
                SendNotice(setting);
            }
          }
        }
        
        #endregion
      }
    }
    
    /// <summary>
    /// Получить дату соответствующую настройкам.
    /// </summary>
    /// <param name="week">День недели.</param>
    /// <param name="weekNumber">Порядковый номер недели.</param>
    /// <param name="date">Дата, от которой происходит отсчёт.</param>
    /// <returns>Дата.</returns>
    private DateTime GetDateTime(Sungero.Core.Enumeration dayOfWeekSetting, Sungero.Core.Enumeration dayOfWeekNumberSetting, DateTime date)
    {
      var dayOfWeek = DayOfWeek.Monday;
      
      if (dayOfWeekSetting == DirRX.ActionItems.RepeatSetting.YearTypeDayOfWeek.Monday || dayOfWeekSetting == DirRX.ActionItems.RepeatSetting.MonthTypeDayOfWeek.Monday)
        dayOfWeek = DayOfWeek.Monday;
      if (dayOfWeekSetting == DirRX.ActionItems.RepeatSetting.YearTypeDayOfWeek.Tuesday || dayOfWeekSetting == DirRX.ActionItems.RepeatSetting.MonthTypeDayOfWeek.Tuesday)
        dayOfWeek = DayOfWeek.Tuesday;
      if (dayOfWeekSetting == DirRX.ActionItems.RepeatSetting.YearTypeDayOfWeek.Wednesday || dayOfWeekSetting == DirRX.ActionItems.RepeatSetting.MonthTypeDayOfWeek.Wednesday)
        dayOfWeek = DayOfWeek.Wednesday;
      if (dayOfWeekSetting == DirRX.ActionItems.RepeatSetting.YearTypeDayOfWeek.Thursday || dayOfWeekSetting == DirRX.ActionItems.RepeatSetting.MonthTypeDayOfWeek.Thursday)
        dayOfWeek = DayOfWeek.Thursday;
      if (dayOfWeekSetting == DirRX.ActionItems.RepeatSetting.YearTypeDayOfWeek.Friday || dayOfWeekSetting == DirRX.ActionItems.RepeatSetting.MonthTypeDayOfWeek.Friday)
        dayOfWeek = DayOfWeek.Friday;
      
      while (date.DayOfWeek != dayOfWeek)
        date = date.NextDay();
      
      var month = date.Month;
      if (dayOfWeekNumberSetting == DirRX.ActionItems.RepeatSetting.YearTypeDayOfWeekNumber.Last || dayOfWeekNumberSetting == DirRX.ActionItems.RepeatSetting.MonthTypeDayOfWeekNumber.Last)
      {
        while (date.AddDays(7).Month == month)
          date = date.AddDays(7);
      }
      else
      {
        if (dayOfWeekNumberSetting == DirRX.ActionItems.RepeatSetting.YearTypeDayOfWeekNumber.Second || dayOfWeekNumberSetting == DirRX.ActionItems.RepeatSetting.MonthTypeDayOfWeekNumber.Second)
          date = date.AddDays(7);
        if (dayOfWeekNumberSetting == DirRX.ActionItems.RepeatSetting.YearTypeDayOfWeekNumber.Third || dayOfWeekNumberSetting == DirRX.ActionItems.RepeatSetting.MonthTypeDayOfWeekNumber.Third)
          date = date.AddDays(14);
        if (dayOfWeekNumberSetting == DirRX.ActionItems.RepeatSetting.YearTypeDayOfWeekNumber.Fourth || dayOfWeekNumberSetting == DirRX.ActionItems.RepeatSetting.MonthTypeDayOfWeekNumber.Fourth)
          date = date.AddDays(21);
      }
      
      return date;
    }
    
    #endregion
    
    #region Ежегодно.
    
    /// <summary>
    /// Получить численное представление месяца.
    /// </summary>
    /// <param name="month">Месяц из перечисления настроек.</param>
    /// <returns>Число от 1 до 12.</returns>
    private int GetMonthValue(Sungero.Core.Enumeration month)
    {
      if (month == DirRX.ActionItems.RepeatSetting.YearTypeMonth.January)
        return 1;
      if (month == DirRX.ActionItems.RepeatSetting.YearTypeMonth.February)
        return 2;
      if (month == DirRX.ActionItems.RepeatSetting.YearTypeMonth.March)
        return 3;
      if (month == DirRX.ActionItems.RepeatSetting.YearTypeMonth.April)
        return 4;
      if (month == DirRX.ActionItems.RepeatSetting.YearTypeMonth.May)
        return 5;
      if (month == DirRX.ActionItems.RepeatSetting.YearTypeMonth.June)
        return 6;
      if (month == DirRX.ActionItems.RepeatSetting.YearTypeMonth.July)
        return 7;
      if (month == DirRX.ActionItems.RepeatSetting.YearTypeMonth.August)
        return 8;
      if (month == DirRX.ActionItems.RepeatSetting.YearTypeMonth.September)
        return 9;
      if (month == DirRX.ActionItems.RepeatSetting.YearTypeMonth.October)
        return 10;
      if (month == DirRX.ActionItems.RepeatSetting.YearTypeMonth.November)
        return 11;
      if (month == DirRX.ActionItems.RepeatSetting.YearTypeMonth.December)
        return 12;
      
      return 1;
    }
    
    /// <summary>
    /// Вычислить подходит ли год под период.
    /// </summary>
    /// <param name="period">Период.</param>
    /// <param name="beginningYear">Год от которого отсчитывается начало отправления поручений.</param>
    /// <param name="endYear">Год для вычисляемой даты.</param>
    /// <returns>True если вычисляемый год попадает в период.</returns>
    private bool IsCorrectYear(int period, int beginningYear, int endYear)
    {
      while (beginningYear <= endYear)
      {
        if (endYear == beginningYear)
          return true;
        
        beginningYear += period;
      }
      
      return false;
    }
    
    #endregion
    
    #region Ежемесячно.
    
    /// <summary>
    /// Вычислить подходит ли месяц под период.
    /// </summary>
    /// <param name="period">Период.</param>
    /// <param name="beginningYear">Дата от которой отсчитывается начало отправления поручений.</param>
    /// <param name="endYear">Вычисляемая дата.</param>
    /// <returns>True если вычисляемая дата попадает в период.</returns>
    private bool IsCorrectMonth(int period, DateTime beginningMonth, DateTime endMonth)
    {
      while (beginningMonth <= endMonth)
      {
        if (endMonth.Month == beginningMonth.Month && endMonth.Year == beginningMonth.Year)
          return true;
        
        beginningMonth = beginningMonth.AddMonths(period);
      }
      
      return false;
    }
    
    #endregion
    
    /// <summary>
    /// Отправить поручение.
    /// </summary>
    /// <param name="setting">Настройки.</param>
    /// <param name="deadline">Срок.</param>
    /// <returns></returns>
    private void SendActionItem(DirRX.ActionItems.IRepeatSetting setting, DateTime deadline)
    {
      var task = DirRX.Solution.ActionItemExecutionTasks.Create();
      
      task.Category = setting.Category;
      task.AssignedBy = setting.AssignedBy;
      task.Initiator = setting.Initiator;
      task.Mark = setting.Mark;
      task.ReportDeadline = setting.ReportDeadline;
      
      foreach (var subscriberSetting in setting.Subscribers)
      {
        var subscriber = task.Subscribers.AddNew();
        subscriber.Subscriber = subscriberSetting.Subscriber;
      }
      
      if (setting.IsCompoundActionItem.GetValueOrDefault())
      {
        task.IsCompoundActionItem = true;
        task.IsUnderControl = setting.IsUnderControl.GetValueOrDefault();
        task.Supervisor = setting.Supervisor;

        foreach (var actionItemPartsSetting in setting.ActionItemsParts)
        {
          var actionItemParts = task.ActionItemParts.AddNew();
          actionItemParts.Assignee = actionItemPartsSetting.Assignee;
          actionItemParts.ActionItemPart = actionItemPartsSetting.ActionItemPart;
          actionItemParts.Deadline = deadline.Date;
        }
      }
      else
      {
        foreach (var coAssigneeSetting in setting.CoAssignees)
        {
          var coAssignee = task.CoAssignees.AddNew();
          coAssignee.Assignee = coAssigneeSetting.Assignee;
        }
        
        task.Assignee = setting.Assignee;
        task.IsUnderControl = setting.IsUnderControl.GetValueOrDefault();
        task.Supervisor = setting.Supervisor;
        task.Deadline = deadline.Date;
        task.ActionItem = setting.ActionItem;
      }
      
      task.RepeatSetting = setting;
      
      task.Start();
    }

    /// <summary>
    /// Отправить уведомление об актуализации расписания.
    /// </summary>
    /// <param name="setting">Настройка повторения поручений.</param>
    private void SendNotice(DirRX.ActionItems.IRepeatSetting setting)
    {
      var notice = Sungero.Workflow.SimpleTasks.Create();
      notice.Subject = DirRX.ActionItems.Resources.LastActionItemText;
      notice.NeedsReview = false;

      var routeStep = notice.RouteSteps.AddNew();
      routeStep.AssignmentType = Sungero.Workflow.SimpleTaskRouteSteps.AssignmentType.Notice;
      routeStep.Performer = setting.Initiator;
      routeStep.Deadline = null;
      
      notice.Attachments.Add(setting);
      
      if (notice.Subject.Length > Sungero.Workflow.Tasks.Info.Properties.Subject.Length)
        notice.Subject = notice.Subject.Substring(0, Sungero.Workflow.Tasks.Info.Properties.Subject.Length);
      
      notice.Start();
    }
    
    #endregion
    
    #region Синхронизация настроек уведомлений.
    
    private bool IsEqualsPriorityCollection(IEnumerable<IPriority> priorities, IEnumerable<IPriority> allPriorities)
    {
      foreach (var priority in priorities)
      {
        if (!allPriorities.Contains(priority))
          return false;
      }
      
      return true;
    }
    
    #endregion
  }
}