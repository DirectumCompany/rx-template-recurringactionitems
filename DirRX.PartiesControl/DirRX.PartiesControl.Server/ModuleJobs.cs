using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using System.Data;
using Sungero.Docflow;
using System.IO;

namespace DirRX.PartiesControl.Server
{
  public class ModuleJobs
  {
    /// <summary>
    /// Отправка уведомление с отчётом для ГД.
    /// </summary>
    public virtual void SendDocumentConrolReport()
    {
      var task = Functions.Module.CreateDocumentControlTask();
      
      if (task != null)
        task.Start();
      else
        throw new Exception(Resources.DocumentControlException);
    }

    /// <summary>
    /// Повторная отправка событий о включении/исключении контрагента в стоп-лист.
    /// </summary>
    public virtual void CSBResendStoplistInfoJob()
    {
      Logger.Debug("CSBResendStoplistInfoJob: Повторная отправка событий о включении/исключении контрагента в стоп-лист.");
      
      var resendIncludeCounterparties = DirRX.Solution.Companies.GetAll(c => c.StoplistHistory.Any(s => (s.EventGUID == null || s.EventGUID == string.Empty) && s.IsIncludeSended != true));
      foreach (var counterparty in resendIncludeCounterparties)
      {
        try
        {
          foreach (var stoplistRecord in counterparty.StoplistHistory.Where(s => string.IsNullOrEmpty(s.EventGUID) && s.IsIncludeSended != true))
          {
            try
            {
              var eventGUID = DirRX.Solution.PublicFunctions.Company.Remote.CreateAndSendStoplistToCSB(stoplistRecord,
                                                                                                       DirRX.Solution.PublicConstants.Parties.Company.CSBStoplistAction.Include,
                                                                                                       DirRX.Solution.PublicConstants.Parties.Company.CSBStoplistStatus.Started,
                                                                                                       stoplistRecord.IncComment);
              if (!string.IsNullOrEmpty(eventGUID))
              {
                stoplistRecord.EventGUID = eventGUID;
                stoplistRecord.IsIncludeSended = true;
              }
            }
            catch (Exception ex)
            {
              throw new Exception(string.Format("При отправке события о включении контрагента {0}, Id:{1} в стоп-лист возникла ошибка. {2}, {3}", 
                                                counterparty.Name, counterparty.Id, ex.Message, ex.StackTrace));
            }
          }
          
          counterparty.Save();
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("При отправке в КСШ информации о включении/исключении контрагентов в стоп-лист произошла ошибка: ", ex);
        }
      }
      
      var resendExcludeCounterparties = DirRX.Solution.Companies.GetAll(c => c.StoplistHistory.Any(s => s.ExcludeDate.HasValue && s.IsExcludeSended != true));
      foreach (var counterparty in resendExcludeCounterparties)
      {
        try
        {
          foreach (var stoplistRecord in counterparty.StoplistHistory.Where(s => s.ExcludeDate.HasValue && s.IsExcludeSended != true))
          {
            try
            {
              var eventGUID = string.Empty;
              if (!counterparty.StoplistHistory.Any(s => !s.ExcludeDate.HasValue))
                eventGUID = DirRX.Solution.PublicFunctions.Company.Remote.CreateAndSendStoplistToCSB(stoplistRecord,
                                                                                                     DirRX.Solution.PublicConstants.Parties.Company.CSBStoplistAction.Exclude,
                                                                                                     DirRX.Solution.PublicConstants.Parties.Company.CSBStoplistStatus.Ended,
                                                                                                     stoplistRecord.ExcComment);
              else
                eventGUID = DirRX.Solution.PublicFunctions.Company.Remote.CreateAndSendStoplistToCSB(stoplistRecord,
                                                                                                     DirRX.Solution.PublicConstants.Parties.Company.CSBStoplistAction.Include,
                                                                                                     DirRX.Solution.PublicConstants.Parties.Company.CSBStoplistStatus.Ended,
                                                                                                     stoplistRecord.ExcComment);
              if (!string.IsNullOrEmpty(eventGUID))
                stoplistRecord.IsExcludeSended = true;
            }
            catch (Exception ex)
            {
              throw new Exception(string.Format("При отправке события о исключении контрагента {0}, Id:{1} в стоп-лист возникла ошибка. {2}, {3}",
                                                counterparty.Name, counterparty.Id, ex.Message, ex.StackTrace));
            }
          }

          counterparty.Save();
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("При отправке в КСШ информации о включении/исключении контрагентов в стоп-лист произошла ошибка: ", ex);
        }
      }
    }

