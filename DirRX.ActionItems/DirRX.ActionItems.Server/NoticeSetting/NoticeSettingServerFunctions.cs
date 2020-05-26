using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.NoticeSetting;

namespace DirRX.ActionItems.Server
{
  partial class NoticeSettingFunctions
  {
    
    /// <summary>
    /// Получить настройку "для всех пользователей" для роли.
    /// </summary>
    /// <param name="role">Роль.</param>
    /// <returns>Настройка.</returns>
    [Remote(IsPure = true), Public]
    public static INoticeSetting GetDefaultSetting(IActionItemsRole role)
    {
      return NoticeSettings.GetAll(s => ActionItemsRoles.Equals(s.AssgnRole, role) && s.AllUsersFlag.HasValue && s.AllUsersFlag.Value).FirstOrDefault();
    }
    
    /// <summary>
    /// Получить похожую запись настроек.
    /// </summary>
    /// <param name="setting">Создаваемая настройка.</param>
    /// <returns>Настройка.</returns>
    [Remote(IsPure = true)]
    public INoticeSetting GetSameSetting()
    {
      return NoticeSettings.GetAll(s => ActionItemsRoles.Equals(s.AssgnRole, _obj.AssgnRole) &&
                                   DirRX.Solution.Employees.Equals(s.Employee, _obj.Employee) &&
                                   !NoticeSettings.Equals(s, _obj)).FirstOrDefault();
    }
    
    /// <summary>
    /// Проверить наличие записи настроек, где текущий пользователь исполнитель той же роли.
    /// </summary>
    /// <param name="role">Роль.</param>
    /// <returns>Признак наличия записи.</returns>
    [Remote(IsPure = true)]
    public bool IsSameRoleSettingExists(IActionItemsRole role)
    {
      if (_obj.Employee == null)
        return NoticeSettings.GetAll(s => ActionItemsRoles.Equals(s.AssgnRole, role) &&
                                     s.AllUsersFlag.HasValue &&
                                     s.AllUsersFlag == true &&
                                     !NoticeSettings.Equals(s, _obj)).Any();
      else
        return NoticeSettings.GetAll(s => ActionItemsRoles.Equals(s.AssgnRole, role) &&
                                     DirRX.Solution.Employees.Equals(s.Employee, _obj.Employee) &&
                                     !NoticeSettings.Equals(s, _obj)).Any();
    }
    
    #region Отправка уведомлений.
    
