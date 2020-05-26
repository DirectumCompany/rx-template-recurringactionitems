using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;

namespace DirRX.ProcessSubstitutionModule.Server
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Получить все задания по процессам, где пользователь указан замещающим.
    /// </summary>
    /// <param name="query">Список заданий.</param>
    /// <param name="user">Замещающий.</param>
    /// <param name="filterAssignments">Отбирать задания.</param>
    /// <param name="filterNotices">Отбирать уведомления.</param>
    /// <param name="employee">Фильтр по замещаемому.</param>
    /// <returns>Задания по процессам, где пользователь указан замещающим.</returns>
    public virtual List<IAssignmentBase> GetAssignmentsByAllSubstitutions(IQueryable<IAssignmentBase> query,
                                                                                IUser user,
                                                                                Structures.Module.ProcessSubstitutionFilter filter)
    {
      var substitutions = GetSubstitutionList(user, filter.Employee);
      
      var assignments = new List<IAssignmentBase>();
      
      foreach (var substitution in substitutions)
      {
        var processQuery = GetAssignmentsBySubstitution(substitution, user, filter);

        if (processQuery.Any())
          assignments.AddRange(processQuery.AsEnumerable());
      }
      
      return assignments;
    }
    
    /// <summary>
    /// Получить список актуальных замещений пользователя.
    /// </summary>
    /// <param name="user">Пользователь.</param>
    /// <param name="substitutor">Замещаемый.</param>
    /// <returns>Список актуальных замещений.</returns>
    public List<IProcessSubstitution> GetSubstitutionList(IUser user, DirRX.Solution.IEmployee substitutor)
    {
      var substitutions = ProcessSubstitutions.GetAll(r => (!r.EndDate.HasValue || r.EndDate.Value >= Calendar.Today) && (!r.BeginDate.HasValue || r.BeginDate.Value <= Calendar.Today));
      substitutions = substitutions.Where(r => r.SubstitutionCollection.Any(s => Users.Equals(s.Substitute, user)));
      
      if (substitutor != null)
        substitutions = substitutions.Where(w => DirRX.Solution.Employees.Equals(w.Employee, substitutor));
      
      return substitutions.ToList();
    }
    
    /// <summary>
    /// Получить задания по конкретному замещению.
    /// </summary>
    /// <param name="substitution">Замещение.</param>
    /// <param name="user">Замещающий.</param>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Задания по конкретному замещению.</returns>
    public List<IAssignmentBase> GetAssignmentsBySubstitution(IProcessSubstitution substitution, IUser user, Structures.Module.ProcessSubstitutionFilter filter)
    {
      var assignmentsByAllProcesses = AssignmentBases.GetAll()
        .Where(a => Users.Equals(a.Performer, substitution.Employee))
        .Where(a => a.Created >= Calendar.Today.AddDays(-filter.DayCount));
      
      if (filter.InWork == true || filter.Completed == true)
        assignmentsByAllProcesses = assignmentsByAllProcesses
          .Where(a => filter.InWork && a.Status.Value == Sungero.Workflow.Assignment.Status.InProcess || filter.Completed && a.Status.Value == Sungero.Workflow.Assignment.Status.Completed);

      if (filter.ShowAssignments == true || filter.ShowNotices == true)
        assignmentsByAllProcesses = assignmentsByAllProcesses
          .Where(a => filter.ShowAssignments && Assignments.Is(a) || filter.ShowNotices && Notices.Is(a));

      if (substitution.BeginDate.HasValue)
        assignmentsByAllProcesses = assignmentsByAllProcesses
          .Where(a => a.Created >= substitution.BeginDate.Value);

      var mainQuery = new List<IAssignmentBase>();
      var processList = GetCurrentProcessList(substitution, user);
      foreach (var process in processList)
      {
        var processQuery = GetAssignmentsByProcess(assignmentsByAllProcesses, process);
        if (processQuery.Any())
        {
          if (mainQuery == null)
            mainQuery = processQuery.ToList();
          else
            mainQuery.AddRange(processQuery.ToList());
        }
      }
      
      return mainQuery.Distinct().ToList();
    }
    
    /// <summary>
    /// Получить список процессов для замещающего.
    /// </summary>
    /// <param name="substitution">Замещение.</param>
    /// <param name="user">Замещающий.</param>
    /// <returns>Список процессов для замещающего.</returns>
    public List<Enumeration> GetCurrentProcessList(IProcessSubstitution substitution, IUser user)
    {
      var rows = substitution.SubstitutionCollection
        .Where(s => Users.Equals(s.Substitute, user));
      
      if (rows.Where(r => !r.Process.HasValue).Any())
      {
        var existingProcessList = substitution.SubstitutionCollection
          .Where(s => !Users.Equals(s.Substitute, user))
          .Where(s => s.Process.HasValue)
          .Select(s => s.Process.Value)
          .ToList();
        return GetFullProcessList().Where(p => !existingProcessList.Contains(p) || rows.Where(r => r.Process.HasValue && r.Process.Value == p).Any()).ToList();
      }
      else
        return rows.Select(r => r.Process.Value).ToList();
    }
    
    /// <summary>
    /// Получить полный список процессов.
    /// </summary>
    /// <returns>Полный список процессов.</returns>
    public List<Enumeration> GetFullProcessList()
    {
      return new List<Enumeration> {
        ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.Assignments,
        ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.Orders,
        ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.OrderRiskConfir,
        ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.Contracts,
        ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.ContractRiskCon,
        ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.Others,
      };
    }
    
    /// <summary>
    /// Получить задания по замещению по одному процессу.
    /// </summary>
    /// <param name="allAssignments">Задания, отфильтрованные по общим параметрам.</param>
    /// <param name="process">Процесс.</param>
    /// <returns>Задания по замещению и по одному процессу.</returns>
    private IQueryable<IAssignmentBase> GetAssignmentsByProcess(IQueryable<IAssignmentBase> allAssignments, Enumeration process)
    {
      var approvalTaskDocumentGroupGuid = Constants.Module.ApprovalTaskDocGuid;
      var ordersGuid = Constants.Module.OrdersGuid;
      var contractsGuid = Constants.Module.ContractsGuid;
      var allAssignmentsList = allAssignments
        .ToList();
      var assigmentsWithAttachedTasks = allAssignmentsList
        .Where(a => a.Task.Attachments.Where(att => Tasks.Is(att)).Any());
      var assigmentsWithAttachedAssigments = allAssignmentsList
        .Where(a => a.Task.Attachments.Where(att => AssignmentBases.Is(att)).Any());

      // Поручения.
      if (process == ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.Assignments)
      {
        var assigments = ApplyAssignmentFilter(allAssignments).ToList();
        var assigmentsWithProcessAssigment = assigmentsWithAttachedAssigments
          .Where(a => a.Task.Attachments
                 .Where(att => GetAssignmentProcess(AssignmentBases.As(att), AssignmentBases.As(att).Task) == process)
                 .Any())
          .ToList();

        var assigmentsWithProcessTask = assigmentsWithAttachedTasks
          .Where(a => a.Task.Attachments
                 .Where(att => GetAssignmentProcess(null, Tasks.As(att)) == process)
                 .Any())
          .ToList();

        var returnedAssigment = new List<Sungero.Workflow.IAssignmentBase>();
        returnedAssigment = assigments.Concat(assigmentsWithProcessAssigment).ToList();
        returnedAssigment = returnedAssigment.Concat(assigmentsWithProcessTask).ToList();
        return returnedAssigment.Distinct().AsQueryable();
      }
      
      // Приказы.
      if (process == ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.Orders)
      {
        var assigments = ApplyAttachmentFilter(ApplyNonRiskFilter(allAssignments), approvalTaskDocumentGroupGuid, ordersGuid).ToList();
        var assigmentsWithProcessAssigment = assigmentsWithAttachedAssigments
          .Where(a => a.Task.Attachments
                 .Where(att => GetAssignmentProcess(AssignmentBases.As(att), AssignmentBases.As(att).Task) == process)
                 .Any())
          .ToList();

        var assigmentsWithProcessTask = assigmentsWithAttachedTasks
          .Where(a => a.Task.Attachments
                 .Where(att => GetAssignmentProcess(null, Tasks.As(att)) == process)
                 .Any())
          .ToList();
        
        var returnedAssigment = new List<Sungero.Workflow.IAssignmentBase>();
        returnedAssigment = assigments.Concat(assigmentsWithProcessAssigment).ToList();
        returnedAssigment = returnedAssigment.Concat(assigmentsWithProcessTask).ToList();
        return returnedAssigment.Distinct().AsQueryable();
      }
      
      // Приказы. Подтверждение рисков.
      if (process == ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.OrderRiskConfir)
        return ApplyAttachmentFilter(ApplyRiskFilter(ApplyTaskFilter(allAssignments)), approvalTaskDocumentGroupGuid, ordersGuid);

      // Договоры.
      if (process == ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.Contracts)
        return ApplyAttachmentFilter(ApplyNonRiskFilter(allAssignments), approvalTaskDocumentGroupGuid, contractsGuid);

      // Договоры. Подтверждение рисков.
      if (process == ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.ContractRiskCon)
        return ApplyAttachmentFilter(ApplyRiskFilter(ApplyTaskFilter(allAssignments)), approvalTaskDocumentGroupGuid, contractsGuid);

      // Прочие.
      if (process == ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.Others)
      {
        var assigments = allAssignments
          .Where(t => !(t.Task.AttachmentDetails.Any(att => att.AttachmentTypeGuid == contractsGuid || att.AttachmentTypeGuid == ordersGuid )))
          .Where(t => !(t.MainTask != null && t.MainTask.AttachmentDetails.Any(att => att.AttachmentTypeGuid == contractsGuid || att.AttachmentTypeGuid == ordersGuid)))
          .Where(w => !(DirRX.Solution.ActionItemExecutionTasks.Is(w) || (w.MainTask != null && DirRX.Solution.ActionItemExecutionTasks.Is(w.MainTask)) ||
                        DirRX.Solution.DeadlineExtensionAssignments.Is(w) || (w.MainTask != null && DirRX.Solution.DeadlineExtensionAssignments.Is(w.MainTask)) ||
                        DirRX.Solution.StatusReportRequestTasks.Is(w) || (w.MainTask != null && DirRX.Solution.StatusReportRequestTasks.Is(w.MainTask)) ||
                        DirRX.ActionItems.ActionItemEscalatedTasks.Is(w) || (w.MainTask != null && DirRX.ActionItems.ActionItemEscalatedTasks.Is(w.MainTask)) ||
                        DirRX.ActionItems.ActionItemRejectionTasks.Is(w) || (w.MainTask != null && DirRX.ActionItems.ActionItemRejectionTasks.Is(w.MainTask)))).ToList();

        var assigmentsWithProcessAssigment = assigmentsWithAttachedAssigments
          .Where(a => a.Task.Attachments
                 .Where(att => GetAssignmentProcess(AssignmentBases.As(att), AssignmentBases.As(att).Task) == process)
                 .Any())
          .ToList();

        var assigmentsWithProcessTask = assigmentsWithAttachedTasks
          .Where(a => a.Task.Attachments
                 .Where(att => GetAssignmentProcess(null, Tasks.As(att)) == process)
                 .Any())
          .ToList();

        var returnedAssigment = new List<Sungero.Workflow.IAssignmentBase>();
        returnedAssigment = assigments.Concat(assigmentsWithProcessAssigment).ToList();
        returnedAssigment = returnedAssigment.Concat(assigmentsWithProcessTask).ToList();
        return returnedAssigment.Distinct().AsQueryable();
      }
      return allAssignments.Where(a => false);
    }
    
    #region Фильтры
    /// <summary>
    /// Применить к списку заданий фильтр по поручениям.
    /// </summary>
    /// <param name="processQuery">Список заданий.</param>
    /// <returns>Задания по поручениям.</returns>
    private IQueryable<IAssignmentBase> ApplyAssignmentFilter(IQueryable<IAssignmentBase> processQuery)
    {
      return processQuery.Where(w => DirRX.Solution.ActionItemExecutionTasks.Is(w.Task) || (w.Task.MainTask != null && DirRX.Solution.ActionItemExecutionTasks.Is(w.Task.MainTask)) ||
                                DirRX.Solution.DeadlineExtensionAssignments.Is(w.Task) || (w.Task.MainTask != null && DirRX.Solution.DeadlineExtensionAssignments.Is(w.Task.MainTask)) ||
                                DirRX.Solution.StatusReportRequestTasks.Is(w.Task) || (w.Task.MainTask != null && DirRX.Solution.StatusReportRequestTasks.Is(w.Task.MainTask)) ||
                                DirRX.ActionItems.ActionItemEscalatedTasks.Is(w.Task) || (w.Task.MainTask != null && DirRX.ActionItems.ActionItemEscalatedTasks.Is(w.Task.MainTask)) ||
                                DirRX.ActionItems.ActionItemRejectionTasks.Is(w.Task) || (w.Task.MainTask != null && DirRX.ActionItems.ActionItemRejectionTasks.Is(w.Task.MainTask)));
    }
    
    /// <summary>
    /// Применить к списку заданий фильтр по типа задачи.
    /// </summary>
    /// <param name="processQuery">Список заданий.</param>
    /// <returns>Задания на согласование документов.</returns>
    private IQueryable<IAssignmentBase> ApplyTaskFilter(IQueryable<IAssignmentBase> processQuery)
    {
      return processQuery.Where(t => Sungero.Docflow.ApprovalTasks.Is(t.Task) || Sungero.Docflow.ApprovalTasks.Is(t.MainTask));
    }
    
    /// <summary>
    /// Применить к списку заданий фильтр по этапам без подтверждения риска.
    /// </summary>
    /// <param name="processQuery">Список заданий, которые сформированы не по этапу подтверждения рисков.</param>
    /// <returns>Задания по этапам без подтверждения риска.</returns>
    private IQueryable<IAssignmentBase> ApplyNonRiskFilter(IQueryable<IAssignmentBase> processQuery)
    {
      return processQuery.Where(t => !DirRX.Solution.ApprovalCheckingAssignments.Is(t) || DirRX.Solution.ApprovalCheckingAssignments.Is(t) &&
                                (!DirRX.Solution.ApprovalCheckingAssignments.As(t).IsRiskConfirmation.HasValue ||
                                 DirRX.Solution.ApprovalCheckingAssignments.As(t).IsRiskConfirmation.HasValue &&
                                 !DirRX.Solution.ApprovalCheckingAssignments.As(t).IsRiskConfirmation.Value));
    }
    
    /// <summary>
    /// Применить к списку заданий фильтр по этапу подтверждения риска.
    /// </summary>
    /// <param name="processQuery">Список заданий.</param>
    /// <returns>Задания по этапу подтверждения рисков.</returns>
    private IQueryable<IAssignmentBase> ApplyRiskFilter(IQueryable<IAssignmentBase> processQuery)
    {
      return processQuery.Where(t => DirRX.Solution.ApprovalCheckingAssignments.Is(t) &&
                                DirRX.Solution.ApprovalCheckingAssignments.As(t).IsRiskConfirmation.HasValue &&
                                DirRX.Solution.ApprovalCheckingAssignments.As(t).IsRiskConfirmation.Value);
    }
    
    /// <summary>
    /// Применить к списку заданий фильтр по вложениям.
    /// </summary>
    /// <param name="processQuery">Список заданий.</param>
    /// <param name="groupGuid">GUID группы вложений задачи.</param>
    /// <param name="docTypeGuid">GUID типа документа.</param>
    /// <returns>Задания с определенным типом вложения в определенной группе вложений.</returns>
    private IQueryable<IAssignmentBase> ApplyAttachmentFilter(IQueryable<IAssignmentBase> processQuery, System.Guid groupGuid, System.Guid docTypeGuid)
    {
      return processQuery.Where(t => t.Task.AttachmentDetails.Any(att => att.EntityTypeGuid == docTypeGuid)||
                                t.MainTask.AttachmentDetails.Any(att => att.EntityTypeGuid == docTypeGuid));
    }
    #endregion
    
    /// <summary>
    /// Получить список актуальных замещений.
    /// </summary>
    /// <returns>Список актуальных замещений.</returns>
    [Public]
    public List<IProcessSubstitution> GetActiveSubstitutionList()
    {
      
      var substitutions = ProcessSubstitutions.GetAll()
        .Where(s => !s.BeginDate.HasValue || s.BeginDate.HasValue && s.BeginDate.Value <= Calendar.Today)
        .Where(s => !s.EndDate.HasValue || s.EndDate.HasValue && s.EndDate.Value >= Calendar.Today)
        .ToList();
      
      return substitutions;
    }
    
    /// <summary>
    /// Определить процесс задания.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Процесс.</returns>
    [Public]
    public Enumeration GetAssignmentProcess(IAssignmentBase assignment, ITask task)
    {
      var groupGuid = Constants.Module.ApprovalTaskDocGuid;
      var ordersGuid = Constants.Module.OrdersGuid;
      var contractsGuid = Constants.Module.ContractsGuid;
      var result = ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.Others;
      
      // Поручения.
      if (DirRX.Solution.ActionItemExecutionTasks.Is(task) || (task.MainTask != null && DirRX.Solution.ActionItemExecutionTasks.Is(task.MainTask)) ||
          DirRX.Solution.DeadlineExtensionAssignments.Is(task) || (task.MainTask != null && DirRX.Solution.DeadlineExtensionAssignments.Is(task.MainTask)) ||
          DirRX.Solution.StatusReportRequestTasks.Is(task) || (task.MainTask != null && DirRX.Solution.StatusReportRequestTasks.Is(task.MainTask)) ||
          DirRX.ActionItems.ActionItemEscalatedTasks.Is(task) || (task.MainTask != null && DirRX.ActionItems.ActionItemEscalatedTasks.Is(task.MainTask)) ||
          DirRX.ActionItems.ActionItemRejectionTasks.Is(task) || (task.MainTask != null && DirRX.ActionItems.ActionItemRejectionTasks.Is(task.MainTask)))
        return ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.Assignments;
      
      
      // Этап подтверждение рисков.
      if (DirRX.Solution.ApprovalCheckingAssignments.Is(assignment) &&
          (DirRX.Solution.ApprovalCheckingAssignments.As(assignment).IsRiskConfirmation.HasValue &&
           DirRX.Solution.ApprovalCheckingAssignments.As(assignment).IsRiskConfirmation.Value))
      {
        if (task.AttachmentDetails.Any(att => att.GroupId == groupGuid && att.EntityTypeGuid == contractsGuid))
          return ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.ContractRiskCon;
        
        if (task.AttachmentDetails.Any(att => att.GroupId == groupGuid && att.EntityTypeGuid == ordersGuid))
          return ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.OrderRiskConfir;
      }
      else
      {
        if (task.AttachmentDetails.Any(att => att.EntityTypeGuid == contractsGuid) || task.MainTask.AttachmentDetails.Any(att => att.EntityTypeGuid == contractsGuid))
          return ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.Contracts;
        
        if (task.AttachmentDetails.Any(att => att.EntityTypeGuid == ordersGuid) || task.MainTask.AttachmentDetails.Any(att => att.EntityTypeGuid == ordersGuid))
          return ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.Orders;
      }
      
      return result;
    }
    
    /// <summary>
    /// Получить количество заданий по процессам, где пользователь указан замещающим.
    /// </summary>
    /// <param name="user">Замещающий.</param>
    /// <returns>Количество заданий.</returns>
    public virtual Structures.Module.AssignmentsCount GetAssignmentsCountBySubstitution(IUser user)
    {
      var count = new Structures.Module.AssignmentsCount();
      count.TotalCount = 0;
      count.UnreadedCount = 0;
      
      var substitutions = GetSubstitutionList(user, null);
      
      foreach (var substitution in substitutions)
      {
        var defaultFilter = Structures.Module.ProcessSubstitutionFilter.Create(null, true, true, true, false, 180);
        var processQuery = GetAssignmentsBySubstitution(substitution, user, defaultFilter);
        if (processQuery.Any())
        {
          count.TotalCount += processQuery.Count();
          count.UnreadedCount += processQuery.Where(a => a.IsRead == false).Count();
        }
      }

      return count;
    }
    
    /// <summary>
    /// Получить список всех действующих замещающих.
    /// </summary>
    /// <returns>Список всех замещающих на текущий момент.</returns>
    public IQueryable<IUser> GetActiveSubstituteUsers()
    {
      var userList = new List<IUser>().AsQueryable();
      var substitutions = GetActiveSubstitutionList();
      foreach (var substitute in substitutions)
        userList = userList.Union(substitute.SubstitutionCollection.Select(s => s.Substitute));
      
      return userList.Distinct();
    }
    
    /// <summary>
    /// Обновить поле Риск в заданиях.
    /// </summary>
    [Remote]
    public void UpdateRiskConfirmationField()
    {
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(Queries.Module.UpdateRiskConfirmationFieldQuery);
    }
  }
}



