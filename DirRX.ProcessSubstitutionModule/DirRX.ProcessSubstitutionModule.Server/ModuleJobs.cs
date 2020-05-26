using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using System.Text;
using CommonLibrary;

namespace DirRX.ProcessSubstitutionModule.Server
{
  public class ModuleJobs
  {

    /// <summary>
    /// Удаление дублирований системных замещений.
    /// </summary>
    public virtual void SubstitutionsClear()
    {     
      var processSubstitutions = SubstituteConnections.GetAll().ToList().SelectMany(s => s.SysSubstitutionCollection.Select(sc => sc.SysSubstitution)).ToList();
      
      var systemSubstitution = Substitutions.GetAll(s => s.IsSystem == true && s.EndDate == null).ToList().Where(s => !processSubstitutions.Contains(s));
      var systemSubstitutionsGroupByName = systemSubstitution.GroupBy(s => s.Name);
      
      foreach (var systemSubstitutionsGroup in systemSubstitutionsGroupByName)
      {
        if (systemSubstitutionsGroup.Count() > 1)
        {
          Logger.DebugFormat("Substitution {0} in process. Count = {1}.", systemSubstitutionsGroup.Key, systemSubstitutionsGroup.Count());
          var substitutions = systemSubstitutionsGroup.Take(systemSubstitutionsGroup.Count() - 1).ToList();

          foreach (var substitution in substitutions)
          {
            try
            {              
              Substitutions.Delete(substitution);
            }
            catch (Exception ex)
            {
              Logger.Debug(ex.Message);
            }
          }
        }
      }
    }
    
    /// <summary>
    /// Отправить напоминание о заданиях по замещению.
    /// </summary>
    public virtual void SendSubstitutionReminderJob()
    {
      var substitutes = ProcessSubstitutionModule.Functions.Module.GetActiveSubstituteUsers();
      foreach (var user in substitutes)
      {
        var count = ProcessSubstitutionModule.Functions.Module.GetAssignmentsCountBySubstitution(user);
        if (count.TotalCount > 0)
        {
          var activeText = DirRX.ProcessSubstitutionModule.Resources.JobReminderActiveTextFormat(count.TotalCount, count.UnreadedCount);
          
          var task = Sungero.Workflow.SimpleTasks.CreateWithNotices(DirRX.ProcessSubstitutionModule.Resources.JobReminderSubject, user);
          task.ActiveText = activeText.ToString();
          task.Attachments.Add(ProcessSubstitutionModule.SpecialFolders.ProcessSubstitutionFolder);
          
          task.Start();
        }
      }
    }
    
    /// <summary>
    /// Отрправить сотрудникам уведомления о том, что им делегированы работы.
    /// </summary>
    public virtual void SendNoticeJob()
    {
      Logger.DebugFormat("Старт фонового процесса: \"Рассылка по назначенным замещениям\".");
      var today = Calendar.Now;
      var paramKey = Constants.Module.SendNoticeDateTimeDocflowParamName;
      var previousDate = GetLastExecutionProccessDateTime(paramKey);
      if (!previousDate.HasValue)
        previousDate = Calendar.BeginningOfYear(today);
      
      var userNoticeDictionary = new Dictionary<IUser, Sungero.Workflow.ISimpleTask>();

      // Если дата окончания замещения меньше текущей даты, то не отправлять уведомление.
      var substitutions = ProcessSubstitutions.GetAll(s => s.Created.HasValue && s.Created.Value >= previousDate &&
                                                      !(s.EndDate.HasValue && s.EndDate.Value < today));
      
      foreach (var record in substitutions)
      {
        Logger.Debug("Обработка замещения по процессам с ИД: " + record.Id);
        foreach (var user in record.SubstitutionCollection.Select(s => s.Substitute).Distinct())
        {
          Logger.Debug("Создание/обновление задачи для пользователя с ИД: " + user.Id);
          var name = Sungero.Company.PublicFunctions.Employee.GetShortName(record.Employee, Sungero.Core.DeclensionCase.Nominative, true);
          var period = GetSubstitutionPeriod(record);
          var jobTitle = string.Empty;
          if (record.Employee.JobTitle != null)
            jobTitle = ", " + record.Employee.JobTitle.Name;
          
          var activeText = DirRX.ProcessSubstitutionModule.Resources.ActiveTextTemplateFormat(name,
                                                                                              jobTitle,
                                                                                              period,
                                                                                              GetProcesses(record, user));
          if (userNoticeDictionary.ContainsKey(user))
          {
            var task = userNoticeDictionary[user];
            task.ActiveText += Environment.NewLine + Environment.NewLine + activeText;
            task.Attachments.Add(record);
          }
          else
          {
            var task = Sungero.Workflow.SimpleTasks.CreateWithNotices(DirRX.ProcessSubstitutionModule.Resources.NoticeTaskSubjectFormat(name, period), user);
            task.ActiveText = activeText.ToString();
            task.Attachments.Add(ProcessSubstitutionModule.SpecialFolders.ProcessSubstitutionFolder);
            task.Attachments.Add(record);
            userNoticeDictionary.Add(user, task);
          }
          
          Logger.DebugFormat("Создание/обновление задачи для пользователя с ИД: {0} завершено.", user.Id);
        }
        Logger.DebugFormat("Обработка замещения по процессам с ИД: {0} завершена.", record.Id);
      }
      
      // Отправить сформированные уведомления.
      foreach (var notice in userNoticeDictionary.Select(d => d.Value))
      {
        Logger.DebugFormat("Отправка задачи с ИД: {0}.", notice.Id);
        var substitutionCount = notice.Attachments.Where(a => ProcessSubstitutions.Is(a)).Count();
        if (substitutionCount > 1)
          notice.Subject = DirRX.ProcessSubstitutionModule.Resources.NoticeTaskSubjectMultiFormat(substitutionCount);
        notice.Start();
        Logger.DebugFormat("Отправка задачи с ИД: {0} завершена.", notice.Id);
      }
      
      UpdateLastExecutionProccessDate(paramKey, today);
      
      Logger.DebugFormat("Выполнение фонового процесса: \"Рассылка по назначенным замещениям\" завершено!");
    }
    