    /// <summary>
    /// Получить сотрудников для отправки уведомлений.
    /// </summary>
    /// <param name="assignmentEvent">Событие.</param>
    /// <param name="task">Поручение.</param>
    /// <returns>Список сотрудников (словарь: Key - сотрудник, Value - Роль).</returns>
    [Public]
    public static System.Collections.Generic.Dictionary<DirRX.Solution.IEmployee, Enumeration> GetPerformersForEvent(string assignmentEvent, DirRX.Solution.IActionItemExecutionTask task)
    {
      var performers = new Dictionary<DirRX.Solution.IEmployee, Enumeration>();
      // Постановщик.
      AddPerformer(performers, task.Initiator, DirRX.ActionItems.ActionItemsRole.Type.Commissioner, task.Priority, assignmentEvent);
      // Контролер.
      AddPerformer(performers, DirRX.Solution.Employees.As(task.Supervisor), DirRX.ActionItems.ActionItemsRole.Type.Controler, task.Priority, assignmentEvent);
      
      // Исполнители и непосредственные руководители.
      if (!task.IsCompoundActionItem.GetValueOrDefault())
      {
        AddPerformer(performers, DirRX.Solution.Employees.As(task.Assignee), DirRX.ActionItems.ActionItemsRole.Type.Performer, task.Priority, assignmentEvent);
        AddPerformer(performers,
                     Functions.ActionItemsRole.GetRolePerformer(DirRX.ActionItems.ActionItemsRoles.GetAll(r => r.Type == DirRX.ActionItems.ActionItemsRole.Type.InitManager).FirstOrDefault(),
                                                                DirRX.Solution.Employees.As(task.Assignee)),
                     DirRX.ActionItems.ActionItemsRole.Type.InitManager, task.Priority, assignmentEvent);
      }
      
      // Подписчики.
      foreach (var subscriber in task.Subscribers.Select(s => s.Subscriber))
        AddPerformer(performers, subscriber, DirRX.ActionItems.ActionItemsRole.Type.Subscriber, task.Priority, assignmentEvent);
      
      // ГД.
      if (task.ParentTask == null && task.ParentAssignment == null)
      {
        var CEO = DirRX.Solution.Employees.Null;
        if (task.IsCompoundActionItem.GetValueOrDefault())
          CEO = Functions.ActionItemsRole.GetRolePerformer(DirRX.ActionItems.ActionItemsRoles.GetAll(r => r.Type == DirRX.ActionItems.ActionItemsRole.Type.CEO).FirstOrDefault(), task.Initiator);
        else
          CEO = Functions.ActionItemsRole.GetRolePerformer(
            DirRX.ActionItems.ActionItemsRoles.GetAll(r => r.Type == DirRX.ActionItems.ActionItemsRole.Type.CEO).FirstOrDefault(), DirRX.Solution.Employees.As(task.Assignee));
        
        if (DirRX.Solution.Employees.Equals(CEO, task.Initiator) || 
            DirRX.Solution.Employees.Equals(CEO, task.Assignee) || 
            DirRX.Solution.Employees.Equals(CEO, task.Supervisor) || 
            DirRX.Solution.Employees.Equals(CEO, task.AssignedBy))
          AddPerformer(performers, CEO, DirRX.ActionItems.ActionItemsRole.Type.CEO, task.Priority, assignmentEvent);
      }
      
      var escalatedTo = PublicFunctions.Module.Remote.GetEscalatedManager(task);
      
      // Руководитель по эскалации.
      if (task.IsEscalated.GetValueOrDefault())
        AddPerformer(performers, escalatedTo, DirRX.ActionItems.ActionItemsRole.Type.InitManager, task.Priority, assignmentEvent);
      
      return performers;
    }
    
