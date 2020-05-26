using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ActionItemExecutionAssignment;

namespace DirRX.Solution.Server
{

  partial class ActionItemExecutionAssignmentFunctions
  {
    
    /// <summary>
    /// Построить модель состояния пояснения.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Модель состояния.</returns>
    [Remote(IsPure = true)]
    public static Sungero.Core.StateView GetActionItemExecutionAssignmentStateView1(IActionItemExecutionAssignment assignment)
    {
      var stateView = Sungero.Core.StateView.Create();
      var block = stateView.AddBlock();
      var content = block.AddContent();
      
      content.AddLabel(GetDescription(assignment));
      
      content.AddLineBreak();
      content.AddLabel(String.Format("{0}: {1}", assignment.Info.Properties.Category.LocalizedName, assignment.Category.Name));
      
      if (assignment.Mark != null)
      {
        content.AddLineBreak();
        content.AddLabel(String.Format("{0}: {1}", assignment.Info.Properties.Mark.LocalizedName, assignment.Mark.Name));
      }
      content.AddLineBreak();
      content.AddLabel(String.Format("{0}: {1}", assignment.Info.Properties.Priority.LocalizedName, assignment.Priority.PriorityValue.GetValueOrDefault().ToString()));
      if (assignment.ReportDeadline != null)
      {
        content.AddLineBreak();
        content.AddLabel(String.Format("{0}: {1}", assignment.Info.Properties.ReportDeadline.LocalizedName, assignment.ReportDeadline.Value));
      }
      
      block.ShowBorder = false;
      
      return stateView;
    }
    
    /// <summary>
    /// Получить пояснение к заданию.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Пояснение.</returns>
    private static string GetDescription(IActionItemExecutionAssignment assignment)
    {
      var description = string.Empty;
      
      var mainTask = ActionItemExecutionTasks.As(assignment.Task);
      
      if (mainTask == null)
        return description;
      
      var supervisor = mainTask.Supervisor;
      
      if (supervisor != null)
        description += (mainTask.ActionItemType == Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Additional)
          ? Sungero.RecordManagement.ActionItemExecutionTasks.Resources.OnControlWithResponsibleFormat(Sungero.Company.PublicFunctions.Employee.GetShortName(supervisor, false).TrimEnd('.'))
          : Sungero.RecordManagement.ActionItemExecutionTasks.Resources.OnControlFormat(Sungero.Company.PublicFunctions.Employee.GetShortName(supervisor, false).TrimEnd('.'));
      
      var currentEmployee = DirRX.Solution.Employees.As(Users.Current);
      var escalateManager = ActionItems.PublicFunctions.Module.Remote.GetEscalatedManager(Solution.ActionItemExecutionTasks.As(mainTask));
      var isEscalateManager = DirRX.Solution.Employees.Equals(escalateManager, currentEmployee);
      
      if (!isEscalateManager)
      {
        if (mainTask.ActionItemType == Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Additional)
        {
          description += Sungero.RecordManagement.ActionItemExecutionTasks.Resources.YouAreAdditionalAssignee;
        }
        else
        {
          if (mainTask.ActionItemType == Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Main && mainTask.CoAssignees.Any() && !mainTask.CoAssignees.Any(ca => Equals(ca.Assignee, assignment.Performer)))
            description += Sungero.RecordManagement.ActionItemExecutionTasks.Resources.YouAreResponsibleAssignee;
          else
            description += Sungero.RecordManagement.ActionItemExecutionTasks.Resources.YouAreAssignee;
        }
      }
      
      return description;
    }
    
