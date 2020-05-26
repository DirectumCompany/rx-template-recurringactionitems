using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Metadata.Services;
using Sungero.Domain.Shared;

namespace DirRX.ActionItems.Server
{
  public class ModuleJobs
  {

    #region Очистка папок входящие/исходящие от устаревших ссылок.
    /// <summary>
    /// Фоновый процесс очистки папок входящие/исходящие от устаревших ссылок.
    /// </summary>
    public virtual void CleanInboxOutboxJob()
    {
      var executedStatuses = string.Join(", ", new[] { Sungero.Workflow.AssignmentBase.Status.Completed,
                                           Sungero.Workflow.AssignmentBase.Status.Aborted,
                                           Sungero.Workflow.AssignmentBase.Status.Suspended }.Select(s => string.Format("'{0}'", s)));
      var taskStatuses = string.Join(", ", new[] { Sungero.Workflow.Task.Status.Completed,
                                       Sungero.Workflow.Task.Status.Aborted,
                                       Sungero.Workflow.Task.Status.Suspended }.Select(s => string.Format("'{0}'", s)));

      var noticeMetadata = typeof(Sungero.Workflow.INotice).GetEntityMetadata();
      var noticesGuids = string.Join(", ", noticeMetadata.DescendantEntities.Union(new[] { noticeMetadata }).Select(m => string.Format("'{0}'", m.NameGuid)).Distinct());
      var assignmentMetadata = typeof(Sungero.Workflow.IAssignment).GetEntityMetadata();
      var assignmentsGuids = string.Join(", ", assignmentMetadata.DescendantEntities.Union(new[] { assignmentMetadata }).Select(m => string.Format("'{0}'", m.NameGuid)).Distinct());

      // Удалить ссылки на уведомления.
      int noticesCleanDays = 0;
      int.TryParse(IntegrationLLK.PublicFunctions.Module.Remote.GetServerConfigValue("AUTOCLEAN_NOTICES_CLEAN_DAYS"), out noticesCleanDays);
      var noticesDate = Calendar.Now.AddDays(-1 * noticesCleanDays);
      var noticesFilter = string.Format(DirRX.ActionItems.Queries.Module.NoticesFilter, noticesGuids, executedStatuses);
      DeleteOldLinksFromFolder(Sungero.Workflow.SpecialFolders.InboxId, Sungero.Workflow.Notices.Info, noticesFilter, noticesDate);
      
      // Удалить ссылки на выполненные задачи и задания.
      int executedJobsCleanDays = 0;
      int.TryParse(IntegrationLLK.PublicFunctions.Module.Remote.GetServerConfigValue("AUTOCLEAN_EXECUTED_JOBS_CLEAN_DAYS"), out executedJobsCleanDays);
      var executedJobsDate = Calendar.Now.AddDays(-1 * executedJobsCleanDays);
      var executedJobsFilter = string.Format(DirRX.ActionItems.Queries.Module.CompletedFilter, assignmentsGuids, executedStatuses);
      DeleteOldLinksFromFolder(Sungero.Workflow.SpecialFolders.InboxId, Sungero.Workflow.Assignments.Info, executedJobsFilter, executedJobsDate);
      var tasksFilter = string.Format(DirRX.ActionItems.Queries.Module.TasksFilter, taskStatuses);
      DeleteOldLinksFromFolder(Sungero.Workflow.SpecialFolders.OutboxId, Sungero.Workflow.Tasks.Info, tasksFilter, executedJobsDate);
      
      // Удалить ссылки на просроченные задания.
      int overdueJobsCleanDays = 0;
      int.TryParse(IntegrationLLK.PublicFunctions.Module.Remote.GetServerConfigValue("AUTOCLEAN_OVERDUE_JOBS_CLEAN_DAYS"), out overdueJobsCleanDays);
      var overdueJobsDate = Calendar.Now.AddDays(-1 * overdueJobsCleanDays);
      var inWorkJobsFilter = string.Format(DirRX.ActionItems.Queries.Module.InWorkFilter, assignmentsGuids, executedStatuses);
      DeleteOldLinksFromFolder(Sungero.Workflow.SpecialFolders.InboxId, Sungero.Workflow.Assignments.Info, inWorkJobsFilter, overdueJobsDate);

      // Удалить теги сформированные для папок потоков (для ранее удаленных ссылок).
      CleanSpecialFolderTags(Sungero.Workflow.SpecialFolders.InboxId, Sungero.Workflow.Assignments.Info);
      CleanSpecialFolderTags(Sungero.Workflow.SpecialFolders.OutboxId, Sungero.Workflow.Tasks.Info);
    }
    
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
    #endregion
    #endregion
    