    /// <summary>
    /// Отправить уведомления по событию в поручении.
    /// </summary>
    /// <param name="assignmentEvent">Название события.</param>
    /// <param name="assignment">Задание на исполнение.</param>
    [Public]
    public static void CollectAndSendNoticesByEvent(string assignmentEvent, DirRX.Solution.IActionItemExecutionTask task)
    {
      var performers = GetPerformersForEvent(assignmentEvent, task);
      var escalatedTo = PublicFunctions.Module.Remote.GetEscalatedManager(task);
      
      #region Отправка уведомлений.
      
      if (performers.Any())
      {
        string defaultEventDescription = Functions.NoticeSetting.GetNoticeDescriptionByEvent(assignmentEvent);
        
        var serviceUsersRole = Sungero.CoreEntities.Roles.GetAll(x => x.Sid == Sungero.Domain.Shared.SystemRoleSid.ServiceUsers).FirstOrDefault();
        var serviceUser = serviceUsersRole.RecipientLinks.Where(x => x.Member.Name == "Service User").FirstOrDefault().Member;
        
        // В зависимости от роли тема уведомления может быть разной.
        // Группируем по роли и формируем уведомление с нужной темой отдельно для сотрудников каждой роли.
        // Если для роли не нужно формировать отдельную тему, то используем по-умолчанию.
        switch (assignmentEvent)
        {
          case "StartEvent":
            foreach(var group in performers.GroupBy(p => p.Value))
            {
              if (group.Key == DirRX.ActionItems.ActionItemsRole.Type.Subscriber)
              {
                SendNotice(task, NoticeSettings.Resources.YouAreSubscriber, group.Select(g => g.Key).ToList(), serviceUser, string.Empty, false);
                continue;
              }
              
              // Контролеру будет приходить уведомление базового решения.
              if (group.Key == DirRX.ActionItems.ActionItemsRole.Type.Controler)
                continue;
              
              SendNotice(task, defaultEventDescription, group.Select(g => g.Key).ToList(), serviceUser, string.Empty, false);
            }
            break;
          case "ActionItemChangedEvent":
            var rejectionAssignment = ActionItemRejectionAssignments.GetAll().ToList()
              .LastOrDefault(a => Solution.ActionItemExecutionTasks.Equals(ActionItemRejectionTasks.As(a.Task).ActionItemExecutionTask, task));
            
            var activeText = string.Empty;
            if (rejectionAssignment != null)
              activeText = Functions.ActionItemRejectionAssignment.GetChangedParamsInfo(rejectionAssignment);
            
            SendNotice(task, defaultEventDescription, performers.Keys.ToList(), serviceUser, activeText, false);
            break;
          case "EscalateEvent":
            SendNotice(task,
                       NoticeSettings.Resources.EscalatedToFormat(Sungero.Company.PublicFunctions.Employee.GetShortName(escalatedTo, DeclensionCase.Accusative, false)),
                       performers.Keys.ToList(), serviceUser, string.Empty, true);
            break;
          case "TimeAcceptEvent":
            foreach(var group in performers.GroupBy(p => p.Value))
            {
              if (group.Key == DirRX.ActionItems.ActionItemsRole.Type.Performer)
                continue;
              
              SendNotice(task, defaultEventDescription, group.Select(g => g.Key).ToList(), serviceUser, string.Empty, false);
            }
            break;
          case "TimeDeclineEvent":
            foreach(var group in performers.GroupBy(p => p.Value))
            {
              if (group.Key == DirRX.ActionItems.ActionItemsRole.Type.Performer)
                continue;
              
              SendNotice(task, defaultEventDescription, group.Select(g => g.Key).ToList(), serviceUser, string.Empty, false);
            }
            break;
          case "EightyEvent":
            SendDeadlinePercentNotice(task, performers, "80", defaultEventDescription, serviceUser);
            break;
          case "SixtyEvent":
            SendDeadlinePercentNotice(task, performers, "60", defaultEventDescription, serviceUser);
            break;
          case "FortyEvent":
            SendDeadlinePercentNotice(task, performers, "40", defaultEventDescription, serviceUser);
            break;
          case "TwentyEvent":
            SendDeadlinePercentNotice(task, performers, "20", defaultEventDescription, serviceUser);
            break;
          case "DeadlineEvent":
            foreach(var group in performers.GroupBy(p => p.Value))
            {
              if (group.Key == DirRX.ActionItems.ActionItemsRole.Type.Subscriber)
              {
                SendNotice(task, NoticeSettings.Resources.DeadlineForSubscriber, group.Select(g => g.Key).ToList(), serviceUser, string.Empty, false);
                continue;
              }
              
              if (group.Key == DirRX.ActionItems.ActionItemsRole.Type.Controler)
              {
                SendNotice(task, NoticeSettings.Resources.DeadlineForSupervisor, group.Select(g => g.Key).ToList(), serviceUser, string.Empty, false);
                continue;
              }
              
              if (group.Key == DirRX.ActionItems.ActionItemsRole.Type.CEO)
              {
                if (task.Priority.PriorityValue.Value == 1)
                  SendNotice(task, NoticeSettings.Resources.DeadlineForFirstPriority, group.Select(g => g.Key).ToList(), serviceUser, string.Empty, false);
                else
                  SendNotice(task, defaultEventDescription, group.Select(g => g.Key).ToList(), serviceUser, string.Empty, false);
                continue;
              }
              
              SendNotice(task, defaultEventDescription, group.Select(g => g.Key).ToList(), serviceUser, string.Empty, false);
            }
            break;
          case "ExpiredEvent":
            foreach(var group in performers.GroupBy(p => p.Value))
            {
              if (group.Key == DirRX.ActionItems.ActionItemsRole.Type.Subscriber)
              {
                SendNotice(task, NoticeSettings.Resources.ExpiredForSubscriber, group.Select(g => g.Key).ToList(), serviceUser, string.Empty, false);
                continue;
              }
              
              if (group.Key == DirRX.ActionItems.ActionItemsRole.Type.Controler)
              {
                SendNotice(task, NoticeSettings.Resources.ExpiredForSupervisor, group.Select(g => g.Key).ToList(), serviceUser, string.Empty, false);
                continue;
              }
              
              SendNotice(task, defaultEventDescription, group.Select(g => g.Key).ToList(), serviceUser, string.Empty, false);
            }
            break;
          default:
            SendNotice(task, defaultEventDescription, performers.Keys.ToList(), serviceUser, string.Empty, false);
            break;
        }
      }
      
      #endregion
    }
    
