using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain;

namespace DirRX.ActionItems.Server
{
  public class ModuleFunctions
  {

    
    /// <summary>
    /// Расcчитать время поручения в работе.
    /// </summary>
    /// <param name="actionItemID">ИД Поручения.</param>
    /// <returns>Время в работе.</returns>
    [Public]
    public static int CalculateTimeInWork (int actionItemID)
    {
      var days = 0;
      var actionItem = DirRX.Solution.ActionItemExecutionTasks.Get(actionItemID);
      var executionAssignments = Solution.ActionItemExecutionAssignments
        .GetAll(i => Sungero.Workflow.Tasks.Equals(i.Task, DirRX.Solution.ActionItemExecutionTasks.As(actionItem)));

      foreach (var assignment in executionAssignments)
      {
        if (assignment.Status == DirRX.Solution.ActionItemExecutionAssignment.Status.InProcess)
          days += WorkingTime.GetDurationInWorkingDays(assignment.Created.Value, Calendar.Now, assignment.EmployeePerformer);
        
        if (assignment.Status == DirRX.Solution.ActionItemExecutionAssignment.Status.Completed)
          days += WorkingTime.GetDurationInWorkingDays(assignment.Created.Value, assignment.Completed.Value, assignment.CompletedBy);
      }
      
      return days;
    }

    
    /// <summary>
    /// Получить данные по исполнению поручений в срок.
    /// </summary>
    
    /// <param name="beginDate">Начало периода.</param>
    /// <param name="endDate">Конец периода.</param>
    /// <param name="author">Автор.</param>
    /// <param name="businessUnit">НОР.</param>
    /// <param name="department">Подразделение.</param>
    /// <param name="performer">Исполнитель.</param>
    /// <param name="initiator">Постановщик.</param>
    /// <param name="priority">Приоритет.</param>
    /// <param name="category">Категория.</param>
    /// <param name="isEscalated">Эскалировано.</param>
    /// <param name="getCoAssignees">Признак необходимости получения соисполнителей.</param>

    /// <returns>Данные по исполнению поручений в срок.</returns>
    public virtual List<Structures.Module.LightActionItem> GetActionItemCompletionData(DateTime? beginDate,
                                                                                       DateTime? endDate,
                                                                                       DirRX.Solution.IEmployee author,
                                                                                       DirRX.Solution.IBusinessUnit businessUnit,
                                                                                       DirRX.Solution.IDepartment department,
                                                                                       Sungero.CoreEntities.IUser performer,
                                                                                       DirRX.Solution.IEmployee initiator,
                                                                                       DirRX.ActionItems.IPriority priority,
                                                                                       DirRX.ActionItems.ICategory category,
                                                                                       bool? isEscalated,
                                                                                       bool getCoAssignees)
    {
      List<DirRX.ActionItems.Structures.Module.LightActionItem> tasks = null;
      
      var isAdministratorOrAdvisor = Sungero.Docflow.PublicFunctions.Module.Remote.IsAdministratorOrAdvisor();
      var recipientsIds = Substitutions.ActiveSubstitutedUsers.Select(u => u.Id).ToList();
      recipientsIds.Add(Users.Current.Id);
      

      var query = DirRX.Solution.ActionItemExecutionTasks.GetAll()
        .Where(t => isAdministratorOrAdvisor ||
               recipientsIds.Contains(t.Author.Id) || recipientsIds.Contains(t.StartedBy.Id) ||
               recipientsIds.Contains(t.Initiator.Id) || recipientsIds.Contains(t.Assignee.Id) ||
               t.ActionItemType == DirRX.Solution.ActionItemExecutionTask.ActionItemType.Component &&
               recipientsIds.Contains(t.MainTask.StartedBy.Id))
        .Where(t => t.Status == Sungero.Workflow.Task.Status.Completed || t.Status == Sungero.Workflow.Task.Status.InProcess)
        .Where(t => t.IsCompoundActionItem != true && t.ActionItemType != DirRX.Solution.ActionItemExecutionTask.ActionItemType.Additional);
      

      var serverBeginDate = Sungero.Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(beginDate.Value);
      var serverEndDate = endDate.Value.EndOfDay().FromUserTime();
      query = query.Where(t => t.Status == Sungero.Workflow.Task.Status.Completed &&
                          (t.ActualDate.Between(beginDate.Value.Date, endDate.Value.Date) ||
                           (t.InitialDeadline.Value.Date == t.InitialDeadline.Value ? t.InitialDeadline.Between(beginDate.Value.Date, endDate.Value.Date) : t.InitialDeadline.Between(serverBeginDate, serverEndDate)) ||
                           t.ActualDate >= endDate && (t.InitialDeadline.Value.Date == t.InitialDeadline.Value ? t.InitialDeadline <= beginDate.Value.Date : t.InitialDeadline <= serverBeginDate)) ||
                          t.Status == Sungero.Workflow.Task.Status.InProcess &&
                          (t.InitialDeadline.Value.Date == t.InitialDeadline.Value ? t.InitialDeadline <= endDate.Value.Date : t.InitialDeadline <= serverEndDate));
      
      if (isEscalated == true)
        query = query.Where(t => t.IsEscalated == true);
      
      // Если надо получить соисполнителей.
      if (getCoAssignees)
        tasks = query
          .Select(t => Structures.Module.LightActionItem
                  .Create(t.Id, t.Status, t.ActualDate, t.InitialDeadline, t.Author, t.Assignee, t.ActionItem, t.ExecutionState, t.Initiator, t.Priority, t.Category, this.GetCoAssigneesShortNames(t), t.Started))
          .ToList();
      else
        tasks = query
          .Select(t => Structures.Module.LightActionItem
                  .Create(t.Id, t.Status, t.ActualDate, t.InitialDeadline, t.Author, t.Assignee, t.ActionItem, t.ExecutionState, t.Initiator, t.Priority, t.Category, null, t.Started))
          .ToList();
      
      if (author != null)
        tasks = tasks.Where(t => Equals(t.Author, author))
          .ToList();
      
      if (businessUnit != null)
        tasks = tasks.Where(t => Equals(Sungero.Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(t.Assignee), businessUnit))
          .ToList();
      
      if (department != null)
        tasks = tasks.Where(t => t.Assignee != null && t.Assignee.Department != null &&
                            Equals(t.Assignee.Department, department))
          .ToList();
      
      if (performer != null)
        tasks = tasks.Where(t => Equals(t.Assignee, performer))
          .ToList();
      
      if (initiator != null)
        tasks = tasks.Where(t => Equals(t.Initiator, initiator))
          .ToList();
      
      if (priority != null)
        tasks = tasks.Where(t => Equals(t.Priority, priority))
          .ToList();
      
      if (category != null)
        tasks = tasks.Where(t => Equals(t.Category, category))
          .ToList();
      
      
      return tasks;
    }
    
    /// <summary>
    /// Получить сокращенные ФИО соисполнителей.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <returns>Список сокращенных ФИО соисполнителей.</returns>
    private List<string> GetCoAssigneesShortNames(Solution.IActionItemExecutionTask task)
    {
      return task.CoAssignees.Select(ca => ca.Assignee.Person.ShortName).ToList();
    }
    