    #region Актуализация головного подразделения у сотрудников.
    /// <summary>
    /// Актуализация головного подразделения в справочнике сотрудники.
    /// </summary>
    public virtual void UpdateEmployeesHeadOffice()
    {
      var changedDepartments = Solution.Departments.GetAll(x => x.IsHeadOfficeChanged == null || x.IsHeadOfficeChanged == true);
      foreach (var department in changedDepartments)
      {
        var employees = department.RecipientLinks.Select(x => x.Member).Where(x => Solution.Employees.Is(x)).ToList().Cast<Solution.IEmployee>();
        foreach (var employee in employees)
        {
          var lockInfo = Locks.GetLockInfo(employee);
          
          if (lockInfo.IsLocked)
          {
            Logger.DebugFormat("Employee with ID={0} is locked", employee.Id);
            continue;
          }
          
          employee.HeadOffice = Solution.Departments.As(employee.Department.HeadOffice);
          employee.Save();
        }
        department.IsHeadOfficeChanged = false;
        department.Save();
      }
    }
    #endregion
    
    #region Рассылка уведомлений новым подписчикам.
    
    /// <summary>
    /// Рассылка уведомлений подписчикам.
    /// </summary>
    public virtual void SendNoticeJob()
    {
      var queueItems = DirRX.ActionItems.SendNoticeQueueItems.GetAll().Where(x => x.Retries == 0).ToList();
      
      // Ошибочные элементы обрабатываются последними пачкой по 25.
      var repetedQueueItems = DirRX.ActionItems.SendNoticeQueueItems.GetAll().Where(x => x.Retries > 0).OrderBy(y => y.Retries).Take(25).ToList();
      queueItems.AddRange(repetedQueueItems);
      
      foreach (var queueItem in queueItems)
      {
        
        #region Добавление подписчика в задание.
        if (queueItem.Task != null && queueItem.Assignees != null)
        {
          #region Задание на исполнение поручения.
          if (DirRX.Solution.ActionItemExecutionTasks.Is(queueItem.Task))
          {
            // Отправка уведомления не зависит от успешности добавления подписчиков в задание, т.к. все процессы подписчиков зависят от задачи.
            if (!queueItem.NoticeIsSend.GetValueOrDefault())
            {
              try
              {
                SendNotice(queueItem);
                queueItem.NoticeIsSend = true;
                queueItem.Save();
              }
              catch (Exception ex)
              {
                // При возникновении ошибок увеличиваем кол-во попыток.
                Transactions.Execute(
                  () =>
                  {
                    Sungero.ExchangeCore.PublicFunctions.QueueItemBase.QueueItemOnError(queueItem, ex.Message);
                  });
                Logger.DebugFormat("{0} Id = '{1}'.", ex.Message, queueItem.Task.Id);
                
                continue;
              }
            }
            
            var actionItemAssignments = Solution.ActionItemExecutionAssignments.GetAll(a => Solution.ActionItemExecutionTasks.Equals(a.Task, queueItem.Task)).ToList();
            
            // Если есть задание в работе, то добавляем подписчиков туда, иначе берём первое, чтобы процесс завершился.
            var actionItemAssignment = Solution.ActionItemExecutionAssignments.Null;
            actionItemAssignment = actionItemAssignments.FirstOrDefault(a => a.Status == Solution.ActionItemExecutionAssignment.Status.InProcess);
            if (actionItemAssignment == null)
              actionItemAssignment = actionItemAssignments.FirstOrDefault();
            
            // Если задание заблокировано, то увеличиваем счётчик попыток, т.к. уведомление уже отправленно.
            if (Locks.GetLockInfo(actionItemAssignment).IsLocked)
            {
              Transactions.Execute(
                () =>
                {
                  Sungero.ExchangeCore.PublicFunctions.QueueItemBase.QueueItemOnError(queueItem, DirRX.ActionItems.Resources.AddSubscriberInJobError);
                });
              Logger.DebugFormat("{0} Id = '{1}'.", DirRX.ActionItems.Resources.AssignmentIsLocked, queueItem.Task.Id);
              
              continue;
            }
            
            var generate = Transactions.Execute(
              () =>
              {
                foreach (var subscriber in queueItem.Assignees)
                {
                  var newSubscriber = actionItemAssignment.Subscribers.AddNew();
                  newSubscriber.Subscriber = subscriber.Subscriber;
                }
                actionItemAssignment.Save();
              });
            
            SetResult(generate, queueItem);
          }
          #endregion

          #region Отправка уведомления в рамках согласования.
          if (DirRX.Solution.ApprovalTasks.Is(queueItem.Task))
          {
            // Отправка уведомления не зависит от успешности добавления подписчиков в задание, т.к. все процессы подписчиков зависят от задачи.
            if (!queueItem.NoticeIsSend.GetValueOrDefault())
            {
              try
              {
                SendNotice(queueItem);
                queueItem.NoticeIsSend = true;
                queueItem.Save();
              }
              catch (Exception ex)
              {
                // При возникновении ошибок увеличиваем кол-во попыток.
                Transactions.Execute(
                  () =>
                  {
                    Sungero.ExchangeCore.PublicFunctions.QueueItemBase.QueueItemOnError(queueItem, ex.Message);
                  });
                Logger.DebugFormat("{0} Id = '{1}'.", ex.Message, queueItem.Task.Id);
                
                continue;
              }
            }
            
            var approvalAssignments = DirRX.Solution.ApprovalAssignments.GetAll(a => Sungero.Workflow.Tasks.Equals(a.Task, queueItem.Task));
            var approvalReworkAssignments = DirRX.Solution.ApprovalReworkAssignments.GetAll(a => Sungero.Workflow.Tasks.Equals(a.Task, queueItem.Task));
            var approvalSigningAssignments = DirRX.Solution.ApprovalSigningAssignments.GetAll(a => Sungero.Workflow.Tasks.Equals(a.Task, queueItem.Task));
            
            var generate = Transactions.Execute(
              () =>
              {
                foreach (var subscriber in queueItem.Assignees)
                {
                  foreach (var approvalAssignment in approvalAssignments)
                  {
                    var approvalAssignmentSub = approvalAssignment.Subscribers.AddNew();
                    approvalAssignmentSub.Subscriber = subscriber.Subscriber;
                  }
                  
                  foreach (var approvalReworkAssignment in approvalReworkAssignments)
                  {
                    var approvalReworkAssignmentSub = approvalReworkAssignment.Subscribers.AddNew();
                    approvalReworkAssignmentSub.Subscriber = subscriber.Subscriber;
                  }
                  
                  foreach (var approvalSigningAssignment in approvalSigningAssignments)
                  {
                    var approvalSigningAssignmentSub = approvalSigningAssignment.Subscribers.AddNew();
                    approvalSigningAssignmentSub.Subscriber = subscriber.Subscriber;
                  }
                }
                
                foreach (var approvalAssignment in approvalAssignments)
                  approvalAssignment.Save();
                
                foreach (var approvalReworkAssignment in approvalReworkAssignments)
                  approvalReworkAssignment.Save();
                
                foreach (var approvalSigningAssignment in approvalSigningAssignments)
                  approvalSigningAssignment.Save();
              });
            
            SetResult(generate, queueItem);
          }
          #endregion
          
        }
        #endregion
        
        #region Добавление подписчика в задачу.
        if (queueItem.Assignment != null && queueItem.Assignees != null)
        {
          #region Задача на исполнение поручения.
          if (DirRX.Solution.ActionItemExecutionAssignments.Is(queueItem.Assignment))
          {
            var actionItemTask = DirRX.Solution.ActionItemExecutionTasks.As(queueItem.Assignment.Task);
            // Если сущность заблокирована, то счётчик попыток не увеличиваем.
            if (Locks.GetLockInfo(actionItemTask).IsLocked)
              continue;
            
            // Операция считается успешной только, если были добавлены подписчики и отправлено уведомление.
            var generate = Transactions.Execute(
              () =>
              {
                // TODO: при одновременном добавлении и в задаче и в задании возможно дублирование.
                foreach (var subscriber in queueItem.Assignees)
                {
                  var newSubscriber = actionItemTask.Subscribers.AddNew();
                  newSubscriber.Subscriber = subscriber.Subscriber;
                }
                actionItemTask.Save();
                SendNotice(queueItem);
              });
            
            SetResult(generate, queueItem);
          }
          #endregion
          
          #region Задача на согласование по регламенту.
          if (DirRX.Solution.ApprovalAssignments.Is(queueItem.Assignment) ||
              DirRX.Solution.ApprovalReworkAssignments.Is(queueItem.Assignment) ||
              DirRX.Solution.ApprovalSigningAssignments.Is(queueItem.Assignment))
          {
            var approvalTask = DirRX.Solution.ApprovalTasks.As(queueItem.Assignment.Task);
            // Если сущность заблокирована, то счётчик попыток не увеличиваем.
            if (Locks.GetLockInfo(approvalTask).IsLocked || CheckLockedRisk(approvalTask))
              continue;
            
            // Операция считается успешной только, если были добавлены подписчики и отправлено уведомление.
            var generate = Transactions.Execute(
              () =>
              {
                // TODO: при одновременном добавлении и в задаче и в задании возможно дублирование.
                foreach (var subscriber in queueItem.Assignees)
                {
                  var newSubscriber = approvalTask.Subscribers.AddNew();
                  newSubscriber.Subscriber = subscriber.Subscriber;
                  approvalTask.AccessRights.Grant(subscriber.Subscriber, new [] {DefaultAccessRightsTypes.Read});
                }
                
                approvalTask.Save();
                SendNotice(queueItem);
              });
            
            SetResult(generate, queueItem);
          }
          #endregion
          
          #region Задача на рассмотрение документа
          if (DirRX.Solution.ReviewManagerAssignments.Is(queueItem.Assignment))
          {
            var docReviewTask = DirRX.Solution.DocumentReviewTasks.As(queueItem.Assignment.Task);
            if (Locks.GetLockInfo(docReviewTask).IsLocked)
              continue;
            
            if (DirRX.Solution.PublicFunctions.DocumentReviewTask.Remote.SendNotificationToSubcribers(docReviewTask,queueItem.Assignees.Select(x => x.Subscriber).ToList()))
            {
              docReviewTask.Save();
              DirRX.ActionItems.SendNoticeQueueItems.Delete(queueItem);
            }
            
          }
          #endregion
          
        }
        #endregion
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

    #region выдача замещений руководителям сотрудников.
    
    /// <summary>
    /// Процесс выдачи замещений руководителям сотрудников.
    /// </summary>
    public virtual void CreateSubstitutions()
    {
      var employees = DirRX.Solution.Employees.GetAll().Where(x => x.NeedUpdateSubtitution == true && x.Status != DirRX.Solution.Employee.Status.Closed);
      foreach (var employee in employees)
      {
        try
        {
          var lockInfo = Locks.GetLockInfo(employee);
          
          if (!lockInfo.IsLocked)
          {
            Solution.PublicFunctions.Employee.Remote.CreateSystemSubstitution(employee, employee.Manager);
            Logger.DebugFormat("Substitutions for employee {0} are created.", employee.Id);
            employee.NeedUpdateSubtitution = false;
            employee.Save();
          }
          else
            Logger.DebugFormat("Employee {0} is locked.", employee.Id);
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("Can not create substitutions for employee {0}. Message: {1}", employee.Id, ex.Message);
        }
      }
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
    
    /// <summary>
    /// Отправка уведомлений по поручениям.
    /// </summary>
    public virtual void SendAssignmentNotices()
    {
      var previousRunDate = GetLastNotificationDate();
      var tasks = DirRX.Solution.ActionItemExecutionTasks.GetAll(t => t.Status == Sungero.Workflow.Task.Status.InProcess &&
                                                                 t.ExecutionState != DirRX.Solution.ActionItemExecutionTask.ExecutionState.OnControl &&
                                                                 t.Deadline.HasValue && t.Started.HasValue);
      
      UpdateLastNotificationDate(Calendar.Now);
      
      foreach (var task in tasks)
      {
        Logger.DebugFormat("Task with Id = {0} in proccess. Started = {1}. Deadline = {2}.", task.Id, task.Started.Value, task.Deadline.Value);
        
        // Просрочено
        if (task.IsExpired)
        {
          Logger.Debug("Task is expired");
          
          if (previousRunDate < task.Deadline.Value)
          {
            try
            {
              DirRX.ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("ExpiredEvent", task);
            }
            catch (Exception ex)
            {
              Logger.Error("Error in ExpiredEvent", ex);
            }
          }
          continue;
        }
        
        // Срок исполнения.
        if (task.Started.Value.Date != task.Deadline.Value.Date && task.Deadline.Value.Date == Calendar.Now.Date && previousRunDate.Date != task.Deadline.Value.Date)
        {
          Logger.Debug("Deadline");
          
          try
          {
            DirRX.ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("DeadlineEvent", task);
          }
          catch (Exception ex)
          {
            Logger.Error("Error in DeadlineEvent", ex);
          }
          continue;
        }
        
        // 20%
        if (IsCorrectPercent(task, 0.2))
        {
          if (!IsSend(task, previousRunDate, 0.2))
          {
            try
            {
              DirRX.ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("TwentyEvent", task);
            }
            catch (Exception ex)
            {
              Logger.Error("Error in TwentyEvent", ex);
            }
          }
          continue;
        }
        
        // 40%
        if (IsCorrectPercent(task, 0.4))
        {
          if (!IsSend(task, previousRunDate, 0.4))
          {
            try
            {
              DirRX.ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("FortyEvent", task);
            }
            catch (Exception ex)
            {
              Logger.Error("Error in FortyEvent", ex);
            }
          }
          continue;
        }
        
        // 60%
        if (IsCorrectPercent(task, 0.6))
        {
          if (!IsSend(task, previousRunDate, 0.6))
          {
            try
            {
              DirRX.ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("SixtyEvent", task);
            }
            catch (Exception ex)
            {
              Logger.Error("Error in SixtyEvent", ex);
            }
          }
          continue;
        }
        
        // 80%
        if (IsCorrectPercent(task, 0.8) && !IsSend(task, previousRunDate, 0.8))
        {
          try
          {
            DirRX.ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("EightyEvent", task);
          }
          catch (Exception ex)
          {
            Logger.Error("Error in EightyEvent", ex);
          }
        }
      }
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

    #region Эскалация.
    
    /// <summary>
    /// Фоновый процесс эскалации поручений.
    /// </summary>
    public virtual void ActionItemExecutionJob()
    {
      
      Logger.Debug("Старт фонового процесса эскалации поручений.");
      var activeTask = Solution.ActionItemExecutionTasks.GetAll(x => (x.ExecutionState == Solution.ActionItemExecutionTask.ExecutionState.OnExecution ||
                                                                      x.ExecutionState == Solution.ActionItemExecutionTask.ExecutionState.OnRework) &&
                                                                x.Priority != null && x.Status == Solution.ActionItemExecutionTask.Status.InProcess);
      foreach (var task in activeTask)
      {
        Logger.Debug(string.Format("Проверка необходимости эскалации задачи {0}.", task.Id));
        var lastEscalatedTask = DirRX.ActionItems.ActionItemEscalatedTasks.GetAll().ToList()
          .Where(x => x.AttachmentGroup.ActionItemExecutionAssignments.Count > 0 &&
                 DirRX.Solution.ActionItemExecutionTasks
                 .Equals(task, DirRX.Solution.ActionItemExecutionTasks.As(x.AttachmentGroup.ActionItemExecutionAssignments.FirstOrDefault().Task)))
          .OrderBy(x => x.Started).LastOrDefault();
        
        // TODO: Дублирование логики из функции GetEscalationReason.
        var endDate = task.FinalDeadline.HasValue ? task.FinalDeadline.Value : task.Deadline.Value;
        var residualPeriod = WorkingTime.GetDurationInWorkingHours(Calendar.Now, endDate);
        
        //Если нет запросов на продление срока, не MainTask для составных и не поручение соисполнителю и задача не была рестартована после последней эскалации.
        if (!task.Subtasks.Any(x => Sungero.RecordManagement.DeadlineExtensionTasks.Is(x)) &&
            !task.ActionItemParts.Any() && task.ActionItemType != Solution.ActionItemExecutionTask.ActionItemType.Additional &&
            (lastEscalatedTask == null /*|| (lastEscalatedTask != null && task.Started > lastEscalatedTask.Started)*/) &&  //TODO: Отключена повторная эскалация.
            residualPeriod > (task.Priority.EscalationPeriodWorkDays ?? -1))
        {
          var reason = DirRX.ActionItems.PublicFunctions.Module.GetEscalationReason(task);
          Logger.Debug(string.Format("Причина эскалации: {0}.", reason));
          if (!string.IsNullOrEmpty(reason))
          {
            var manager = PublicFunctions.Module.Remote.GetEscalatedManager(task);
            
            if (manager != null)
            {
              Logger.DebugFormat("manager: {0}.", manager.Name);
              
              if (DirRX.ActionItems.PublicFunctions.Module.EscalateActionItemTask(task, reason, manager))
                Logger.Debug(string.Format("Уведомление о задаче {0} отправлено сотруднику: {1}.", task, manager));
            }
            else
              Logger.Debug("manager is NULL");
          }
        }
      }
      Logger.Debug("Завершение фонового процесса эскалации поручений.");
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
    
    public virtual void SynchronizeNoticeSettings()
    {
      var allUsersSettings = NoticeSettings.GetAll().ToList().Where(s => s.AllUsersFlag.GetValueOrDefault());
      
      foreach (var setting in allUsersSettings)
      {
        var userSettings = NoticeSettings.GetAll().ToList().Where(s => ActionItemsRoles.Equals(s.AssgnRole, setting.AssgnRole));
        Logger.DebugFormat("Setting with role = {0} in proccess", setting.AssgnRole.DisplayValue);
        
        foreach (var userSetting in userSettings)
        {
          var lockInfo = Locks.GetLockInfo(userSetting);
          
          if (lockInfo.IsLocked)
          {
            Logger.DebugFormat("Notice setting with ID={0} is locked", userSetting.Id);
            continue;
          }
          
          #region Обработка реквизитов.
          var y = userSetting.AAbortsPriority.Select(s => s.Priority);
          // Проверяем изменение неотключаемости настройки.
          if (userSetting.IsAAbortsRequired.GetValueOrDefault() != setting.IsAAbortsRequired.GetValueOrDefault() ||
              userSetting.AAbortsPriority.Count() != setting.AAbortsPriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.AAbortsPriority.Select(s => s.Priority), setting.AAbortsPriority.Select(s => s.Priority)))
          {
            userSetting.IsAAbortsRequired = setting.IsAAbortsRequired.GetValueOrDefault();
            
            // Если настройка изменилась на неотключаемую, то меняем соответствувющие параметры, иначе только даём возможность изменения.
            if (setting.IsAAbortsRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentAborts = setting.IsAssignmentAborts.GetValueOrDefault();
              
              userSetting.AAbortsPriority.Clear();
              foreach (var priority in setting.AAbortsPriority)
                userSetting.AAbortsPriority.AddCopy(priority);
            }
          }
          
          if (userSetting.IsAAcceptRequired.GetValueOrDefault() != setting.IsAAcceptRequired.GetValueOrDefault() ||
              userSetting.AAcceptPriority.Count() != setting.AAcceptPriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.AAcceptPriority.Select(s => s.Priority), setting.AAcceptPriority.Select(s => s.Priority)))
          {
            userSetting.IsAAcceptRequired = setting.IsAAcceptRequired.GetValueOrDefault();
            
            if (setting.IsAAcceptRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentAccept = setting.IsAssignmentAccept.GetValueOrDefault();
              
              userSetting.AAcceptPriority.Clear();
              foreach (var priority in setting.AAcceptPriority)
                userSetting.AAcceptPriority.AddCopy(priority);
            }
          }
          
          if (userSetting.IsAAddTimeRequired.GetValueOrDefault() != setting.IsAAddTimeRequired.GetValueOrDefault() ||
              userSetting.AAddTimePriority.Count() != setting.AAddTimePriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.AAddTimePriority.Select(s => s.Priority), setting.AAddTimePriority.Select(s => s.Priority)))
          {
            userSetting.IsAAddTimeRequired = setting.IsAAddTimeRequired.GetValueOrDefault();
            
            if (setting.IsAAddTimeRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentAddTime = setting.IsAssignmentAddTime.GetValueOrDefault();
              
              userSetting.AAddTimePriority.Clear();
              foreach (var priority in setting.AAddTimePriority)
                userSetting.AAddTimePriority.AddCopy(priority);
            }
          }
          
          if (userSetting.IsADeadlineRequired.GetValueOrDefault() != setting.IsADeadlineRequired.GetValueOrDefault() ||
              userSetting.ADeadlinePriority.Count() != setting.ADeadlinePriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.ADeadlinePriority.Select(s => s.Priority), setting.ADeadlinePriority.Select(s => s.Priority)))
          {
            userSetting.IsADeadlineRequired = setting.IsADeadlineRequired.GetValueOrDefault();
            
            if (setting.IsADeadlineRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentDeadline = setting.IsAssignmentDeadline.GetValueOrDefault();
              
              userSetting.ADeadlinePriority.Clear();
              foreach (var priority in setting.ADeadlinePriority)
                userSetting.ADeadlinePriority.AddCopy(priority);
            }
          }
          
          if (userSetting.IsADeclinedRequired.GetValueOrDefault() != setting.IsADeclinedRequired.GetValueOrDefault() ||
              userSetting.ADeclinedPriority.Count() != setting.ADeclinedPriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.ADeclinedPriority.Select(s => s.Priority), setting.ADeclinedPriority.Select(s => s.Priority)))
          {
            userSetting.IsADeclinedRequired = setting.IsADeclinedRequired.GetValueOrDefault();
            
            if (setting.IsADeclinedRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentDeclined = setting.IsAssignmentDeclined.GetValueOrDefault();
              
              userSetting.ADeclinedPriority.Clear();
              foreach (var priority in setting.ADeclinedPriority)
                userSetting.ADeclinedPriority.AddCopy(priority);
            }
          }
          
          if (userSetting.IsAEightyRequired.GetValueOrDefault() != setting.IsAEightyRequired.GetValueOrDefault() ||
              userSetting.AEightyPriority.Count() != setting.AEightyPriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.AEightyPriority.Select(s => s.Priority), setting.AEightyPriority.Select(s => s.Priority)))
          {
            userSetting.IsAEightyRequired = setting.IsAEightyRequired.GetValueOrDefault();
            
            if (setting.IsAEightyRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentEighty = setting.IsAssignmentEighty.GetValueOrDefault();
              
              userSetting.AEightyPriority.Clear();
              foreach (var priority in setting.AEightyPriority)
                userSetting.AEightyPriority.AddCopy(priority);
            }
          }
          
          if (userSetting.IsAEscalatedRequired.GetValueOrDefault() != setting.IsAEscalatedRequired.GetValueOrDefault() ||
              userSetting.AEscalatedPriority.Count() != setting.AEscalatedPriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.AEscalatedPriority.Select(s => s.Priority), setting.AEscalatedPriority.Select(s => s.Priority)))
          {
            userSetting.IsAEscalatedRequired = setting.IsAEscalatedRequired.GetValueOrDefault();
            
            if (setting.IsAEscalatedRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentEscalated = setting.IsAssignmentEscalated.GetValueOrDefault();
              
              userSetting.AEscalatedPriority.Clear();
              foreach (var priority in setting.AEscalatedPriority)
                userSetting.AEscalatedPriority.AddCopy(priority);
            }
          }
          
          if (userSetting.IsAExpiredRequired.GetValueOrDefault() != setting.IsAExpiredRequired.GetValueOrDefault() ||
              userSetting.AExpiredPriority.Count() != setting.AExpiredPriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.AExpiredPriority.Select(s => s.Priority), setting.AExpiredPriority.Select(s => s.Priority)))
          {
            userSetting.IsAExpiredRequired = setting.IsAExpiredRequired.GetValueOrDefault();
            
            if (setting.IsAExpiredRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentExpired = setting.IsAssignmentExpired.GetValueOrDefault();
              
              userSetting.AExpiredPriority.Clear();
              foreach (var priority in setting.AExpiredPriority)
                userSetting.AExpiredPriority.AddCopy(priority);
            }
          }
          