    /// <summary>
    /// Обработка приёма документов при проверке контрагента.
    /// </summary>
    public virtual void AllDocsReceivedHandling()
    {
      Logger.Debug("Обработка получения документов при проверке контрагента");
      
      var revisionRequestsCounterpartyGroups = DirRX.PartiesControl.RevisionRequests.GetAll(r => r.Counterparty.IsDocumentsProvided == false).ToList()
        .GroupBy(r => r.Counterparty);
      
      foreach (var revisionRequestsGroup in revisionRequestsCounterpartyGroups)
      {
        var revisionRequest = revisionRequestsGroup.OrderBy(r => r.Created.GetValueOrDefault()).LastOrDefault();
        
        if (revisionRequest.AllDocsReceived.GetValueOrDefault())
        {
          var revisionRequestLockInfo = Locks.GetLockInfo(revisionRequest.Counterparty);
          if (revisionRequestLockInfo.IsLocked)
          {
            Logger.Debug(revisionRequestLockInfo.LockedMessage);
            continue;
          }
          
          var waitingAssignments = DirRX.Solution.ApprovalCheckingAssignments.GetAll(a => a.Status == Sungero.Workflow.Assignment.Status.InProcess &&
                                                                                     a.ApprovalStage.IsAssignmentAllDocsReceived == true).ToList()
            .Where(a => Sungero.Contracts.ContractualDocuments.Is(a.DocumentGroup.OfficialDocuments.FirstOrDefault()) &&
                   DirRX.Solution.Companies.Equals(Sungero.Contracts.ContractualDocuments.As(a.DocumentGroup.OfficialDocuments.FirstOrDefault()).Counterparty, revisionRequest.Counterparty));
          
          // Признак в карточке контрагента служит триггером для выполнения заданий, поэтому заполняем его только, если все задания выполненны.
          if (!waitingAssignments.Any())
          {
            revisionRequest.Counterparty.IsDocumentsProvided = true;
            revisionRequest.Counterparty.Save();
          }
          else
          {
            bool allAssignmentsCompleted = true;
            
            foreach (var assignment in waitingAssignments)
            {
              var assignmentLockInfo = Locks.GetLockInfo(assignment);
              if (assignmentLockInfo.IsLocked)
              {
                Logger.Debug(assignmentLockInfo.LockedMessage);
                break;
              }
              
              assignment.Complete(DirRX.Solution.ApprovalCheckingAssignment.Result.Accept);
            }
            
            if (allAssignmentsCompleted)
            {
              revisionRequest.Counterparty.IsDocumentsProvided = true;
              revisionRequest.Counterparty.Save();
            }
          }
        }
      }
      
      /// <summary>
    }
    
    /// <summary>
    /// Помещение в дело связанных документов.
    /// </summary>
    public virtual void PlacingToCaseFileJob()
    {
      Logger.Debug("Старт фонового процесса PlacingToCaseFileJob");
      var requests = RevisionRequests.GetAll()
        .Where(r => r.CaseFile != null && r.HasRelations && r.PlacedToCaseFileDate.HasValue && r.PlacedToCaseFileDate.Value >= Calendar.Today.AddDays(-7))
        .ToList();
      Logger.DebugFormat("Заявок на обработку: {0}", requests.Count);
      
      foreach (var request in requests)
      {
        Logger.DebugFormat("Обработка заявки: {0}", request.Id);
        var caseFile = request.CaseFile;
        var relatedDocs = request.Relations.GetRelated()
          .Where(d => OfficialDocuments.Is(d))
          .Where(d => OfficialDocuments.As(d).CaseFile == null);
        foreach (var related in relatedDocs)
        {
          Logger.DebugFormat("Обработка связанного документа: {0}", related.Id);
          if (!Locks.GetLockInfo(related).IsLockedByOther)
          {
            OfficialDocuments.As(related).CaseFile = caseFile;
            related.Save();
          }
          else
            Logger.DebugFormat("Связанный документ {0} заблокирован", related.Id);
        }
      }
      Logger.Debug("Завершение фонового процесса PlacingToCaseFileJob");
    }
    