    /// <summary>
    /// Получить руководителя по эскалации.
    /// </summary>
    /// <param name="task">Задача на исполнение поручения.</param>
    /// <returns>Руководитель по эскалации.</returns>
    [Public, Remote(IsPure = true)]
    public static Solution.IEmployee GetEscalatedManager(Solution.IActionItemExecutionTask task)
    {
      var employee = (task.Priority.Manager.Type.Value == ActionItems.ActionItemsRole.Type.CEO ||
                      task.Priority.Manager.Type.Value == ActionItems.ActionItemsRole.Type.CEOAssistant) ? task.Initiator : Solution.Employees.As(task.Assignee);
      
      //Подчинённые поручения эскалируются постановщику.
      return task.ParentAssignment != null && DirRX.Solution.ActionItemExecutionAssignments.Is(task.ParentAssignment) ? task.Initiator :
        DirRX.ActionItems.Functions.ActionItemsRole.GetRolePerformer(task.Priority.Manager, employee);
    }
    
    /// <summary>
    /// Получить действующие настройки контроля по категории.
    /// </summary>
    /// <param name="category">Категория.</param>
    /// <returns>Список настроек контроля по поручениям.</returns>
    [Public, Remote(IsPure = true)]
    public static List<DirRX.ActionItems.IControlSetting> GetControlSettings(DirRX.ActionItems.ICategory category)
    {
      return ControlSettings.GetAll()
        .Where(s => Categories.Equals(s.Category, category) && s.Status == DirRX.ActionItems.ControlSetting.Status.Active)
        .ToList();
    }
    
    /// <summary>
    /// Получить контролёра по сотруднику и категории.
    /// </summary>
    /// <param name="employee">Инициатор.</param>
    /// <param name="category">Категория.</param>
    /// <param name="assignee">Исполнитель.</param>
    /// <returns>Контролёр.</returns>
    /// <returns>Контролёр.</returns>
    [Public, Remote(IsPure = true)]
    public DirRX.Solution.IEmployee GetSupervisor(DirRX.Solution.IEmployee initiator, DirRX.ActionItems.ICategory category, DirRX.Solution.IEmployee assignee)
    {
      var controlSettings = GetControlSettings(category);
      var controlSetting = controlSettings.FirstOrDefault(s => s.Status.Value == Sungero.CoreEntities.DatabookEntry.Status.Active &&
                                                          DirRX.ActionItems.Functions.ActionItemsRole.IsPerformerRole(s.Initiator, initiator, true));
      
      if (controlSetting != null)
      {
        if (controlSetting.Supervisor.Type.Value == ActionItems.ActionItemsRole.Type.CEOAssistant ||
            controlSetting.Supervisor.Type.Value == ActionItems.ActionItemsRole.Type.CEO)
          return DirRX.ActionItems.Functions.ActionItemsRole.GetRolePerformer(controlSetting.Supervisor, initiator);
        
        if (assignee != null)
        {
          if(controlSetting.Supervisor.Type.Value == ActionItems.ActionItemsRole.Type.InitCEOManager &&
             DirRX.ActionItems.Functions.ActionItemsRole
             .IsPerformerRole(ActionItems.ActionItemsRoles.GetAll(x => x.Type == ActionItems.ActionItemsRole.Type.CEO).FirstOrDefault(), assignee, false))
            return DirRX.Solution.Employees.As(Solution.BusinessUnits.GetAll(x => x.HeadCompany == null).FirstOrDefault().CEO);
          else
            return DirRX.ActionItems.Functions.ActionItemsRole.GetRolePerformer(controlSetting.Supervisor, assignee);
        }
        else
          return initiator;
      }
      return null;
    }
    
    /// <summary>
    /// Получить роль ответственных за настройку поручений.
    /// </summary>
    /// <returns>Роль ответственных за настройку поручений.</returns>
    [Remote(IsPure = true), Public]
    public static IRole GetAssignmentSettingResponsiblesRole()
    {
      return Roles.GetAll().SingleOrDefault(r => r.Sid == Constants.Module.AssignmentSettingResponsiblesRole);
    }

    /// <summary>
    /// Создать новое поручение.
    /// </summary>
    /// <returns>Созданное поручение.</returns>
    [Remote]
    public DirRX.Solution.IActionItemExecutionTask CreateAssignment()
    {
      return DirRX.Solution.ActionItemExecutionTasks.Create();
    }
    
    /// <summary>
    /// Получить список подчиненных подразделений начиная от текущего.
    /// </summary>
    /// <param name="depatments">Головные подразделения, по которым ведется поиск.</param>
    /// <param name="depatmentsAll">Обобщенный список подразделений, накопленных на текущий момент.</param>
    /// <returns>Список подчиненных подразделений, включая текущее.</returns>
    [Remote(IsPure = true), Public]
    public static List<Sungero.Company.IDepartment> GetDepartmentHierarchyDown(List<Sungero.Company.IDepartment> depatments, List<Sungero.Company.IDepartment> depatmentsAll)
    {
      foreach (Sungero.Company.IDepartment depatment in depatments)
      {
        var lowerDepartments = Sungero.Company.Departments.GetAll(d => Sungero.Company.Departments.Equals(d.HeadOffice, depatment) && !depatmentsAll.Contains(d)).ToList();
        if (lowerDepartments.Any())
        {
          depatmentsAll.AddRange(lowerDepartments);
          GetDepartmentHierarchyDown(lowerDepartments, depatmentsAll);
        }
      }
      
      return depatmentsAll;
    }
    
    /// <summary>
    /// Получить список подчиненных подразделений начиная от текущего.
    /// </summary>
    /// <param name="depatments">Головные подразделения, по которым ведется поиск.</param>
    /// <param name="depatmentsAll">Обобщенный список подразделений, накопленных на текущий момент.</param>
    /// <param name="steps">Уровень подчинения подразделений относительно головных подразделений (-1 - вся ветка целиком).</param>
    /// <returns>Список подчиненных подразделений, включая текущее.</returns>
    [Remote(IsPure = true), Public]
    public static List<Sungero.Company.IDepartment> GetDepartmentHierarchyDown(List<Sungero.Company.IDepartment> depatments, List<Sungero.Company.IDepartment> depatmentsAll, int steps)
    {
      if (steps == 0)
        return depatmentsAll;
      
      foreach (Sungero.Company.IDepartment depatment in depatments)
      {
        var lowerDepartments = Sungero.Company.Departments.GetAll(d => Sungero.Company.Departments.Equals(d.HeadOffice, depatment) && !depatmentsAll.Contains(d)).ToList();
        if (lowerDepartments.Any())
        {
          depatmentsAll.AddRange(lowerDepartments);
          GetDepartmentHierarchyDown(lowerDepartments, depatmentsAll, steps - 1);
        }
      }
      
      return depatmentsAll;
    }
    
    /// <summary>
    /// Получить все подзадачи в работе запущенные из текущего задания на исполнение.
    /// </summary>
    /// <param name="assignment">Задание на исполнение поручения.</param>
    /// <returns>Список подзадач в работе.</returns>
    [Remote(IsPure = true), Public]
    public static List<Sungero.Workflow.ITask> GetSubTasksByAssignment(DirRX.Solution.IActionItemExecutionAssignment assignment)
    {
      return Sungero.Workflow.Tasks.GetAll(t => t.ParentAssignment == assignment &&
                                           !Sungero.Workflow.SimpleTasks.Is(t) &&
                                           t.Status.Value == Sungero.Workflow.Task.Status.InProcess).ToList();
    }
    
