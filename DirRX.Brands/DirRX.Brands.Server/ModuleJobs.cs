using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Brands.Server
{
  public class ModuleJobs
  {

    /// <summary>
    /// Рассылка уведомлений о приближении срока окончания регистраций.
    /// </summary>
    public virtual void SendExpiredRegistrationsNoticeJob()
    {
      Logger.Debug("Старт фонового процесса: \"Рассылка уведомлений о приближении срока окончания регистраций\".");
      
      var role = Roles.GetAll(r => r.Sid == Constants.Module.IntellectualPropertySpecialistRoleGuid).FirstOrDefault();
      var expiredRegistrations = BrandsRegistrations.GetAll(r => r.ValidUntil.HasValue &&
                                                            r.ValidUntil.Value == Calendar.Today.AddDays(7));
      if (role == null || !expiredRegistrations.Any())
        return;
      
      var task = Sungero.Workflow.SimpleTasks.Create();
      task.Subject = Resources.ExpiredRegistrationsNoticeSubject;
      var step = task.RouteSteps.AddNew();
      step.AssignmentType = Sungero.Workflow.SimpleTask.AssignmentType.Notice;
      step.Performer = role;
      task.ActiveText = Resources.ExpiredRegistrationsNoticeText;
      
      foreach (var registration in expiredRegistrations)
        task.Attachments.Add(registration);
      
      task.Start();
      
      Logger.Debug("Выполнение фонового процесса: \"Рассылка уведомлений о приближении срока окончания регистраций\" закончено.");
    }

    /// <summary>
    /// Изменение статуса у истекших регистраций товарных знаков.
    /// </summary>
    public virtual void SetRegistrationExpiredStatus()
    {
      Logger.Debug("Старт фонового процесса: \"Изменение статуса у истекших регистраций товарных знаков\".");
      
      var countChange = uint.MinValue;
      var expiredRegistrations = BrandsRegistrations.GetAll(r => r.ValidUntil.HasValue &&
                                                            r.ValidUntil.Value <= Calendar.Today.AddDays(-1) &&
                                                            r.Status != Brands.BrandsRegistration.Status.Overdue);
      foreach (var registration in expiredRegistrations)
      {
        try
        {
          var lockInfo = Locks.GetLockInfo(registration);
          var isLockedByOther = lockInfo != null && lockInfo.IsLocked;
          if (!isLockedByOther)
          {
            registration.Status = Brands.BrandsRegistration.Status.Overdue;
            registration.Save();
            countChange++;
          }
        }
        catch (Exception ex)
        {
          Logger.DebugFormat("Во время изменения статуса элемента регистраций товарных знаков с ID {0} - произошла ошибка: {1}", registration.Id.ToString(), ex.Message);
        }
      }
      
      Logger.DebugFormat("Выполнение фонового процесса: \"Изменение статуса у истекших регистраций товарных знаков\" закончено. Изменен статус у {0} элементов.", countChange);
    }

    /// <summary>
    /// Рассылка заданий-оповещений о появлении новых наименований.
    /// </summary>
    public virtual void NewDesignationSendTaskJob()
    {
      var today = Calendar.Now;
      string paramKey = Constants.Module.LastNewDesignationTaskDateTimeDocflowParamName;
      var previousDate = GetLastExecutionProccessDateTime(paramKey);
      if (!previousDate.HasValue)
        previousDate = Calendar.BeginningOfYear(today);

      var role = Roles.GetAll(x => x.Sid == Constants.Module.IntellectualPropertySpecialistRoleGuid).FirstOrDefault();
      var newWordMarks = WordMarks.GetAll().WhereHistory(h => h.Action == Sungero.CoreEntities.History.Action.Create &&
                                                         h.Operation == null &&
                                                         h.HistoryDate > previousDate);
      
      if (role != null && newWordMarks.Any())
      {
        string activeText = Resources.DesignationTaskActiveTextFormat(previousDate.Value.ToString("d"), today.ToString("d"));
        
        var task = Sungero.Workflow.SimpleTasks.Create();
        task.Subject = Resources.DesignationTaskSubject;
        var step = task.RouteSteps.AddNew();
        step.AssignmentType = Sungero.Workflow.SimpleTask.AssignmentType.Assignment;
        task.Deadline = Calendar.Now.AddWorkingDays(3);
        task.NeedsReview = false;
        step.Performer = role;
        task.ActiveText = activeText;
        
        foreach (var mark in newWordMarks)
          task.Attachments.Add(mark);
        
        task.Start();
      }
      
      UpdateLastExecutionProccessDate(paramKey, today);
    }
    
    #region Запись и считывание дат последнего выполнения фоновых процессов.
    
    /// <summary>
    /// Получить дату последнего выполнения фонового процесса.
    /// </summary>
    /// <param name="key">Имя ключа в таблице параметров.</param>
    /// <returns>Дата последнего выполнения.</returns>
    public static DateTime? GetLastExecutionProccessDateTime(string key)
    {
      var command = string.Format(Queries.Module.SelectDocflowParamsValue, key);
      try
      {
        var executionResult = Sungero.Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
        var date = string.Empty;
        if ((executionResult is DBNull) || executionResult == null)
          return null;

        date = executionResult.ToString();
        Logger.DebugFormat("Время последнего выполнения данного фонового процесса с ключом {0} записанное в БД: {1} (UTC)", key, date);

        return Calendar.FromUtcTime(DateTime.Parse(date, null, System.Globalization.DateTimeStyles.AdjustToUniversal));
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("При получении времени последнего выполнения фонового процесса с ключом {0} возникла ошибка", ex, key);
        return null;
      }
    }

    /// <summary>
    /// Обновить дату последней рассылки уведомлений.
    /// </summary>
    /// <param name="key">Имя ключа в таблице параметров.</param>
    /// <param name="notificationDate">Дата рассылки уведомлений.</param>
    public static void UpdateLastExecutionProccessDate(string key, DateTime notificationDate)
    {
      var newDate = notificationDate.Add(-Calendar.UtcOffset).ToString("yyyy-MM-ddTHH:mm:ss.ffff+0");
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.Module.InsertOrUpdateDocflowParamsValue, new[] { key, newDate });
      Logger.DebugFormat("Зафиксировано время выполнения фонового процесса с ключом {0} {1} (UTC)", key, newDate);
    }
    
    #endregion

  }
}