    /// <summary>
    /// Создать новую задачу на исполнение поручения при изменении исполнителя или соисполнителей.
    /// </summary>
    /// <param name="assignment">Текущее задание.</param>
    /// <param name="performer">Новый исполнитель.</param>
    /// <param name="coPerformers">Новый состав соисполнителей.</param>
    /// <returns>Новая задача.</returns>
    [Remote]
    public static void StartNewPerformerAssignment(IActionItemExecutionAssignment assignment, IEmployee performer, List<IEmployee> coPerformers)
    {
      bool withCoPerformers = coPerformers.Count > 0;

      var currentTask = ActionItemExecutionTasks.As(assignment.Task);
      string abortingReason = ActionItemExecutionAssignments.Resources.ChangePerformerActiveTextFormat(Users.Current.DisplayValue, performer.DisplayValue);
      currentTask.AbortingReason = abortingReason;
      
      // Отправить уведомление Исполнителю и Контролеру о прекращении.
      List<IUser> recipients = new List<IUser>() { assignment.Performer };
      if (currentTask.Supervisor != null)
        recipients.Add(currentTask.Supervisor);
      string threadSubject = Sungero.RecordManagement.ActionItemExecutionTasks.Resources.NoticeSubjectWithoutDoc;
      string noticesSubject = string.Format("{0} {1}", threadSubject, currentTask.Subject);
      Sungero.Docflow.PublicFunctions.Module.Remote.SendNoticesAsSubtask(noticesSubject, recipients, currentTask, abortingReason, null, threadSubject);
      
      // Прекратить задачи соисполнителям.
      var subAssignments = GetSubAssignments(assignment, DirRX.Solution.ActionItemExecutionTask.ActionItemType.Additional);
      if (subAssignments.Any())
        AbortSubAssignments(subAssignments);
      
      // Прекратить основную задачу.
      currentTask.Abort();
      
      // Дописать сообщение об изменении исполнителя.
      string jobActiveText = abortingReason;
      if (!string.IsNullOrEmpty(assignment.ActiveText))
        jobActiveText = string.Format("{0}{1}{2}", assignment.ActiveText, Environment.NewLine, abortingReason);
      assignment.ActiveText = jobActiveText;
      assignment.Save();
      
      if (currentTask.CoAssignees != null)
      {
        currentTask.CoAssignees.Clear();
        if (withCoPerformers)
          foreach (DirRX.Solution.IEmployee coperformer in coPerformers)
            currentTask.CoAssignees.AddNew().Assignee = coperformer;
      }
      
      currentTask.Assignee = performer;
      currentTask.Restart();
      currentTask.Start();
    }
    
    /// <summary>
    /// Получить список подзадач по поручению.
    /// </summary>
    /// <param name="assignment">Текущее задание.</param>
    /// <param name="isCoPerformersType">Признак типа подзадачи (соисполнителю или подчиненное поручение).</param>
    /// <returns>Список найденных подзадач.</returns>
    [Remote(IsPure = true)]
    public static List<IActionItemExecutionTask> GetSubAssignments(IActionItemExecutionAssignment assignment, Enumeration assignmentType)
    {
      return ActionItemExecutionTasks.GetAll().Where(a => ActionItemExecutionTasks.Is(a) &&
                                                     ActionItemExecutionAssignments.Equals(ActionItemExecutionAssignments.As(a.ParentAssignment), assignment) &&
                                                     a.ActionItemType == assignmentType &&
                                                     (a.Status == Sungero.Workflow.Task.Status.InProcess ||
                                                      a.Status == Sungero.Workflow.Task.Status.UnderReview)).ToList();
    }
    
    /// <summary>
    /// Прекратить подзадачи по поручению.
    /// </summary>
    /// <param name="subAssignments">Список подзадач.</param>
    [Remote]
    public static void AbortSubAssignments(List<IActionItemExecutionTask> subAssignments)
    {
      foreach (IActionItemExecutionTask subAssignment in subAssignments)
      {
        subAssignment.Abort();
      }
    }
    
    #region Скопировано из стандартной разработки.
    
    /// <summary>
    /// Проверка, все ли задания соисполнителям созданы.
    /// </summary>
    /// <returns>True, если можно выполнять задание.</returns>
    [Remote(IsPure = true)]
    public bool IsCoAllAssigneeAssignamentCreated()
    {
      var task = ActionItemExecutionTasks.As(_obj.Task);
      var taskAssignees = task.CoAssignees.Select(c => c.Assignee).Distinct().ToList();
      var asgAssignees = ActionItemExecutionAssignments
        .GetAll(j => j.Task.ParentAssignment != null &&
                Equals(task, j.Task.ParentAssignment.Task) &&
                Equals(task.StartId, j.Task.ParentAssignment.TaskStartId) &&
                Equals(ActionItemExecutionTasks.As(j.Task).ActionItemType, Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Additional))
        .Select(c => c.Performer).Distinct().ToList();
      return !taskAssignees.Any(x => !asgAssignees.Contains(x));
    }
    
    #endregion
  }
}