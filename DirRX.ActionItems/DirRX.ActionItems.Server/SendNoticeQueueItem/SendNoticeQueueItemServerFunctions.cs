using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.SendNoticeQueueItem;

namespace DirRX.ActionItems.Server
{
  partial class SendNoticeQueueItemFunctions
  {
    
    /// <summary>
    /// Добавление в очередь элемента для отправки уведомления.
    /// </summary>
    /// <param name="subscribers">Новые подписчики.</param>
    /// <param name="task">Задача.</param>
    /// <param name="assignment">Задание.</param>
    /// <returns>True, если создан элемент очереди.</returns>
    [Public, Remote]
    public static bool CreateSendNoticeQueueItem(List<DirRX.Solution.IEmployee> subscribers, Sungero.Workflow.ITask task, Sungero.Workflow.IAssignment assignment)
    {
      var sendNoticeQueueItem = SendNoticeQueueItems.Create();
      
      foreach (var subscriber in subscribers)
      {
        var noticePerformer = sendNoticeQueueItem.Assignees.AddNew();
        noticePerformer.Subscriber = subscriber;
      }
      
      sendNoticeQueueItem.Retries = 0;
      sendNoticeQueueItem.ProcessingStatus = DirRX.ActionItems.SendNoticeQueueItem.ProcessingStatus.NotProcessed;
      
      if (task != null)
      {
        sendNoticeQueueItem.Subject = DirRX.ActionItems.SendNoticeQueueItems.Resources.NewSubscriberFormat(task.Subject);
        sendNoticeQueueItem.Task = task;
      }
      
      if (assignment != null)
      {
        sendNoticeQueueItem.Subject = DirRX.ActionItems.SendNoticeQueueItems.Resources.NewSubscriberFormat(assignment.Task.Subject);
        sendNoticeQueueItem.Assignment = assignment;
      }
      
      sendNoticeQueueItem.SendAsSubTask = false;
      
      try
      {
        sendNoticeQueueItem.Save();
        // Принудительный запуск процесса для рассылки уведомлений.                
        DirRX.ActionItems.Jobs.SendNoticeJob.Enqueue();
      }
      catch (Exception ex)
      {
        if (task != null)
          Logger.ErrorFormat("Error with task ID = {0}, message: {1}", task.Id, ex.Message);
        
        if (assignment != null)
          Logger.ErrorFormat("Error with assignment ID = {0}, message: {1}", assignment.Id, ex.Message);
        
        return false;
      }
      
      return true;
    }    
  }
}