    #region Передача оригиналов в архив
    /// <summary>
    /// Агент рассылки уведомления о необходимости передачи оригиналов в архив.
    /// </summary>
    public virtual void TransferringOriginalsReminderJob()
    {
      int initiatorMonthCount;
      var initiatorMonthCountConstant = DirRX.ContractsCustom.ContractConstants.GetAll(c => c.Sid == Constants.Module.InitiatorMonthCountGuid.ToString()).FirstOrDefault();
      if (initiatorMonthCountConstant == null || !initiatorMonthCountConstant.Period.HasValue || initiatorMonthCountConstant.Unit != DirRX.ContractsCustom.ContractConstant.Unit.Month)
      {
        Logger.Debug("TransferringOriginalsReminder: Не заполнена константа \"Срок формирования уведомления инициатору заявки на проверку о передаче оригиналов в архив\", " +
                     "или в константе указана единица измерения не равная \"Месяц\"");
        initiatorMonthCount = 1;
      }
      else
        initiatorMonthCount = initiatorMonthCountConstant.Period.Value;
      
      int supervisorMonthCount;
      var supervisorMonthCountConstant = DirRX.ContractsCustom.ContractConstants.GetAll(c => c.Sid == Constants.Module.SupervisorMonthCountGuid.ToString()).FirstOrDefault();
      if (supervisorMonthCountConstant == null || !supervisorMonthCountConstant.Period.HasValue || supervisorMonthCountConstant.Unit != DirRX.ContractsCustom.ContractConstant.Unit.Month)
      {
        Logger.Debug("TransferringOriginalsReminder: Не заполнена константа \"Срок формирования уведомления куратору заявки на проверку о передаче оригиналов в архив\", " +
                     "или в константе указана единица измерения не равная \"Месяц\"");
        supervisorMonthCount = 2;
      }
      else
        supervisorMonthCount = supervisorMonthCountConstant.Period.Value;
      
      CreateTableForTransferringOriginals();
      
      var lastNotification = GetLastNotificationDate();
      var today = Calendar.Today;
      var alreadySendedDocs = GetDocumentsWithSendedTask();
      var requests = RevisionRequests.GetAll()
        .Where(r => !alreadySendedDocs.Contains(r.Id))
        .ToList()
        .Where(r => r.CheckingDate.HasValue && (r.CheckingDate.Value.AddMonths(initiatorMonthCount) > lastNotification && r.CheckingDate.Value.AddMonths(initiatorMonthCount) <= today ||
                                                r.CheckingDate.Value.AddMonths(supervisorMonthCount) > lastNotification && r.CheckingDate.Value.AddMonths(supervisorMonthCount) <= today))
        .Where(r => r.AllDocsReceived == false)
        .ToList();
      
      Logger.Debug("Обработка заявок на проверку контрагентов.");
      foreach (var request in requests)
      {
        Logger.DebugFormat("Обработка заявки {0}", request.Id);
        try
        {
          var taskIds = GetTaskIDs(request.Id);
          
          if (taskIds[0] == 0 && request.CheckingDate.Value.AddMonths(initiatorMonthCount) <= today)
            CreateAndStartTask(request, Constants.Module.InitiatorTask);
          
          if (taskIds[1] == 0 && request.CheckingDate.Value.AddMonths(supervisorMonthCount) <= today)
            CreateAndStartTask(request, Constants.Module.SupervisorTask);
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("Ошибка при отправке уведомления по заявке {1}. {0}.", ex, request.Id);
        }
      }
      UpdateLastNotificationDate(today);
    }

    /// <summary>
    /// Создать таблицу для хранения информации о заявках и отправленных по ним задачах.
    /// </summary>
    private static void CreateTableForTransferringOriginals()
    {
      var command = Queries.Module.CreateTableForTransferringOriginals;
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
    }