    /// <summary>
    /// Получить все подзадачи в работе запущенные из составного поручения.
    /// </summary>
    /// <param name="assignment">Составное поручение.</param>
    /// <returns>Список подзадач в работе.</returns>
    [Remote(IsPure = true), Public]
    public static List<DirRX.Solution.IActionItemExecutionTask> GetSubTasksByCompoundTask(DirRX.Solution.IActionItemExecutionTask task)
    {
      return DirRX.Solution.ActionItemExecutionTasks.GetAll(t => t.ParentTask == task &&
                                                            DirRX.Solution.ActionItemExecutionTasks.Is(t) &&
                                                            t.Status.Value == Sungero.Workflow.Task.Status.InProcess).ToList();
    }

    /// <summary>
    /// Прекратить все подзадачи в работе запущенные из текущего задания на исполнение.
    /// </summary>
    /// <param name="assignment">Задание на исполнение поручения.</param>
    [Remote(IsPure = true), Public]
    public static void AbortSubTasksByAssignment(DirRX.Solution.IActionItemExecutionAssignment assignment)
    {
      List<Sungero.CoreEntities.IUser> recipients = new List<Sungero.CoreEntities.IUser>();
      if (!Equals(assignment.Performer, Users.Current))
        recipients.Add(assignment.Performer);
      
      var subTasks = GetSubTasksByAssignment(assignment);
      foreach (Sungero.Workflow.ITask subTask in subTasks)
      {
        var assignmentTask = DirRX.Solution.ActionItemExecutionTasks.As(subTask);
        if (assignmentTask != null)
        {
          if (assignmentTask.IsCompoundActionItem.GetValueOrDefault())
          {
            var compoundSubTasks = GetSubTasksByCompoundTask(assignmentTask);
            foreach (DirRX.Solution.IActionItemExecutionTask compoundSubTask in compoundSubTasks)
            {
              compoundSubTask.AbortingReason = DirRX.Solution.ActionItemExecutionTasks.Resources.AutoAbortingReason;
              compoundSubTask.Abort();
              recipients.AddRange(Sungero.Workflow.AssignmentBases.GetAll(a => Equals(a.Task, compoundSubTask)).Select(u => u.Performer).ToList());
            }
          }
          else
            assignmentTask.AbortingReason = DirRX.Solution.ActionItemExecutionTasks.Resources.AutoAbortingReason;
        }
        
        subTask.Abort();
        recipients.AddRange(Sungero.Workflow.AssignmentBases.GetAll(a => Equals(a.Task, subTask)).Select(u => u.Performer).ToList());
      }

      recipients = recipients.Distinct().ToList();
      recipients.Remove(Users.Current);

      if (recipients.Any())
      {
        string threadSubject = DirRX.Solution.ActionItemExecutionTasks.Resources.ExecutionStoppedThreadNoticeSubject;
        string noticesSubject = DirRX.Solution.ActionItemExecutionTasks.Resources.ExecutionStoppedNoticeSubjectFormat(threadSubject, assignment.Subject);
        Sungero.Docflow.PublicFunctions.Module.Remote.SendNoticesAsSubtask(noticesSubject, recipients, assignment.Task, DirRX.Solution.ActionItemExecutionTasks.Resources.AutoAbortingReason, Users.Current, threadSubject);
      }
    }
    
    
    #region Скопировано из стандартной разработки.

    /// <summary>
    /// Выдать права на задачу контролеру, инициатору и группе регистрации инициатора ведущей задачи (включая ведущие ведущего).
    /// </summary>
    /// <param name="targetTask">Текущая задача.</param>
    /// <param name="sourceTask">Ведущая задача.</param>
    /// <returns>Текущую задачу с правами.</returns>
    [Public]
    public static Sungero.Domain.Shared.IEntity GrantAccessRightToTask(Sungero.Domain.Shared.IEntity targetTask, Sungero.Workflow.ITask sourceTask)
    {
      if (targetTask == null || sourceTask == null)
        return null;
      
      if (!DirRX.Solution.ActionItemExecutionTasks.Is(sourceTask))
        sourceTask = GetLeadTaskToTask(sourceTask);
      
      var leadPerformers = GetLeadActionItemExecutionPerformers(DirRX.Solution.ActionItemExecutionTasks.As(sourceTask));
      foreach (var performer in leadPerformers)
        targetTask.AccessRights.Grant(performer, DefaultAccessRightsTypes.Change);
      
      return targetTask;
    }
    
    /// <summary>
    /// Получить всех контролеров, инициаторов (включая группу регистрации) ведущих задач.
    /// </summary>
    /// <param name="actionItemExecution">Поручение.</param>
    /// <returns>Список контроллеров, инициаторов.</returns>
    private static List<IRecipient> GetLeadActionItemExecutionPerformers(Sungero.RecordManagement.IActionItemExecutionTask actionItemExecution)
    {
      var leadPerformers = new List<IRecipient>();
      var taskAuthors = new List<IRecipient>();
      Sungero.Workflow.ITask parentTask = actionItemExecution;
      
      while (true)
      {
        if (parentTask.StartedBy != null)
          taskAuthors.Add(parentTask.StartedBy);
        
        if (DirRX.Solution.ActionItemExecutionTasks.Is(parentTask))
        {
          var parentActionItemExecution = DirRX.Solution.ActionItemExecutionTasks.As(parentTask);
          taskAuthors.Add(parentActionItemExecution.Author);
          if (parentActionItemExecution.Supervisor != null)
            leadPerformers.Add(parentActionItemExecution.Supervisor);
          if (parentActionItemExecution.AssignedBy != null)
            leadPerformers.Add(parentActionItemExecution.AssignedBy);
        }
        else if (Sungero.RecordManagement.DocumentReviewTasks.Is(parentTask))
        {
          var parentDocumentReview = Sungero.RecordManagement.DocumentReviewTasks.As(parentTask);
          taskAuthors.Add(parentDocumentReview.Author);
        }
        else if (Sungero.Docflow.ApprovalTasks.Is(parentTask))
        {
          // TODO Добавить исполнителей соласования.
          var parentApprovalTask = Sungero.Docflow.ApprovalTasks.As(parentTask);
          taskAuthors.Add(parentApprovalTask.Author);
        }
        
        if (Equals(parentTask.MainTask, parentTask))
          break;
        parentTask = GetLeadTaskToTask(parentTask);
      }
      
      // В список также включить: группы регистрации в которых находится инициатор задачи, группу регистрации для документа.
      var registrationGroups = GetActionItemRegistrationGroups(taskAuthors, actionItemExecution.DocumentsGroup.OfficialDocuments.FirstOrDefault());
      
      leadPerformers.AddRange(taskAuthors);
      leadPerformers.AddRange(registrationGroups);
      return leadPerformers.Distinct().ToList();
    }

