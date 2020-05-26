using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.LocalActs.Server
{
  public class ModuleJobs
  {

    /// <summary>
    /// Уведомление исполнителя о не предоставлении ответа в рамках задачи по согласованию ЛНА. 
    /// </summary>
    public virtual void RequestInitiatorActuallyJob()
    {
      var previousRunDate = Functions.Module.GetLastNotificationDate(Constants.Module.RequestInitiatorActuallyNoticeDateTimeDocflowParamName);
      var startTime = Calendar.Now;
      var assignments = LocalActs.RequestInitiatorAssingments.GetAll(x => x.Status == Solution.ApprovalReworkAssignment.Status.InProcess &&
                                                                     x.Created.Value.Date <= Calendar.Today.AddDays(-30));
      if (previousRunDate < Calendar.Today)
        assignments = assignments.Where(x => x.Created.Value.Date > previousRunDate.AddDays(-30));
      
      foreach (var assignment in assignments)
      {
        var performers = new List<IRecipient>();
        performers.Add(assignment.Performer);
        var notice = Sungero.Workflow.SimpleTasks.CreateWithNotices(DirRX.LocalActs.Resources.DocumentReworkActuallyNotice,
                                                                    performers, new[] {assignment});
        notice.Save();
        notice.Start();
      }
      Functions.Module.UpdateLastNotificationDate(Constants.Module.RequestInitiatorActuallyNoticeDateTimeDocflowParamName, startTime);
    }

    /// <summary>
    /// Уведомление исполнителя о нахождении документа на доработке более 30 дней.
    /// </summary>
    public virtual void DocumentReworkActuallyJob()
    {
      var previousRunDate = Functions.Module.GetLastNotificationDate(Constants.Module.LastNoticeSendDateTimeDocflowParamName);
      var startTime = Calendar.Now;
      var assignments = Solution.ApprovalReworkAssignments.GetAll(x => x.Status == Solution.ApprovalReworkAssignment.Status.InProcess &&
                                                                  x.Created.Value.Date <= Calendar.Today.AddDays(-30));
      if (previousRunDate < Calendar.Today)
        assignments = assignments.Where(x => x.Created.Value.Date > previousRunDate.AddDays(-30));
      
      foreach (var assignment in assignments)
      {
        var performers = new List<IRecipient>();
        performers.Add(assignment.Performer);
        var notice = Sungero.Workflow.SimpleTasks.CreateWithNotices(DirRX.LocalActs.Resources.DocumentReworkActuallyNotice,
                                                                    performers, new[] {assignment});
        notice.Save();
        notice.Start();
      }
      Functions.Module.UpdateLastNotificationDate(Constants.Module.LastNoticeSendDateTimeDocflowParamName, startTime);
    }
    
  
    /// <summary>
    /// Продление срока согласования
    /// </summary>
    public virtual void ApprorovalDedlineIncrementJob()
    {
      Logger.DebugFormat("Старт фонового процесса: \"Продление срока согласования\".");
      var tasks = LocalActs.RequestInitiatorTasks.GetAll(p => p.Status == LocalActs.RequestInitiatorTask.Status.InProcess);
      
      foreach (var task in tasks)
      {
        try
        {
          Logger.Debug("RequestInitiatorTasksID: " + task.Id);
          
          var parentAssigment = task.ParentAssignment;
          var hoursInWork = WorkingTime.GetDurationInWorkingHours(task.Created.Value, Calendar.Now, parentAssigment.Performer);
          
          //Вычисление дедлайна относительно текущего времени с поправкой на количество дней между стартом задания и отправкой задачи инициатору.
          parentAssigment.Deadline = task.MainAssignmentDefaultDeadline.Value.AddWorkingHours(parentAssigment.Performer, hoursInWork);
          parentAssigment.Save();
          Logger.Debug(string.Format("RequestInitiatorTasksID: {0}. ParentAssigment new deadline {1}", task.Id, parentAssigment.Deadline));
          
          var approvalTask = Solution.ApprovalTasks.As(parentAssigment.MainTask);
          // Обновить срок у задачи на согласование по регламенту.
          if (approvalTask != null)
          {
            approvalTask.MaxDeadline = Solution.PublicFunctions.ApprovalTask.Remote.GetExpectedDate(approvalTask);
            approvalTask.Save();
            Logger.Debug(string.Format("RequestInitiatorTasksID: {0}. ApprovalTask new deadline {1}", task.Id, approvalTask.MaxDeadline));
          }
          
          Logger.Debug(string.Format("RequestInitiatorTasksID: {0} completed. Duration working hours {1}", task.Id, hoursInWork.ToString()));
        }
        catch(Exception ex)
        {
          Logger.Debug(string.Format("RequestInitiatorTasksID: {0} error: {1}",task.Id, ex.Message));
        }
      }
      
      Logger.DebugFormat("Выполнение фонового процесса: \"Продление срока согласования\" завершено!.");
    }
    
    /// <summary>
    /// Изменение состояния приказа и регламетриющего документа.
    /// </summary>
    public virtual void ChangeStatusOrderJob()
    {
      Logger.DebugFormat("Старт фонового процесса: \"Изменение состояния приказа и регламетриющего документа\".");
      var orders = DirRX.Solution.Orders.GetAll(p => p.LifeCycleState != DirRX.Solution.Order.LifeCycleState.Obsolete &&
                                                p.RevokeDate.HasValue &&
                                                Calendar.Today >= p.RevokeDate.Value);
      
      var countChangeO = uint.MinValue;
      
      foreach (var order in orders)
      {
        try
        {
          var lockInfo = Locks.GetLockInfo(order);
          var isLockedByOther = lockInfo != null && lockInfo.IsLocked;
          if (!isLockedByOther)
          {
            order.LifeCycleState = DirRX.Solution.Order.LifeCycleState.Obsolete;
            order.Save();
            countChangeO++;
          }
        }
        catch (Exception ex)
        {
          Logger.DebugFormat("Во время изменения статуса элемента коллекции приказов по номером {0} - произошла ошибка: {1}", countChangeO, ex.Message);
        }
      }

      Logger.DebugFormat("Изменен статус в {0} приказах.", countChangeO);
      
      var regulatoryDocuments = DirRX.LocalActs.RegulatoryDocuments.GetAll(p => p.LifeCycleState != DirRX.LocalActs.RegulatoryDocument.LifeCycleState.Obsolete &&
                                                                           p.EndDate.HasValue &&
                                                                           Calendar.Today >= p.EndDate.Value);
      var countChangeR = uint.MinValue;
      
      foreach (var regulatoryDocument in regulatoryDocuments)
      {
        try
        {
          var lockInfo = Locks.GetLockInfo(regulatoryDocument);
          var isLockedByOther = lockInfo != null && lockInfo.IsLocked;
          if (!isLockedByOther)
          {
            regulatoryDocument.LifeCycleState = DirRX.LocalActs.RegulatoryDocument.LifeCycleState.Obsolete;
            regulatoryDocument.Save();
            countChangeR++;
          }
        }
        catch (Exception ex)
        {
          Logger.DebugFormat("Во время изменения статуса элемента коллекции регламентирующих документов по номером {0} - произошла ошибка: {1}", countChangeR, ex.Message);
        }
      }
      

      Logger.DebugFormat("Выполнение фонового процесса: \"Изменение состояния приказа и регламетриющего документа\" закончено. Изменен статус у {0} элементов.", countChangeO + countChangeR);
    }
  }
}