    /// <summary>
    /// Отправка уведомлений для событий связанных с % от срока исполнения.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="performers">Получатели уведомлений.</param>
    /// <param name="percent">Процент.</param>
    /// <param name="defaultEventDescription">Тема по-умолчанию.</param>
    /// <param name="serviceUser">Пользователь от которого отправляется уведомление.</param>
    private static void SendDeadlinePercentNotice(DirRX.Solution.IActionItemExecutionTask task,
                                                  Dictionary<DirRX.Solution.IEmployee, Sungero.Core.Enumeration> performers,
                                                  string percent,
                                                  string defaultEventDescription,
                                                  Sungero.CoreEntities.IRecipient serviceUser)
    {
      foreach(var group in performers.GroupBy(p => p.Value))
      {
        if (group.Key == DirRX.ActionItems.ActionItemsRole.Type.Subscriber)
        {
          SendNotice(task, NoticeSettings.Resources.AssignmentPercentForSubcriberDescriptionFormat(percent), group.Select(g => g.Key).ToList(), serviceUser, string.Empty, false);
          continue;
        }
        
        if (group.Key == DirRX.ActionItems.ActionItemsRole.Type.Controler)
        {
          SendNotice(task, NoticeSettings.Resources.AssignmentPercentForSupervisorDescriptionFormat(percent), group.Select(g => g.Key).ToList(), serviceUser, string.Empty, false);
          continue;
        }
        
        if (group.Key == DirRX.ActionItems.ActionItemsRole.Type.InitManager)
        {
          SendNotice(task, NoticeSettings.Resources.AssignmentPercentForManagerDescriptionFormat(percent), group.Select(g => g.Key).ToList(), serviceUser, string.Empty, false);
          continue;
        }
        
        if (group.Key == DirRX.ActionItems.ActionItemsRole.Type.CEO)
        {
          if (task.Priority.PriorityValue.Value == 1)
            SendNotice(task, NoticeSettings.Resources.AssignmentPercentForCEODescriptionFormat(percent), group.Select(g => g.Key).ToList(), serviceUser, string.Empty, false);
          else
            SendNotice(task, defaultEventDescription, group.Select(g => g.Key).ToList(), serviceUser, string.Empty, false);
          continue;
        }
        
        SendNotice(task, defaultEventDescription, group.Select(g => g.Key).ToList(), serviceUser, string.Empty, false);
      }
    }
    
