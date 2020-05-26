using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Solution.Module.Contracts.Server
{
  partial class ModuleJobs
  {

    /// <summary>
    /// Агент рассылки уведомления об окончании срока действия договорных документов.
    /// </summary>
    public override void SendNotificationForExpiringContracts()
    {
      using (var session = new Sungero.Domain.Session())
      {
        CreateTableForExpiringContracts();
        
        var lastNotification = GetLastNotificationDate();
        var today = Calendar.Today;
        var alreadySendedDocs = GetDocumentsWithSendedTask();
        ClearTable(true);
        
        var contractualDocs = new List<Sungero.Contracts.IContractualDocument>();
        // Договоры.
        var contracts = Solution.Contracts.GetAll()
          .Where(c => !alreadySendedDocs.Contains(c.Id))
          .Where(c => c.LifeCycleState == Solution.Contract.LifeCycleState.Active ||
                 c.LifeCycleState == Solution.Contract.LifeCycleState.Draft)
          .Where(c => c.ResponsibleEmployee != null || c.Author != null)
          .ToList()
          .Where(c => lastNotification.ToUserTime(c.ResponsibleEmployee ?? c.Author).AddMonths(c.DaysToFinishWorks != null ? c.DaysToFinishWorks.Value : 0) < c.ValidTill &&
                 c.ValidTill <= Calendar.GetUserToday(c.ResponsibleEmployee ?? c.Author).AddMonths(c.DaysToFinishWorks != null ? c.DaysToFinishWorks.Value : 0))
          .ToList();
        AddDocumentsToTable(contracts.Select(x => x.Id));
        contractualDocs.AddRange(contracts);
        
        // ДС.
        var supAgreements = Solution.SupAgreements.GetAll()
          .Where(s => !alreadySendedDocs.Contains(s.Id))
          .Where(s => s.LifeCycleState == Solution.SupAgreement.LifeCycleState.Active ||
                 s.LifeCycleState == Solution.SupAgreement.LifeCycleState.Draft)
          .Where(s => s.ResponsibleEmployee != null || s.Author != null)
          .ToList()
          .Where(s => lastNotification.ToUserTime(s.ResponsibleEmployee ?? s.Author).AddMonths(s.DaysToFinishWorks != null ? s.DaysToFinishWorks.Value : 0) < s.ValidTill &&
                 s.ValidTill <= Calendar.GetUserToday(s.ResponsibleEmployee ?? s.Author).AddMonths(s.DaysToFinishWorks != null ? s.DaysToFinishWorks.Value : 0))
          .ToList();
        AddDocumentsToTable(supAgreements.Select(x => x.Id));
        contractualDocs.AddRange(supAgreements);
        
        foreach (var contractualDoc in contractualDocs)
        {
          try
          {
            string subject = string.Empty;
            string activeText = string.Empty;
            var attachments = new List<Sungero.Content.IElectronicDocument>() { contractualDoc };
            
            var contract = Solution.Contracts.As(contractualDoc);
            if (contract != null)
            {
              subject = Sungero.Docflow.PublicFunctions.Module.TrimQuotes(
                contract.IsAutomaticRenewal == true ?
                Sungero.Contracts.Resources.AutomaticRenewalContractExpiresFormat(contract.DisplayValue) :
                Sungero.Contracts.Resources.ExpiringContractsSubjectFormat(contract.DisplayValue));
              
              activeText = Sungero.Docflow.PublicFunctions.Module.TrimQuotes(
                contract.IsAutomaticRenewal == true ?
                Sungero.Contracts.Resources.ExpiringContractsRenewalTextFormat(contract.ValidTill.Value.ToShortDateString(), contract.DisplayValue) :
                Sungero.Contracts.Resources.ExpiringContractsTextFormat(contract.ValidTill.Value.ToShortDateString(), contract.DisplayValue));

              // Добавить в приложения ДС.
              var related = contract.Relations.GetRelated(Sungero.Contracts.Constants.Module.SupAgreementRelationName).ToList();
              attachments.AddRange(related);
            }
            else
            {
              subject = Resources.ExpiringSupAgreementSubjectFormat(contractualDoc.DisplayValue);
              activeText = Resources.ExpiringSupAgreementTextFormat(contractualDoc.ValidTill.Value.ToShortDateString(), contractualDoc.DisplayValue);
            }
            
            if (subject.Length > Sungero.Workflow.SimpleTasks.Info.Properties.Subject.Length)
              subject = subject.Substring(0, Sungero.Workflow.SimpleTasks.Info.Properties.Subject.Length);
            
            var performers = Functions.Module.GetPerformersOfContractualDocument(contractualDoc);
            if (performers != null && performers.Any())
            {
              performers = performers.Where(p => p != null).Distinct().ToList();
              
              var newTask = Sungero.Workflow.SimpleTasks.CreateWithNotices(subject, performers, attachments.ToArray());
              newTask.NeedsReview = false;
              newTask.ActiveText = activeText;
              newTask.Start();
              AddTaskToTable(contractualDoc.Id, newTask.Id);
            }
            else
            {
              AddTaskToTable(contractualDoc.Id, 0);
              Logger.DebugFormat("Contractual document {0} has no employees to notify.", contractualDoc.Id);
            }
          }
          catch (Exception ex)
          {
            Logger.ErrorFormat("Contractual document {0} notification failed.", ex, contractualDoc.Id);
          }
        }
        if (IsAllNotificationsStarted())
        {
          UpdateLastNotificationDate(today);
          ClearTable(false);
        }
        session.SubmitChanges();
      }
    }

    /// <summary>
    /// Создать таблицу для хранения информации о договорах и отправленных по ним заданиях.
    /// </summary>
    private static void CreateTableForExpiringContracts()
    {
      var command = Queries.Module.CreateTableForExpiringContracts;
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
    }

    /// <summary>
    /// Получить дату последней рассылки уведомлений.
    /// </summary>
    /// <returns>Дата последней рассылки.</returns>
    private static DateTime GetLastNotificationDate()
    {
      var command = string.Format(Queries.Module.SelectLastNotificationDate, Sungero.Contracts.Constants.Module.NotificationDatabaseKey);
      try
      {
        var executionResult = Sungero.Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
        var date = string.Empty;
        if (!(executionResult is DBNull) && executionResult != null)
          date = executionResult.ToString();
        Logger.DebugFormat("Last notification date in DB is {0}", date);
        
        var result = DateTime.Parse(date);
        return result;
      }
      catch (Exception ex)
      {
        Logger.DebugFormat("Error while getting last notification date {0}", ex);
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
          Logger.Error("Error while getting array of docs with sended task", ex);
          return result;
        }
      }
    }
    
    /// <summary>
    /// Очистить таблицу.
    /// </summary>
    /// <param name="taskIsNull">True, если чистить записи по неотправленным задачам,
    /// False, если чистить записи по отправленным.</param>
    private static void ClearTable(bool taskIsNull)
    {
      string command = string.Empty;
      
      if (taskIsNull)
        command = Queries.Module.DeleteTasksWithoutDocuments;
      else
        command = Queries.Module.DeleteTasksWithDocuments;
      
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
    }
    
    /// <summary>
    /// Записать документы.
    /// </summary>
    /// <param name="ids">Документы.</param>
    private static void AddDocumentsToTable(IEnumerable<int> ids)
    {
      if (!ids.Any())
        return;
      
      var command = string.Format(Queries.Module.AddDocumentsToTable, string.Join(Constants.Module.SeparatorJoin, ids));
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
    }
    
    /// <summary>
    /// Добавить в табличку задачу, с указанием документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="task">Задача, которая была запущена.</param>
    private static void AddTaskToTable(int document, int task)
    {
      var command = string.Format(Queries.Module.AddTask, document, task);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
      Logger.DebugFormat("Task {0} for document {1} started and marked in db.", task, document);
    }
    
    /// <summary>
    /// Проверить, все ли задачи запущены.
    /// </summary>
    /// <returns>True, если все завершено корректно.</returns>
    private static bool IsAllNotificationsStarted()
    {
      var command = Queries.Module.CountNullTasks;
      try
      {
        var executionResult = Sungero.Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
        var result = 0;
        if (!(executionResult is DBNull) && executionResult != null)
          int.TryParse(executionResult.ToString(), out result);
        Logger.DebugFormat("Not sended task count: {0}", result);
        
        return result == 0;
      }
      catch (Exception ex)
      {
        Logger.Error("Error while getting count of not sended task", ex);
        return false;
      }
    }
    
    /// <summary>
    /// Обновить дату последней рассылки уведомлений.
    /// </summary>
    /// <param name="notificationDate">Дата рассылки уведомлений.</param>
    private static void UpdateLastNotificationDate(DateTime notificationDate)
    {
      var newDate = notificationDate.ToString("yyyy-MM-dd HH:mm:ss");
      var command = string.Format(Queries.Module.UpdateLastNotificationDate,
                                  Sungero.Contracts.Constants.Module.NotificationDatabaseKey, newDate);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
      Logger.DebugFormat("Last notification date is set to {0}", newDate);
    }
    
  }
}