          if (userSetting.IsAFortyRequired.GetValueOrDefault() != setting.IsAFortyRequired.GetValueOrDefault() ||
              userSetting.AFortyPriority.Count() != setting.AFortyPriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.AFortyPriority.Select(s => s.Priority), setting.AFortyPriority.Select(s => s.Priority)))
          {
            userSetting.IsAFortyRequired = setting.IsAFortyRequired.GetValueOrDefault();
            
            if (setting.IsAFortyRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentForty = setting.IsAssignmentForty.GetValueOrDefault();
              
              userSetting.AFortyPriority.Clear();
              foreach (var priority in setting.AFortyPriority)
                userSetting.AFortyPriority.AddCopy(priority);
            }
          }
          
          if (userSetting.IsANewSubjRequired.GetValueOrDefault() != setting.IsANewSubjRequired.GetValueOrDefault() ||
              userSetting.ANewSubjPriority.Count() != setting.ANewSubjPriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.ANewSubjPriority.Select(s => s.Priority), setting.ANewSubjPriority.Select(s => s.Priority)))
          {
            userSetting.IsANewSubjRequired = setting.IsANewSubjRequired.GetValueOrDefault();
            
            if (setting.IsANewSubjRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentNewSubj = setting.IsAssignmentNewSubj.GetValueOrDefault();
              
              userSetting.ANewSubjPriority.Clear();
              foreach (var priority in setting.ANewSubjPriority)
                userSetting.ANewSubjPriority.AddCopy(priority);
            }
          }
          
          if (userSetting.IsAOnControlRequired.GetValueOrDefault() != setting.IsAOnControlRequired.GetValueOrDefault() ||
              userSetting.AOnControlPriority.Count() != setting.AOnControlPriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.AOnControlPriority.Select(s => s.Priority), setting.AOnControlPriority.Select(s => s.Priority)))
          {
            userSetting.IsAOnControlRequired = setting.IsAOnControlRequired.GetValueOrDefault();
            
            if (setting.IsAOnControlRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentOnControl = setting.IsAssignmentOnControl.GetValueOrDefault();
              
              userSetting.AOnControlPriority.Clear();
              foreach (var priority in setting.AOnControlPriority)
                userSetting.AOnControlPriority.AddCopy(priority);
            }
          }

          if (userSetting.IsAOnControlRequired.GetValueOrDefault() != setting.IsAOnControlRequired.GetValueOrDefault() ||
              userSetting.AOnControlPriority.Count() != setting.AOnControlPriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.AOnControlPriority.Select(s => s.Priority), setting.AOnControlPriority.Select(s => s.Priority)))
          {
            userSetting.IsAOnControlRequired = setting.IsAOnControlRequired.GetValueOrDefault();
            
            if (setting.IsAOnControlRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentOnControl = setting.IsAssignmentOnControl.GetValueOrDefault();
              
              userSetting.AOnControlPriority.Clear();
              foreach (var priority in setting.AOnControlPriority)
                userSetting.AOnControlPriority.AddCopy(priority);
            }
          }
          
          if (userSetting.IsAPerformRequired.GetValueOrDefault() != setting.IsAPerformRequired.GetValueOrDefault() ||
              userSetting.APerformPriority.Count() != setting.APerformPriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.APerformPriority.Select(s => s.Priority), setting.APerformPriority.Select(s => s.Priority)))
          {
            userSetting.IsAPerformRequired = setting.IsAPerformRequired.GetValueOrDefault();
            
            if (setting.IsAPerformRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentPerform = setting.IsAssignmentPerform.GetValueOrDefault();
              
              userSetting.APerformPriority.Clear();
              foreach (var priority in setting.APerformPriority)
                userSetting.APerformPriority.AddCopy(priority);
            }
          }
          
          if (userSetting.IsARevisionRequired.GetValueOrDefault() != setting.IsARevisionRequired.GetValueOrDefault() ||
              userSetting.ARevisionPriority.Count() != setting.ARevisionPriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.ARevisionPriority.Select(s => s.Priority), setting.ARevisionPriority.Select(s => s.Priority)))
          {
            userSetting.IsARevisionRequired = setting.IsARevisionRequired.GetValueOrDefault();
            
            if (setting.IsARevisionRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentRevision = setting.IsAssignmentRevision.GetValueOrDefault();
              
              userSetting.ARevisionPriority.Clear();
              foreach (var priority in setting.ARevisionPriority)
                userSetting.ARevisionPriority.AddCopy(priority);
            }
          }
          
          if (userSetting.IsAReworkRequired.GetValueOrDefault() != setting.IsAReworkRequired.GetValueOrDefault() ||
              userSetting.AReworkPriority.Count() != setting.AReworkPriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.AReworkPriority.Select(s => s.Priority), setting.AReworkPriority.Select(s => s.Priority)))
          {
            userSetting.IsAReworkRequired = setting.IsAReworkRequired.GetValueOrDefault();
            
            if (setting.IsAReworkRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentRework = setting.IsAssignmentRework.GetValueOrDefault();
              
              userSetting.AReworkPriority.Clear();
              foreach (var priority in setting.AReworkPriority)
                userSetting.AReworkPriority.AddCopy(priority);
            }
          }

          if (userSetting.IsASixtyRequired.GetValueOrDefault() != setting.IsASixtyRequired.GetValueOrDefault() ||
              userSetting.ASixtyPriority.Count() != setting.ASixtyPriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.ASixtyPriority.Select(s => s.Priority), setting.ASixtyPriority.Select(s => s.Priority)))
          {
            userSetting.IsASixtyRequired = setting.IsASixtyRequired.GetValueOrDefault();
            
            if (setting.IsASixtyRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentSixty = setting.IsAssignmentSixty.GetValueOrDefault();
              
              userSetting.ASixtyPriority.Clear();
              foreach (var priority in setting.ASixtyPriority)
                userSetting.ASixtyPriority.AddCopy(priority);
            }
          }
          
          if (userSetting.IsAStartsRequired.GetValueOrDefault() != setting.IsAStartsRequired.GetValueOrDefault() ||
              userSetting.AStartsPriority.Count() != setting.AStartsPriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.AStartsPriority.Select(s => s.Priority), setting.AStartsPriority.Select(s => s.Priority)))
          {
            userSetting.IsAStartsRequired = setting.IsAStartsRequired.GetValueOrDefault();
            
            if (setting.IsAStartsRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentStarts = setting.IsAssignmentStarts.GetValueOrDefault();
              
              userSetting.AStartsPriority.Clear();
              foreach (var priority in setting.AStartsPriority)
                userSetting.AStartsPriority.AddCopy(priority);
            }
          }
          
          if (userSetting.IsATimeAcceptRequired.GetValueOrDefault() != setting.IsATimeAcceptRequired.GetValueOrDefault() ||
              userSetting.ATimeAcceptPriority.Count() != setting.ATimeAcceptPriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.ATimeAcceptPriority.Select(s => s.Priority), setting.ATimeAcceptPriority.Select(s => s.Priority)))
          {
            userSetting.IsATimeAcceptRequired = setting.IsATimeAcceptRequired.GetValueOrDefault();
            
            if (setting.IsATimeAcceptRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentTimeAccept = setting.IsAssignmentTimeAccept.GetValueOrDefault();
              
              userSetting.ATimeAcceptPriority.Clear();
              foreach (var priority in setting.ATimeAcceptPriority)
                userSetting.ATimeAcceptPriority.AddCopy(priority);
            }
          }
          
          if (userSetting.IsATwentyRequired.GetValueOrDefault() != setting.IsATwentyRequired.GetValueOrDefault() ||
              userSetting.ATwentyPriority.Count() != setting.ATwentyPriority.Count() ||
              !IsEqualsPriorityCollection(userSetting.ATwentyPriority.Select(s => s.Priority), setting.ATwentyPriority.Select(s => s.Priority)))
          {
            userSetting.IsATwentyRequired = setting.IsATwentyRequired.GetValueOrDefault();
            
            if (setting.IsATwentyRequired.GetValueOrDefault())
            {
              userSetting.IsAssignmentTwenty = setting.IsAssignmentTwenty.GetValueOrDefault();
              
              userSetting.ATwentyPriority.Clear();
              foreach (var priority in setting.ATwentyPriority)
                userSetting.ATwentyPriority.AddCopy(priority);
            }
          }
          
          #endregion
          
          if (userSetting.State.IsChanged)
            userSetting.Save();
        }
      }
    }
    
    #endregion
  }
}