    /// <summary>
    /// Отправить уведомление.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="eventDescription">Тема по-умолчанию.</param>
    /// <param name="performers">Получатели уведомлений.</param>
    /// <param name="serviceUser">Пользователь от которого отправляется уведомление.</param>
    /// <param name="activeText">Текст уведомления.</param>
    /// <param name="needCreateAsSubtask">Необходимость создания уведомления подзадачей.</param>
    private static void SendNotice(DirRX.Solution.IActionItemExecutionTask task,
                                   string eventDescription,
                                   List<DirRX.Solution.IEmployee> performers,
                                   Sungero.CoreEntities.IRecipient serviceUser,
                                   string activeText,
                                   bool needCreateAsSubtask)
    {
      var notice = Sungero.Workflow.SimpleTasks.Null;
      
      if (needCreateAsSubtask)
        notice = Sungero.Workflow.SimpleTasks.CreateAsSubtask(task);
      else
        notice = Sungero.Workflow.SimpleTasks.Create();
      
      notice.ActiveText = activeText;
      notice.Attachments.Add(task);
      notice.Subject = string.Format("{0}. {1}", eventDescription, task.Subject);
      notice.NeedsReview = false;

      foreach (var performer in performers)
      {
        var routeStep = notice.RouteSteps.AddNew();
        routeStep.AssignmentType = Sungero.Workflow.SimpleTaskRouteSteps.AssignmentType.Notice;
        routeStep.Performer = performer;
        routeStep.Deadline = null;
      }
      
      notice.Author = Sungero.CoreEntities.Users.As(serviceUser);
      notice.StartedBy = Sungero.CoreEntities.Users.As(serviceUser);
      
      if (notice.Subject.Length > Sungero.Workflow.Tasks.Info.Properties.Subject.Length)
        notice.Subject = notice.Subject.Substring(0, Sungero.Workflow.Tasks.Info.Properties.Subject.Length);
      
      notice.Start();
    }
    
    /// <summary>
    /// Добавить участника процесса.
    /// </summary>
    /// <param name="performers">Список текущих участников.</param>
    /// <param name="newPerformer">Новый участник.</param>
    /// <param name="roleType">Роль.</param>
    /// <param name="priority">Приоритет.</param>
    /// <param name="assignmentEvent">Событие.</param>
    public static void AddPerformer(System.Collections.Generic.Dictionary<DirRX.Solution.IEmployee, Enumeration> performers,
                                    DirRX.Solution.IEmployee newPerformer,
                                    Enumeration roleType,
                                    DirRX.ActionItems.IPriority priority,
                                    string assignmentEvent)
    {
      if (newPerformer == null ||
          performers.Keys.Any(e => DirRX.Solution.Employees.Equals(e, newPerformer)) ||
          !NeedNotice(assignmentEvent, newPerformer, roleType, priority))
        return;
      
      performers.Add(newPerformer, roleType);
    }
    
    /// <summary>
    /// Получить настройку для пользователя.
    /// </summary>
    /// <param name="employee">Пользователь.</param>
    /// <param name="type">Тип роли.</param>
    /// <returns></returns>
    public static DirRX.ActionItems.INoticeSetting GetSetting(DirRX.Solution.IEmployee employee, Enumeration type)
    {
      var employeeSetting = NoticeSettings.GetAll().SingleOrDefault(s => s.AssgnRole.Type == type &&
                                                                    DirRX.Solution.Employees.Equals(s.Employee, employee));
      if (employeeSetting == null)
        employeeSetting = NoticeSettings.GetAll().SingleOrDefault(s => s.AssgnRole.Type == type && s.AllUsersFlag.HasValue && s.AllUsersFlag.Value);
      
      return employeeSetting;
    }
    