    /// <summary>
    /// Получить дату последней рассылки уведомлений.
    /// </summary>
    /// <returns>Дата последней рассылки.</returns>
    private static DateTime GetLastNotificationDate()
    {
      var command = string.Format(Queries.Module.SelectLastNotificationDate, Constants.Module.NotificationDatabaseKey);
      try
      {
        var executionResult = Sungero.Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
        var date = string.Empty;
        if (!(executionResult is DBNull) && executionResult != null)
          date = executionResult.ToString();
        Logger.DebugFormat("Прошлая дата уведомления в БД: {0}", date);
        
        var result = DateTime.Parse(date);
        return result;
      }
      catch (Exception ex)
      {
        Logger.DebugFormat("Ошибка при получении даты прошлого уведомления {0}", ex);
        return Calendar.Now.AddDays(-1);
      }
    }
    /// <summary>
    /// Получить документы, по которым уже отправлены уведомления.
    /// </summary>
    /// <returns>Массив с Id.</returns>
    private static List<int> GetDocumentsWithSendedTask()
    {
      var result = new List<int>();
      var commandText = Queries.Module.SelectDocumentWithSenderTask;
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        try
        {
          command.CommandText = commandText;
          using (var rdr = command.ExecuteReader())
          {
            while (rdr.Read())
              result.Add(rdr.GetInt32(0));
          }
          return result;
        }
        catch (Exception ex)
        {
          Logger.Error("Ошибка при получении документов, для которых уже отправлены уведомления.", ex);
          return result;
        }
      }
    }
    /// <summary>
    /// Записать документы.
    /// </summary>
    /// <param name="ids">Документы.</param>
    private static void AddDocumentsToTable(IEnumerable<int> ids)
    {
      if (!ids.Any())
        return;
      
      var command = string.Format(Queries.Module.AddDocumentsToTable, string.Join("), (", ids));
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
    }
    
    /// <summary>
    /// Добавить в табличку задачу, с указанием документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="field">Поле таблицы для вставки.</param>
    /// <param name="task">Задача, которая была запущена.</param>
    private static void AddTaskToTable(int document, string field, int task)
    {
      var command = string.Format(Queries.Module.AddTask, document, task, field);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
      Logger.DebugFormat("Задача {0} для заявки {1} отправлена и зафиксирована в БД.", task, document);
    }
    
    /// <summary>
    /// Обновить дату последней рассылки уведомлений.
    /// </summary>
    /// <param name="notificationDate">Дата рассылки уведомлений.</param>
    private static void UpdateLastNotificationDate(DateTime notificationDate)
    {
      var newDate = notificationDate.ToString("yyyy-MM-dd HH:mm:ss");
      var command = string.Format(Queries.Module.UpdateLastNotificationDate,
                                  Constants.Module.NotificationDatabaseKey, newDate);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
      Logger.DebugFormat("Новая дата уведомления: {0}", newDate);
    }
    