    /// <summary>
    /// Получить ведущую задачу задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <returns>Ведущая задача.</returns>
    private static Sungero.Workflow.ITask GetLeadTaskToTask(Sungero.Workflow.ITask task)
    {
      if (task.ParentAssignment != null)
        return task.ParentAssignment.Task;
      else
        return task.ParentTask ?? task.MainTask;
    }

    /// <summary>
    /// Получить группы регистрации для поручения.
    /// </summary>
    /// <param name="users">Список пользователей.</param>
    /// <param name="document">Вложенный документ.</param>
    /// <returns>Список групп регистрации.</returns>
    private static List<Sungero.Docflow.IRegistrationGroup> GetActionItemRegistrationGroups(IList<IRecipient> users, Sungero.Docflow.IOfficialDocument document)
    {
      var groups = new List<Sungero.Docflow.IRegistrationGroup>();
      if (document != null && document.DocumentRegister != null &&
          document.DocumentRegister.RegistrationGroup != null &&
          document.DocumentRegister.RegistrationGroup.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
        groups.Add(document.DocumentRegister.RegistrationGroup);
      
      return groups;
    }
    #endregion
    
    /// <summary>
    /// Отправка задачи на отклонение поручения.
    /// </summary>
    /// <param name="actionItemExecutionTask"></param>
    [Public, Remote]
    public void SendRejectionTask(DirRX.Solution.IActionItemExecutionTask actionItemExecutionMainTask, DirRX.Solution.IActionItemExecutionTask actionItemExecutionTask, string reason)
    {
      var actionItemRejectionTask = DirRX.ActionItems.ActionItemRejectionTasks.CreateAsSubtask(actionItemExecutionTask);
      
      actionItemRejectionTask.ActionItemExecutionMainTask = actionItemExecutionMainTask;
      actionItemRejectionTask.ActionItemExecutionTask = actionItemExecutionTask;
      actionItemRejectionTask.Category = actionItemExecutionTask.Category;
      actionItemRejectionTask.Priority = actionItemExecutionTask.Priority;
      actionItemRejectionTask.Initiator = actionItemExecutionTask.Initiator;
      actionItemRejectionTask.ReportDeadline = actionItemExecutionTask.ReportDeadline;
      actionItemRejectionTask.Mark = actionItemExecutionTask.Mark;
      actionItemRejectionTask.AssignedBy = actionItemExecutionTask.AssignedBy;
      actionItemRejectionTask.Assignee = DirRX.Solution.Employees.As(actionItemExecutionTask.Assignee);
      actionItemRejectionTask.IsUnderControl = actionItemExecutionTask.IsUnderControl.GetValueOrDefault();
      actionItemRejectionTask.Supervisor = DirRX.Solution.Employees.As(actionItemExecutionTask.Supervisor);
      actionItemRejectionTask.ActionItemDeadline = actionItemExecutionTask.Deadline;
      actionItemRejectionTask.ActionItem = actionItemExecutionTask.ActionItem;
      actionItemRejectionTask.Reason = reason;
      actionItemRejectionTask.ActiveText = reason;
      
      foreach (var subscriberTask in actionItemExecutionMainTask.Subscribers)
      {
        var subscriber = actionItemRejectionTask.Subscribers.AddNew();
        subscriber.Subscriber = DirRX.Solution.Employees.As(subscriberTask.Subscriber);
      }
      
      foreach (var coAssigneeMain in actionItemExecutionMainTask.CoAssignees)
      {
        var coAssignee = actionItemRejectionTask.CoAssignees.AddNew();
        coAssignee.Assignee = DirRX.Solution.Employees.As(coAssigneeMain.Assignee);
      }
      
      var document = actionItemExecutionMainTask.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      // Синхронизировать вложения.
      if (document != null)
      {
        actionItemRejectionTask.DocumentsGroup.OfficialDocuments.Add(document);
        Sungero.Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(actionItemRejectionTask.AddendaGroup, document);
      }
      
      foreach (var otherEntity in actionItemExecutionMainTask.OtherGroup.All)
        actionItemRejectionTask.OtherGroup.All.Add(otherEntity);
      // TODO: Не синхронизируется группа "Дополнительно".
      
      actionItemRejectionTask.Subject = Functions.ActionItemRejectionTask.GetSubjectRejectAssignment(ActionItemRejectionTasks.Resources.SubjectRejectionTask, actionItemExecutionTask);
      actionItemRejectionTask.Start();
    }
    
    /// <summary>
    /// Проверить наличие запроса на отклонения поручения.
    /// </summary>
    /// <param name="actionItemExecutionTask">Поручение.</param>
    /// <returns>True если  по поручению уже был создан запрос.</returns>
    [Public, Remote(IsPure = true)]
    public bool RejectionTaskCreated(DirRX.Solution.IActionItemExecutionAssignment actionItemExecutionAssignment)
    {
      var actionItemExecutionTask	= DirRX.Solution.ActionItemExecutionTasks.As(actionItemExecutionAssignment.Task);
      
      var rejectionTasks = DirRX.ActionItems.ActionItemRejectionTasks.GetAll()
        .Where(t => DirRX.Solution.ActionItemExecutionTasks.Equals(t.ActionItemExecutionTask, actionItemExecutionTask)
               && t.Created > actionItemExecutionAssignment.Created);
      
      if (rejectionTasks.Any())
        return true;
      
      return false;
    }
    
    
    /// <summary>
    /// Эскалация поручения.
    /// </summary>
    /// <param name="task">Задача на исполнение поручения.</param>
    /// <param name="reason">Причина эскалации.</param>
    /// <param name="assignee">Исполнитель задания на эскалацию.</param>
    [Public]
    public bool EscalateActionItemTask(Solution.IActionItemExecutionTask task, string reason, DirRX.Solution.IEmployee assignee)
    {
      var escalatedTask = ActionItemEscalatedTasks.Create();
      
      escalatedTask.NeedsReview = false;
      var assignment = Solution.ActionItemExecutionAssignments.GetAll(x => Sungero.Workflow.Tasks.Equals(x.Task, task) &&
                                                                      x.Status == Solution.ActionItemExecutionAssignment.Status.InProcess).FirstOrDefault();
      if (assignment != null)
      {
        var lockAssigmentInfo = Locks.GetLockInfo(assignment);
        if (lockAssigmentInfo.IsLocked)
          return false;
        
        Logger.Debug(string.Format("Вложенное задание: {0}.", assignment.Subject));
        assignment.IsEscalated = true;
        task.IsEscalated = true;
        escalatedTask.AttachmentGroup.All.Add(assignment);
        var link = Hyperlinks.Get(assignment);
        escalatedTask.Subject = DirRX.ActionItems.Resources.EscalatedTaskSubjectFormat(assignment.Subject);
        
        if (escalatedTask.Subject.Length > escalatedTask.Info.Properties.Subject.Length)
          escalatedTask.Subject = escalatedTask.Subject.Substring(0, escalatedTask.Info.Properties.Subject.Length);
        
        Logger.DebugFormat("Тема: {0}", escalatedTask.Subject);
        var text = escalatedTask.Texts.AddNew();
        text.Body = escalatedTask.Subject;
        escalatedTask.ActiveText = DirRX.ActionItems.Resources.EscalatedTaskActiveTextFormat(link, reason);
        
        escalatedTask.Performer = assignee;
        escalatedTask.Save();
        assignment.MainTask.AccessRights.Grant(assignee, DefaultAccessRightsTypes.Change);
        assignment.MainTask.AccessRights.Save();
        task.AccessRights.Grant(assignee, DefaultAccessRightsTypes.Change);
        task.AccessRights.Save();
        escalatedTask.Start();
        return true;
      }
      else
      {
        Logger.Debug("Задание не найдено");
        return false;
      }
    }
    
    /// <summary>
    /// Определение причины эскалации.
    /// </summary>
    [Public]
    public string GetEscalationReason(Solution.IActionItemExecutionTask task)
    {
      var endDate = Sungero.Docflow.PublicFunctions.Module.GetDateWithTime(task.FinalDeadline.HasValue ? task.FinalDeadline.Value : task.Deadline.Value, task.Initiator);
      
      if ((endDate.Hour > 17 && endDate.Hour < 23 || endDate.Hour == 0) && Calendar.Now.EndOfDay() == endDate.EndOfDay())
        return string.Empty;
      
      var executionPeriod = WorkingTime.GetDurationInWorkingHours(task.Started.Value, endDate, task.Initiator);
      var residualPeriod = WorkingTime.GetDurationInWorkingHours(Calendar.Now, endDate, task.Initiator);
      
      Logger.DebugFormat("endDate: {0}, executionPeriod {1}, residualPeriod {2}", endDate.ToString(), executionPeriod.ToString(), residualPeriod.ToString());
      
      if (residualPeriod <= task.Priority.EscalationPeriodWorkDays)
        return string.Format("{0} {1}", residualPeriod.ToString(), task.Priority.EscalationPeriodWorkDaysText);
      else if (((double)residualPeriod / executionPeriod * 100) <= task.Priority.EscalationPeriodPercent)
        return DirRX.ActionItems.Resources.EscalatedReasonTextFormat(residualPeriod.ToString(),
                                                                     task.Priority.EscalationPeriodWorkDaysText, task.Priority.EscalationPeriodPercent);
      else
        return string.Empty;
    }
    
    /// <summary>
    /// Создать новую настройку уведомлений.
    /// </summary>
    /// <returns>Созданная настройка.</returns>
    [Remote]
    public ActionItems.INoticeSetting CreateNoticeSetting(ActionItems.IActionItemsRole role)
    {
      var employee = DirRX.Solution.Employees.As(Users.Current);
      var setting = NoticeSettings.GetAll(s => ActionItemsRoles.Equals(s.AssgnRole, role) && DirRX.Solution.Employees.Equals(s.Employee, employee)).FirstOrDefault();
      if (setting == null)
      {
        setting = NoticeSettings.Create();
        setting.AssgnRole = role;
        setting.Employee = employee;
        setting.AllUsersFlag = false;
        
        var allUserSetting = NoticeSettings.GetAll().FirstOrDefault(s => s.AllUsersFlag.HasValue && s.AllUsersFlag.Value && ActionItemsRoles.Equals(s.AssgnRole, role));
        Functions.NoticeSetting.FillSetting(setting, allUserSetting);
      }

      return setting;
    }
    
    #region Из стандартной.
    
    /// <summary>
    /// Получить список всех получателей.
    /// </summary>
    /// <returns>Список всех получателей.</returns>
    [Remote(IsPure = true)]
    public IQueryable<IRecipient> GetAllRecipients()
    {
      return Sungero.CoreEntities.Recipients.GetAll();
    }
    
    #endregion

    #region Получение данных для виджета.
    #region Для роли Автор.
    /// <summary>
    /// Получить просроченные поручения для роли Автор.
    /// </summary>
    /// <param name="users">Авторы поручений.</param>
    /// <param name="priority">Приоритет поручений.</param>
    /// <param name="isEscalated">Признак эскалированности.</param>
    /// <returns>Задачи.</returns>
    [Remote]
    public List<Solution.IActionItemExecutionTask> GetOverdueTaskByAuthor(List<IUser> users, int? priority, bool? isEscalated)
    {
      var result = GetActionItemTasks(priority, isEscalated);
      return result.Where(x => users.Contains(x.AssignedBy) && x.MaxDeadline.HasValue &&
                          ((x.MaxDeadline.Value.HasTime() && x.MaxDeadline.Value <= Calendar.Now) ||
                           (!x.MaxDeadline.Value.HasTime() && x.MaxDeadline.Value.EndOfDay() <= Calendar.Now))).ToList();
    }
    
    /// <summary>
    /// Получить поручения со сроком сегодня для роли Автор.
    /// </summary>
    /// <param name="users">Авторы поручений.</param>
    /// <param name="priority">Приоритет поручений.</param>
    /// <param name="isEscalated">Признак эскалированности.</param>
    /// <returns>Задачи.</returns>
    [Remote]
    public List<Solution.IActionItemExecutionTask> GetTaskDeadlineLess1DayByAuthor(List<IUser> users, int? priority, bool? isEscalated)
    {
      var result = GetActionItemTasks(priority, isEscalated);
      return result.Where(x => users.Contains(x.AssignedBy) && x.MaxDeadline.HasValue &&
                          ((x.MaxDeadline.Value.HasTime() && x.MaxDeadline.Value > Calendar.Now) ||
                           (!x.MaxDeadline.Value.HasTime() && x.MaxDeadline.Value.EndOfDay() > Calendar.Now)) &&
                          WorkingTime.GetDurationInWorkingHours(Calendar.Now,
                                                                x.MaxDeadline.Value.HasTime() ? x.MaxDeadline.Value : x.MaxDeadline.Value.EndOfDay(), x.Initiator) < 8).ToList();
    }
    
    /// <summary>
    /// Получить поручения со сроком больше одного дня для роли Автор.
    /// </summary>
    /// <param name="users">Авторы поручений.</param>
    /// <param name="priority">Приоритет поручений.</param>
    /// <param name="isEscalated">Признак эскалированности.</param>
    /// <returns>Задачи.</returns>
    [Remote]
    public List<Solution.IActionItemExecutionTask> GetTaskDeadlineMore1DayByAuthor(List<IUser> users, int? priority, bool? isEscalated)
    {
      var result = GetActionItemTasks(priority, isEscalated);
      return result.Where(x => users.Contains(x.AssignedBy) && x.MaxDeadline.HasValue &&
                          ((x.MaxDeadline.Value.HasTime() && x.MaxDeadline.Value > Calendar.Now) ||
                           (!x.MaxDeadline.Value.HasTime() && x.MaxDeadline.Value.EndOfDay() > Calendar.Now)) &&
                          WorkingTime.GetDurationInWorkingHours(Calendar.Now,
                                                                x.MaxDeadline.Value.HasTime() ? x.MaxDeadline.Value : x.MaxDeadline.Value.EndOfDay(), x.Initiator) >= 8).ToList();
    }
    
    /// <summary>
    /// Получить выполненные поручения для роли Автор.
    /// </summary>
    /// <param name="users">Авторы поручений.</param>
    /// <param name="priority">Приоритет поручений.</param>
    /// <param name="isEscalated">Признак эскалированности.</param>
    /// <returns>Задачи.</returns>
    [Remote]
    public List<Solution.IActionItemExecutionTask> GetExecutedTaskByAuthor(List<IUser> users, int? priority, bool? isEscalated)
    {
      var result = new List<Solution.IActionItemExecutionTask>();
      foreach(var user in users)
      {
        result.AddRange(Solution.ActionItemExecutionTasks.GetAll(x => Users.Equals(x.AssignedBy, user) &&
                                                                 (priority == null || x.Priority.PriorityValue == priority) &&
                                                                 (isEscalated == null || x.IsEscalated == isEscalated) &&
                                                                 (x.ExecutionState.Equals(Solution.ActionItemExecutionTask.ExecutionState.Executed) ||
                                                                  x.ExecutionState.Equals(Solution.ActionItemExecutionTask.ExecutionState.OnControl))).ToList());
      }
      return result;
    }
    #endregion
    #region Для роли Постановщик.
    /// <summary>
    /// Получить просроченные поручения для роли Постановщик.
    /// </summary>
    /// <param name="users">Авторы поручений.</param>
    /// <param name="priority">Приоритет поручений.</param>
    /// <param name="isEscalated">Признак эскалированности.</param>
    /// <returns>Задачи.</returns>
    [Remote]
    public List<Solution.IActionItemExecutionTask> GetOverdueTaskByInitiator(List<IUser> users, int? priority, bool? isEscalated)
    {
      var result = GetActionItemTasks(priority, isEscalated);
      return result.Where(x => users.Contains(x.Initiator) && x.MaxDeadline.HasValue &&
                          ((x.MaxDeadline.Value.HasTime() && x.MaxDeadline.Value <= Calendar.Now) ||
                           (!x.MaxDeadline.Value.HasTime() && x.MaxDeadline.Value.EndOfDay() <= Calendar.Now))).ToList();
    }
    
    /// <summary>
    /// Получить поручения со сроком сегодня для роли Постановщик.
    /// </summary>
    /// <param name="users">Авторы поручений.</param>
    /// <param name="priority">Приоритет поручений.</param>
    /// <param name="isEscalated">Признак эскалированности.</param>
    /// <returns>Задачи.</returns>
    [Remote]
    public List<Solution.IActionItemExecutionTask> GetTaskDeadlineLess1DayByInitiator(List<IUser> users, int? priority, bool? isEscalated)
    {
      var result = GetActionItemTasks(priority, isEscalated);
      return result.Where(x => users.Contains(x.Initiator) && x.MaxDeadline.HasValue &&
                          ((x.MaxDeadline.Value.HasTime() && x.MaxDeadline.Value > Calendar.Now) ||
                           (!x.MaxDeadline.Value.HasTime() && x.MaxDeadline.Value.EndOfDay() > Calendar.Now)) &&
                          WorkingTime.GetDurationInWorkingHours(Calendar.Now,
                                                                x.MaxDeadline.Value.HasTime() ? x.MaxDeadline.Value : x.MaxDeadline.Value.EndOfDay(), x.Initiator) < 8).ToList();
    }
    
    /// <summary>
    /// Получить поручения со сроком более одного дня для роли Постановщик.
    /// </summary>
    /// <param name="users">Авторы поручений.</param>
    /// <param name="priority">Приоритет поручений.</param>
    /// <param name="isEscalated">Признак эскалированности.</param>
    /// <returns>Задачи.</returns>
    [Remote]
    public List<Solution.IActionItemExecutionTask> GetTaskDeadlineMore1DayByInitiator(List<IUser> users, int? priority, bool? isEscalated)
    {
      var result = GetActionItemTasks(priority, isEscalated);
      return result.Where(x => users.Contains(x.Initiator) && x.MaxDeadline.HasValue &&
                          ((x.MaxDeadline.Value.HasTime() && x.MaxDeadline.Value > Calendar.Now) ||
                           (!x.MaxDeadline.Value.HasTime() && x.MaxDeadline.Value.EndOfDay() > Calendar.Now)) &&
                          WorkingTime.GetDurationInWorkingHours(Calendar.Now,
                                                                x.MaxDeadline.Value.HasTime() ? x.MaxDeadline.Value : x.MaxDeadline.Value.EndOfDay(), x.Initiator) >= 8).ToList();
    }
    
    /// <summary>
    /// Получить выполненные поручения для роли Постановщик.
    /// </summary>
    /// <param name="users">Авторы поручений.</param>
    /// <param name="priority">Приоритет поручений.</param>
    /// <param name="isEscalated">Признак эскалированности.</param>
    /// <returns>Задачи.</returns>
    [Remote]
    public List<Solution.IActionItemExecutionTask> GetExecutedTaskByInitiator(List<IUser> users, int? priority, bool? isEscalated)
    {
      var result = new List<Solution.IActionItemExecutionTask>();
      foreach(var user in users)
      {
        result.AddRange(Solution.ActionItemExecutionTasks.GetAll(x => Users.Equals(x.Initiator, user) &&
                                                                 (priority == null || x.Priority.PriorityValue == priority) &&
                                                                 (isEscalated == null || x.IsEscalated == isEscalated) && x.MaxDeadline.HasValue &&
                                                                 (x.ExecutionState.Equals(Solution.ActionItemExecutionTask.ExecutionState.Executed) ||
                                                                  x.ExecutionState.Equals(Solution.ActionItemExecutionTask.ExecutionState.OnControl))).ToList());
      }
      return result;
    }
    #endregion
    #region Для роли Исполнитель.
    /// <summary>
    /// Получить просроченные поручения для роли Исполнитель.
    /// </summary>
    /// <param name="users">Авторы поручений.</param>
    /// <param name="priority">Приоритет поручений.</param>
    /// <param name="isEscalated">Признак эскалированности.</param>
    /// <returns>Задания.</returns>
    [Remote]
    public List<Solution.IActionItemExecutionAssignment> GetOverdueTaskByPerformer(List<IUser> users, int? priority, bool? isEscalated)
    {
      var result = GetActionItemAssignments(priority, isEscalated);
      return result.Where(x => users.Contains(x.Performer) && x.Deadline.HasValue &&
                          ((x.Deadline.Value.HasTime() && x.Deadline.Value <= Calendar.Now) ||
                           (!x.Deadline.Value.HasTime() && x.Deadline.Value.EndOfDay() <= Calendar.Now))).ToList();
    }
    
    /// <summary>
    /// Получить поручения со сроком сегодня для роли Исполнитель.
    /// </summary>
    /// <param name="users">Авторы поручений.</param>
    /// <param name="priority">Приоритет поручений.</param>
    /// <param name="isEscalated">Признак эскалированности.</param>
    /// <returns>Задания.</returns>
    [Remote]
    public List<Solution.IActionItemExecutionAssignment> GetTaskDeadlineLess1DayByPerformer(List<IUser> users, int? priority, bool? isEscalated)
    {
      var result = GetActionItemAssignments(priority, isEscalated);
      return result.Where(x => users.Contains(x.Performer) && x.Deadline.HasValue &&
                          ((x.Deadline.Value.HasTime() && x.Deadline.Value > Calendar.Now) ||
                           (!x.Deadline.Value.HasTime() && x.Deadline.Value.EndOfDay() > Calendar.Now)) &&
                          WorkingTime.GetDurationInWorkingHours(Calendar.Now,
                                                                x.Deadline.Value.HasTime() ? x.Deadline.Value : x.Deadline.Value.EndOfDay(), x.Initiator) < 8).ToList();
    }
    
    /// <summary>
    /// Получить поручения со сроком более одного дня для роли Исполнитель.
    /// </summary>
    /// <param name="users">Авторы поручений.</param>
    /// <param name="priority">Приоритет поручений.</param>
    /// <param name="isEscalated">Признак эскалированности.</param>
    /// <returns>Задания.</returns>
    [Remote]
    public List<Solution.IActionItemExecutionAssignment> GetTaskDeadlineMore1DayByPerformer(List<IUser> users, int? priority, bool? isEscalated)
    {
      var result = GetActionItemAssignments(priority, isEscalated);
      return result.Where(x => users.Contains(x.Performer) && x.Deadline.HasValue &&
                          ((x.Deadline.Value.HasTime() && x.Deadline.Value > Calendar.Now) ||
                           (!x.Deadline.Value.HasTime() && x.Deadline.Value.EndOfDay() > Calendar.Now)) &&
                          WorkingTime.GetDurationInWorkingHours(Calendar.Now,
                                                                x.Deadline.Value.HasTime() ? x.Deadline.Value : x.Deadline.Value.EndOfDay(), x.Initiator) >= 8).ToList();
    }
    
    /// <summary>
    /// Получить выполненные поручения для роли Исполнитель.
    /// </summary>
    /// <param name="users">Авторы поручений.</param>
    /// <param name="priority">Приоритет поручений.</param>
    /// <param name="isEscalated">Признак эскалированности.</param>
    /// <returns>Задания.</returns>
    [Remote]
    public List<Solution.IActionItemExecutionAssignment> GetExecutedTaskByPerformer(List<IUser> users, int? priority, bool? isEscalated)
    {
      var result = new List<Solution.IActionItemExecutionAssignment>();
      foreach(var user in users)
      {
        result.AddRange(Solution.ActionItemExecutionAssignments.GetAll(x => Users.Equals(x.Performer, user) &&
                                                                       (priority == null || x.Priority.PriorityValue == priority) &&
                                                                       (isEscalated == null || x.IsEscalated == isEscalated) && x.Deadline.HasValue &&
                                                                       x.Status.Equals(Solution.ActionItemExecutionAssignment.Status.Completed)).ToList());
      }
      return result;
    }
    #endregion
    #region Для роли Руководитель Исполнителя.
    /// <summary>
    /// Получить просроченные поручения для роли Руководитель Исполнителя.
    /// </summary>
    /// <param name="users">Авторы поручений.</param>
    /// <param name="priority">Приоритет поручений.</param>
    /// <param name="isEscalated">Признак эскалированности.</param>
    /// <returns>Задания.</returns>
    [Remote]
    public List<Solution.IActionItemExecutionAssignment> GetOverdueTaskByManager(List<IUser> users, int? priority, bool? isEscalated)
    {
      var result = GetActionItemAssignments(priority, isEscalated);
      return result.Where(x => users.Contains(PublicFunctions.ActionItemsRole.Remote
                                              .GetRolePerformer(ActionItems.ActionItemsRoles.GetAll(r => r.Type == ActionItems.ActionItemsRole.Type.InitManager)
                                                                .FirstOrDefault(), Solution.Employees.As(x.Performer))) &&
                          x.Deadline.HasValue && ((x.Deadline.Value.HasTime() && x.Deadline.Value <= Calendar.Now) ||
                                                  (!x.Deadline.Value.HasTime() && x.Deadline.Value.EndOfDay() <= Calendar.Now))).ToList();

    }
    
    /// <summary>
    /// Получить поручения со сроком сегодня для роли Руководитель Исполнителя.
    /// </summary>
    /// <param name="users">Авторы поручений.</param>
    /// <param name="priority">Приоритет поручений.</param>
    /// <param name="isEscalated">Признак эскалированности.</param>
    /// <returns>Задания.</returns>
    [Remote]
    public List<Solution.IActionItemExecutionAssignment> GetTaskDeadlineLess1DayByManager(List<IUser> users, int? priority, bool? isEscalated)
    {
      var result = GetActionItemAssignments(priority, isEscalated);
      return result.Where(x => users.Contains(PublicFunctions.ActionItemsRole.Remote
                                              .GetRolePerformer(ActionItems.ActionItemsRoles.GetAll(r => r.Type == ActionItems.ActionItemsRole.Type.InitManager)
                                                                .FirstOrDefault(), Solution.Employees.As(x.Performer))) &&
                          x.Deadline.HasValue && ((x.Deadline.Value.HasTime() && x.Deadline.Value > Calendar.Now) ||
                                                  (!x.Deadline.Value.HasTime() && x.Deadline.Value.EndOfDay() > Calendar.Now)) &&
                          WorkingTime.GetDurationInWorkingHours(Calendar.Now,
                                                                x.Deadline.Value.HasTime() ? x.Deadline.Value : x.Deadline.Value.EndOfDay(), x.Initiator) < 8).ToList();
    }
    
    /// <summary>
    /// Получить поручения со сроком более одного дня для роли Руководитель Исполнителя.
    /// </summary>
    /// <param name="users">Авторы поручений.</param>
    /// <param name="priority">Приоритет поручений.</param>
    /// <param name="isEscalated">Признак эскалированности.</param>
    /// <returns>Задания.</returns>
    [Remote]
    public List<Solution.IActionItemExecutionAssignment> GetTaskDeadlineMore1DayByManager(List<IUser> users, int? priority, bool? isEscalated)
    {
      var result = GetActionItemAssignments(priority, isEscalated);
      return result.Where(x => users.Contains(PublicFunctions.ActionItemsRole.Remote
                                              .GetRolePerformer(ActionItems.ActionItemsRoles.GetAll(r => r.Type == ActionItems.ActionItemsRole.Type.InitManager)
                                                                .FirstOrDefault(), Solution.Employees.As(x.Performer))) &&
                          x.Deadline.HasValue && ((x.Deadline.Value.HasTime() && x.Deadline.Value > Calendar.Now) ||
                                                  (!x.Deadline.Value.HasTime() && x.Deadline.Value.EndOfDay() > Calendar.Now)) &&
                          WorkingTime.GetDurationInWorkingHours(Calendar.Now,
                                                                x.Deadline.Value.HasTime() ? x.Deadline.Value : x.Deadline.Value.EndOfDay(), x.Initiator) >= 8).ToList();
    }
    
    /// <summary>
    /// Получить выполненные поручения для роли Руководитель Исполнителя.
    /// </summary>
    /// <param name="users">Авторы поручений.</param>
    /// <param name="priority">Приоритет поручений.</param>
    /// <param name="isEscalated">Признак эскалированности.</param>
    /// <returns>Задания.</returns>
    [Remote]
    public List<Solution.IActionItemExecutionAssignment> GetExecutedTaskByManager(List<IUser> users, int? priority, bool? isEscalated)
    {
      var result = new List<Solution.IActionItemExecutionAssignment>();
      foreach(var user in users)
      {
        result.AddRange(Solution.ActionItemExecutionAssignments.GetAll(x => (priority == null || x.Priority.PriorityValue == priority) &&
                                                                       (isEscalated == null || x.IsEscalated == isEscalated) && x.Deadline.HasValue &&
                                                                       x.Status.Equals(Solution.ActionItemExecutionAssignment.Status.Completed)).ToList()
                        .Where(z => Users.Equals(ActionItems.PublicFunctions.ActionItemsRole.Remote
                                                 .GetRolePerformer(ActionItems.ActionItemsRoles.GetAll(r => r.Type == ActionItems.ActionItemsRole.Type.InitManager)
                                                                   .FirstOrDefault(), Solution.Employees.As(z.Performer)), user)));
      }
      return result;
    }
    #endregion
    /// <summary>
    /// Получить замещения пользователя.
    /// </summary>
    /// <param name="user">Замещающий пользователь.</param>
    /// <returns>Замещаемые пользователи.</returns>
    [Remote]
    public List<IUser> GetSubstitutionUsers(IUser user)
    {
      return Substitutions.GetAll(x => Users.Equals(user, x.Substitute) && x.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
        .Select(x => x.User).ToList();
    }
    
    /// <summary>
    /// Получить активные задачи по поручениям.
    /// </summary>
    /// <param name="priority">Приоритет.</param>
    /// <param name="isEscalated">Признак эскалации.</param>
    /// <returns>Задачи.</returns>
    public List<Solution.IActionItemExecutionTask> GetActionItemTasks(int? priority, bool? isEscalated)
    {
      return Solution.ActionItemExecutionTasks.GetAll(x => (priority == null || x.Priority.PriorityValue == priority) &&
                                                      (isEscalated == null || x.IsEscalated == isEscalated) &&
                                                      (x.Status.Equals(Solution.ActionItemExecutionTask.Status.UnderReview) ||
                                                       x.Status.Equals(Solution.ActionItemExecutionTask.Status.InProcess))).ToList();
    }
    
    /// <summary>
    /// Получить активные задания по поручениям.
    /// </summary>
    /// <param name="priority">Приоритет.</param>
    /// <param name="isEscalated">Признак эскалации.</param>
    /// <returns>Задания.</returns>
    public List<Solution.IActionItemExecutionAssignment> GetActionItemAssignments(int? priority, bool? isEscalated)
    {
      return Solution.ActionItemExecutionAssignments.GetAll(x => (priority == null || x.Priority.PriorityValue == priority) &&
                                                            (isEscalated == null || x.IsEscalated == isEscalated) &&
                                                            x.Status.Equals(Solution.ActionItemExecutionAssignment.Status.InProcess)).ToList();
    }
    #endregion
    
    /// <summary>
    /// Данные для печати поручения.
    /// </summary>
    /// <param name="employee">Поручение для обработки.</param>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public static List<Structures.PrintActionItemTask.ResponsibilitiesReportTableLine> GetAllAssignmentsReportData(Solution.IActionItemExecutionTask task)
    {
      var result = new List<Structures.PrintActionItemTask.ResponsibilitiesReportTableLine>();
      
      if (task == null)
        return result;
      
      var assignments = Solution.ActionItemExecutionAssignments.GetAll(a => DirRX.Solution.ActionItemExecutionTasks.Equals(a.MainTask, task) &&
                                                                       a.Status != Sungero.RecordManagement.ActionItemExecutionAssignment.Status.Aborted);
      
      foreach (var assignment in assignments)
      {
        var actionItemTask = DirRX.Solution.ActionItemExecutionTasks.As(assignment.Task);
        var newLine = new Structures.PrintActionItemTask.ResponsibilitiesReportTableLine();
        
        // Исполнитель.
        var performer = DirRX.Solution.Employees.As(assignment.Performer);
        if (performer != null)
          newLine.Assignee = string.Format("{0}{1}{1}{2}", performer.DisplayValue,
                                           Environment.NewLine,
                                           performer.JobTitle != null ? performer.JobTitle.DisplayValue : string.Empty);
        
        // Текст поручения.
        if (actionItemTask.ActionItemType == Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Additional)
          newLine.ActionItem = Reports.Resources.PrintActionItemTask.AdditionalType;
        else
          newLine.ActionItem = assignment.ActionItem;
        
        // Срок.
        if (assignment.Deadline.HasValue && actionItemTask.ActionItemType != Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Additional)
          newLine.Deadline = assignment.Deadline.Value.HasTime() ? assignment.Deadline.ToUserTime().Value.ToString("g") : assignment.Deadline.Value.ToString("d");
        
        // Статус.
        if (assignment.IsEscalated.GetValueOrDefault())
        {
          var escalatedTask = DirRX.ActionItems.ActionItemEscalatedTasks.GetAll().ToList()
            .Where(t => Sungero.RecordManagement.ActionItemExecutionAssignments.Equals(t.AttachmentGroup.ActionItemExecutionAssignments.FirstOrDefault(), assignment))
            .FirstOrDefault();
          
          if (escalatedTask != null && escalatedTask.Started.HasValue)
            newLine.Status = string.Format("{0} {1}", DirRX.ActionItems.Resources.Escalated, escalatedTask.Started.Value.ToString("g"));
          else
            newLine.Status = DirRX.ActionItems.Resources.Escalated;
          
          newLine.Status += Environment.NewLine + Environment.NewLine;
        }
        
        if (assignment.Status == Sungero.RecordManagement.ActionItemExecutionAssignment.Status.Completed)
        {
          var statusText = new System.Text.StringBuilder();
          statusText.AppendLine(string.Format("{0} {1}", Sungero.RecordManagement.ActionItemExecutionAssignments.Info.Properties.Status.GetLocalizedValue(assignment.Status),
                                              assignment.Completed.Value.ToString("g")))
            .AppendLine()
            .AppendLine(string.Format("{0}: {1}", DirRX.ActionItems.Resources.Result, assignment.ActiveText))
            .AppendLine();
          
          if (assignment.ResultGroup.OfficialDocuments.Any())
          {
            statusText.AppendLine(DirRX.ActionItems.Resources.ResultDocs);
            
            foreach (var document in assignment.ResultGroup.OfficialDocuments)
            {
              statusText.AppendLine(document.DisplayValue)
                .AppendLine(string.Format("({0} {1})", Reports.Resources.PrintActionItemTask.ID, document.Id))
                .AppendLine();
            }
          }
          
          if (actionItemTask.Status == DirRX.Solution.ActionItemExecutionTask.Status.InProcess)
            statusText.AppendLine(DirRX.ActionItems.Resources.ActionItemOnControl);
          
          newLine.Status += statusText.ToString();
        }
        else
          newLine.Status += Sungero.RecordManagement.ActionItemExecutionAssignments.Info.Properties.Status.GetLocalizedValue(assignment.Status);
        
        result.Add(newLine);
      }
      
      return result;
    }
    
    /// <summary>
    /// Получить поручение по ведущей задаче.
    /// </summary>
    /// <param name="task">Ведущая задача.</param>
    /// <returns>Поручение.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<DirRX.Solution.IActionItemExecutionTask> GetActionItemTaskWithMainTask(Sungero.Workflow.ITask task)
    {
      return DirRX.Solution.ActionItemExecutionTasks.GetAll(t => Equals(t.MainTask, task));
    }
    
  }
}