    /// <summary>
    /// Проверить соответствие настроек.
    /// </summary>
    /// <param name="assignmentEvent">Событие.</param>
    /// <param name="performer">Исполнитель.</param>
    /// <param name="roleType">Роль.</param>
    /// <param name="priority">Приоритет.</param>
    /// <returns>True, если необходимо отправить уведомление.</returns>
    public static bool NeedNotice(string assignmentEvent, DirRX.Solution.IEmployee performer, Enumeration roleType, DirRX.ActionItems.IPriority priority)
    {
      var setting = GetSetting(performer, roleType);
      if (setting == null)
        return false;
      
      switch (assignmentEvent)
      {
        case "StartEvent":
          return setting.IsAssignmentStarts.GetValueOrDefault() && (setting.AStartsPriority.Select(p => p.Priority).Contains(priority) || !setting.AStartsPriority.Any());
        case "AbortEvent":
          return setting.IsAssignmentAborts.GetValueOrDefault() && (setting.AAbortsPriority.Select(p => p.Priority).Contains(priority) || !setting.AAbortsPriority.Any());
        case "RejectionEvent":
          return setting.IsAssignmentDeclined.GetValueOrDefault() && (setting.ADeclinedPriority.Select(p => p.Priority).Contains(priority) || !setting.ADeclinedPriority.Any());
        case "ActionItemChangedEvent":
          return setting.IsAssignmentNewSubj.GetValueOrDefault() && (setting.ANewSubjPriority.Select(p => p.Priority).Contains(priority) || !setting.ANewSubjPriority.Any());
        case "ReturnEvent":
          return setting.IsAssignmentRevision.GetValueOrDefault() && (setting.ARevisionPriority.Select(p => p.Priority).Contains(priority) || !setting.ARevisionPriority.Any());
        case "OnControlEvent":
          return setting.IsAssignmentOnControl.GetValueOrDefault() && (setting.AOnControlPriority.Select(p => p.Priority).Contains(priority) || !setting.AOnControlPriority.Any());
        case "AcceptEvent":
          return setting.IsAssignmentAccept.GetValueOrDefault() && (setting.AAcceptPriority.Select(p => p.Priority).Contains(priority) || !setting.AAcceptPriority.Any());
        case "ReworkEvent":
          return setting.IsAssignmentRework.GetValueOrDefault() && (setting.AReworkPriority.Select(p => p.Priority).Contains(priority) || !setting.AReworkPriority.Any());
        case "PerformEvent":
          return setting.IsAssignmentPerform.GetValueOrDefault() && (setting.APerformPriority.Select(p => p.Priority).Contains(priority) || !setting.APerformPriority.Any());
        case "EscalateEvent":
          return setting.IsAssignmentEscalated.GetValueOrDefault() && (setting.AEscalatedPriority.Select(p => p.Priority).Contains(priority) || !setting.AEscalatedPriority.Any());
        case "AddTimeEvent":
          return setting.IsAssignmentAddTime.GetValueOrDefault() && (setting.AAddTimePriority.Select(p => p.Priority).Contains(priority) || !setting.AAddTimePriority.Any());
        case "TimeAcceptEvent":
          return setting.IsAssignmentTimeAccept.GetValueOrDefault() && (setting.ATimeAcceptPriority.Select(p => p.Priority).Contains(priority) || !setting.ATimeAcceptPriority.Any());
        case "TimeDeclineEvent":
          return setting.IsAssignmentTimeAccept.GetValueOrDefault() && (setting.ATimeAcceptPriority.Select(p => p.Priority).Contains(priority) || !setting.ATimeAcceptPriority.Any());
        case "EightyEvent":
          return setting.IsAssignmentEighty.GetValueOrDefault() && (setting.AEightyPriority.Select(p => p.Priority).Contains(priority) || !setting.AEightyPriority.Any());
        case "SixtyEvent":
          return setting.IsAssignmentSixty.GetValueOrDefault() && (setting.ASixtyPriority.Select(p => p.Priority).Contains(priority) || !setting.ASixtyPriority.Any());
        case "FortyEvent":
          return setting.IsAssignmentForty.GetValueOrDefault() && (setting.AFortyPriority.Select(p => p.Priority).Contains(priority) || !setting.AFortyPriority.Any());
        case "TwentyEvent":
          return setting.IsAssignmentTwenty.GetValueOrDefault() && (setting.ATwentyPriority.Select(p => p.Priority).Contains(priority) || !setting.ATwentyPriority.Any());
        case "DeadlineEvent":
          return setting.IsAssignmentDeadline.GetValueOrDefault() && (setting.ADeadlinePriority.Select(p => p.Priority).Contains(priority) || !setting.ADeadlinePriority.Any());
        case "ExpiredEvent":
          return setting.IsAssignmentExpired.GetValueOrDefault() && (setting.AExpiredPriority.Select(p => p.Priority).Contains(priority) || !setting.AExpiredPriority.Any());

          default: return false;
      }
    }
    
    #endregion
  }
}