    /// <summary>
    /// Получить ИД задач пользователям.
    /// </summary>
    /// <param name="requestId">ИД заявки.</param>
    /// <returns>Массив с ИД задач инициатору и куратору.</returns>
    private static int[] GetTaskIDs(int requestId)
    {
      var result = new int [2] {0, 0};
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = string.Format(Queries.Module.SelectTaskIDs, requestId);
        var queryResult = command.ExecuteReader();
        if (queryResult.Read())
        {
          result[0] = (int)queryResult[0];
          result[1] = (int)queryResult[1];
        }
        queryResult.Close();
      }
      return result;
    }
    
    /// <summary>
    /// Отправить уведомление и добавить в таблицу.
    /// </summary>
    /// <param name="request">Запрос.</param>
    /// <param name="type">Тип уведомления (ицициатору или куратору).</param>
    private static void CreateAndStartTask(IRevisionRequest request, string type)
    {
      Logger.DebugFormat("Отправка уведомления для заявки {0}", request.Id);
      var performers = new List<IRecipient>();
      performers.Add(type == Constants.Module.InitiatorTask ? request.PreparedBy : request.Supervisor);
      
      var task = Sungero.Workflow.SimpleTasks.CreateWithNotices(DirRX.PartiesControl.Resources.TransferringOriginalsSubjectFormat(request.DisplayValue), performers, new [] { request });
      task.Start();
      AddTaskToTable(request.Id, type, task.Id);
    }
    #endregion
    
    /// <summary>
    /// Напоминание об истечении срока действия последней проверки контрагентов.
    /// </summary>
    public virtual void VerificationReminderJob()
    {
      Logger.Debug("Старт фонового процесса VerificationReminderJob");
      IUser user = null;
      foreach (var counterparty in DirRX.Solution.Companies.GetAll().Where(c => c.CheckingValidDate.Value.Date == Calendar.Today.AddDays(30).Date))
      {
        user = null;
        Logger.DebugFormat("Начало обработки контрагента: {0}", counterparty.DisplayValue);
        var contractualDocument = Sungero.Contracts.ContractualDocuments.GetAll()
          .Where(c => DirRX.Solution.Companies.Equals(counterparty, c.Counterparty) && (!c.ValidTill.HasValue || c.ValidTill.Value >= Calendar.Today))
          .OrderByDescending(c => c.ValidTill.Value)
          .FirstOrDefault();
        
        if (contractualDocument != null)
        {
          Logger.DebugFormat("Отправка уведомления по контрагенту: {0}", counterparty.DisplayValue);
          if (contractualDocument.ResponsibleEmployee != null)
            user = contractualDocument.ResponsibleEmployee;
          else
          {
            var contract = DirRX.Solution.Contracts.As(contractualDocument);
            if (contract != null && contract.Supervisor != null)
              user = contract.Supervisor;
            
            var supAgreement = DirRX.Solution.SupAgreements.As(contractualDocument);
            if (supAgreement != null && supAgreement.Supervisor != null)
              user = supAgreement.Supervisor;
          }

          Sungero.Workflow.ISimpleTask task = null;
          if (user != null)
            task = Sungero.Workflow.SimpleTasks.CreateWithNotices(DirRX.PartiesControl.Resources.VerificationReminderSubjectFormat(counterparty.DisplayValue), user);
          else
            task = Sungero.Workflow.SimpleTasks.CreateWithNotices(DirRX.PartiesControl.Resources.VerificationReminderSubjectFormat(counterparty.DisplayValue), Roles.Administrators);
          task.ActiveText = DirRX.PartiesControl.Resources.VerificationReminderTextFormat(counterparty.DisplayValue);
          task.Attachments.Add(counterparty);
          
          task.Start();
        }
        Logger.DebugFormat("Завершение обработки контрагента: {0}", counterparty.DisplayValue);
      }
      Logger.Debug("Завершение фонового процесса VerificationReminderJob");
    }
    
    /// <summary>
    /// Обновление статуса проверки у контрагента с истекшим сроком проверки.
    /// </summary>
    public virtual void ResetCheckingStatusJob()
    {
      Logger.DebugFormat("Старт фонового процесса: \"Обновление статуса проверки\".");
      
      var stoplistStatus = CounterpartyStatuses.GetAll(s => s.Sid == DirRX.PartiesControl.PublicConstants.CounterpartyStatus.DefaultStatus.StopListSid).FirstOrDefault();
      var checkRequiredStatus = CounterpartyStatuses.GetAll(s => s.Sid == DirRX.PartiesControl.PublicConstants.CounterpartyStatus.DefaultStatus.CheckingRequiredSid).FirstOrDefault();
      
      if (stoplistStatus == null || checkRequiredStatus == null)
        return;
      
      var yesterday = Calendar.Today.AddDays(-1).EndOfDay();
      var expiredCheckCompanies = DirRX.Solution.Companies.GetAll(c => c.CheckingValidDate.HasValue &&
                                                                  c.CheckingValidDate.Value <= yesterday &&
                                                                  !CounterpartyStatuses.Equals(c.CounterpartyStatus, stoplistStatus) &&
                                                                  !CounterpartyStatuses.Equals(c.CounterpartyStatus, checkRequiredStatus));
      foreach (var company in expiredCheckCompanies)
      {
        if (!Locks.GetLockInfo(company).IsLocked)
        {
          company.CounterpartyStatus = checkRequiredStatus;
          company.Save();
          
          Logger.DebugFormat("Обновлен статус контрагента \"{0}\" (Id {1}).", company.Name, company.Id.ToString());
        }
        else
          Logger.DebugFormat("Контрагент заблокирован \"{0}\" (Id {1}).", company.Name, company.Id.ToString());
      }
    }
  }
}