    /// <summary>
    /// Создать, обновить и удалить системные замещения.
    /// </summary>
    public virtual void CreateSystemSubstitutions()
    {
      foreach (var record in SubstituteConnections.GetAll(g => g.NeedUpdateSubtitution.HasValue && g.NeedUpdateSubtitution.Value))
      {
        try
        {
          var lockInfo = Locks.GetLockInfo(record);
          
          if (!lockInfo.IsLocked)
          {
            DirRX.ProcessSubstitutionModule.Functions.SubstituteConnection.UpdateSystemSubstitution(record);
            Logger.DebugFormat("Substitutions for record {0} are created.", record.Id);
          }
          else
            Logger.DebugFormat("Record {0} is locked.", record.Id);
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("Can not create substitutions for record {0}. Message: {1}", record.Id, ex.Message);
        }
      }
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
    
    /// <summary>
    /// Получить период замещения.
    /// </summary>
    /// <param name="record">Замещение.</param>
    /// <returns>Строка с периодом замещениея.</returns>
    private string GetSubstitutionPeriod(IProcessSubstitution record)
    {
      var result = new StringBuilder();
      if (record.BeginDate.HasValue)
        result.AppendFormat("{0} {1} ", DirRX.ProcessSubstitutionModule.Resources.BeginDateAdditionalText, record.BeginDate.Value.ToShortDateString());
      
      if (record.EndDate.HasValue)
        result.AppendFormat("{0} {1} ", DirRX.ProcessSubstitutionModule.Resources.EndDateAdditionalText, record.EndDate.Value.ToShortDateString());
      
      if (result.Length == 0)
        result.Append(DirRX.ProcessSubstitutionModule.Resources.Termless);
      return result.ToString().Trim();
    }
    
    /// <summary>
    /// Получить все процессы, по которым пользователь замещает.
    /// </summary>
    /// <param name="record">Замещение.</param>
    /// <param name="user">Замещающий.</param>
    /// <returns>Список процессов.</returns>
    private string GetProcesses(IProcessSubstitution record, IUser user)
    {
      var result = new StringBuilder();
      var processList = record.SubstitutionCollection.Where(s => Users.Equals(s.Substitute, user)).Select(p => p.Process).ToList();
      if (processList.Any(p => !p.HasValue))
      {
        foreach (var process in  Enumeration.GetItems(typeof(ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process)))
        {
          if (!record.SubstitutionCollection.Any(s => Enumeration.Equals(process, s.Process)))
            processList.Add(process);
        }
      }
      
      foreach (var process in processList)
      {
        if (process != null)
          result.AppendFormat("- {0}{1}", record.Info.Properties.SubstitutionCollection.Properties.Process.GetLocalizedValue(process), Environment.NewLine);
      }
      
      return result.ToString().Trim();
    }
  }
}