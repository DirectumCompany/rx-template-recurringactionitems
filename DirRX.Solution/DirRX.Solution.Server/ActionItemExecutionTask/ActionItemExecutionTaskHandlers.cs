using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ActionItemExecutionTask;

namespace DirRX.Solution
{
  partial class ActionItemExecutionTaskCategoryPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CategoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      var document = _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
      {
      	var documentKind = DirRX.Solution.DocumentKinds.Null;
        var incomingLetter = DirRX.Solution.IncomingLetters.As(document);
        if (incomingLetter != null)
          documentKind = DirRX.Solution.DocumentKinds.As(incomingLetter.DocumentKind);
      
       var order = DirRX.Solution.Orders.As(document);
        if (order != null)
          documentKind = DirRX.Solution.DocumentKinds.As(order.StandardForm.DocumentKind);
        
        if (documentKind != null && documentKind.AssignmentsCategories.Any())
        {
          var categories = documentKind.AssignmentsCategories.Where(c => c.AssignmentCategory != null).Select(c => c.AssignmentCategory).ToList();
          query = query.Where(c => categories.Contains(c));
        }
      }
    	
      return query;
    }
  }

  partial class ActionItemExecutionTaskFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      // Вернуть нефильтрованный список, если нет фильтра. Он будет использоватьсяво всех Get() и GetAll().
      var filter = _filter;
      if (filter == null)
        return query;
      
      // Не показывать не стартованные поручения.
      query = query.Where(l => l.Status != Sungero.Workflow.Task.Status.Draft);
      
      // Не показывать составные поручения (только подзадачи).
      query = query.Where(j => j.IsCompoundActionItem == false);
      
      // Фильтр по статусу.
      var statuses = new List<Enumeration>();
      if (filter.OnExecution)
      {
        statuses.Add(ExecutionState.OnExecution);
        statuses.Add(ExecutionState.OnRework);
      }
      if (filter.OnControl)
        statuses.Add(ExecutionState.OnControl);
      if (filter.Executed)
      {
        statuses.Add(ExecutionState.Executed);
        statuses.Add(ExecutionState.Aborted);
      }
      if (statuses.Any())
        query = query.Where(q => q.ExecutionState != null && statuses.Contains(q.ExecutionState.Value));
      
      // Фильтр по признакам в карточке.
      if (filter.Escalated)
        query = query.Where(j => j.IsEscalated != null && j.IsEscalated.Value);
      if (filter.Category != null)
        query = query.Where(j => ActionItems.Categories.Equals(j.Category, filter.Category));
      if (filter.Priority != null)
        query = query.Where(j => ActionItems.Priorities.Equals(j.Priority, filter.Priority));
      
      // Наложить фильтр по роли в поручении.
      if (filter.IAmAuthor || filter.IAmCommis || filter.IAmPerformer || filter.IAmControler || filter.IAmSubscriber)
        query = query.Where(t => filter.IAmAuthor && DirRX.Solution.Employees.Equals(Users.Current, t.AssignedBy) ||
                            filter.IAmCommis && DirRX.Solution.Employees.Equals(Users.Current, t.Initiator) ||
                            filter.IAmPerformer && DirRX.Solution.Employees.Equals(Users.Current, t.Assignee) ||
                            filter.IAmControler && DirRX.Solution.Employees.Equals(Users.Current, t.Supervisor) ||
                            filter.IAmSubscriber && t.Subscribers.Any(s => DirRX.Solution.Employees.Equals(Users.Current, s.Subscriber)));
      
      // Фильтр по подразделению исполнителя.
      if (filter.PerformerDept != null)
      {
        List<Sungero.Company.IDepartment> depatmentsAll = new List<Sungero.Company.IDepartment>() { filter.PerformerDept };
        depatmentsAll = DirRX.ActionItems.PublicFunctions.Module.Remote.GetDepartmentHierarchyDown(new List<Sungero.Company.IDepartment>() { filter.PerformerDept }, depatmentsAll);
        
        query = query.Where(d => depatmentsAll.Contains(d.Assignee.Department));
      }
      
      // Скопировано из стандартной разработки, фильтр по соблюдению сроков.
      var now = Calendar.Now;
      var today = Calendar.UserToday;
      var endOfToday = today.EndOfDay().FromUserTime();
      if (filter.DisciplineOverdue)
        query = query.Where(j => j.Status != Sungero.Workflow.Task.Status.Aborted &&
                            ((j.ActualDate != null && !j.Deadline.Value.HasTime() && j.Deadline < j.ActualDate) ||
                             (j.ActualDate != null && j.Deadline.Value.HasTime() && j.Deadline < j.ActualDate.Value.EndOfDay()) ||
                             (j.ActualDate == null && !j.Deadline.Value.HasTime() && j.Deadline < today) ||
                             (j.ActualDate == null && j.Deadline.Value.HasTime() && j.Deadline < now)));

      // Скопировано из стандартной разработки, фильтр по плановому сроку.
      if (filter.LastMonthPlan)
      {
        var lastMonthBeginDate = Sungero.Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(today.AddDays(-30));
        query = query.Where(j => ((!j.Deadline.Value.HasTime() && today.AddDays(-30) <= j.Deadline ||
                                   j.Deadline.Value.HasTime() && lastMonthBeginDate <= j.Deadline) &&
                                  (!j.Deadline.Value.HasTime() && j.Deadline <= today ||
                                   j.Deadline.Value.HasTime() && j.Deadline < endOfToday)));
      }
      
      if (filter.Manual)
      {
        if (filter.PlanDateRangeFrom != null)
        {
          var dateFrom = Sungero.Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(filter.PlanDateRangeFrom.Value);
          query = query.Where(j => j.Deadline.Value.HasTime() && j.Deadline >= dateFrom || !j.Deadline.Value.HasTime() && j.Deadline >= filter.PlanDateRangeFrom.Value);
        }
        if (filter.PlanDateRangeTo != null)
        {
          var dateTo = filter.PlanDateRangeTo.Value.EndOfDay().FromUserTime();
          query = query.Where(j => j.Deadline.Value.HasTime() && j.Deadline <= dateTo ||
                              !j.Deadline.Value.HasTime() && j.Deadline <= filter.PlanDateRangeTo.Value);
        }
        
      }
      
      return query;
    }
  }


  partial class ActionItemExecutionTaskAssignedByPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> AssignedByFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      // TODO: Непонятно по каким признакам необходимо фильтровать, оставляем всех сотрудников.
      //query = base.AssignedByFiltering(query, e);
      
      return query;
    }
  }

  partial class ActionItemExecutionTaskServerHandlers
  {

    public override void BeforeAbort(Sungero.Workflow.Server.BeforeAbortEventArgs e)
    {
      // Рассылка уведомлений по событию "Постановщик прекратил исполнение поручения в системе".
      try
      {
        ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("AbortEvent", _obj);
      }
      catch (Exception ex)
      {
        Logger.Error("Error in AbortEvent", ex);
      }
      
      base.BeforeAbort(e);
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      if (_obj.IsCompoundActionItem.GetValueOrDefault())
      {
        if (_obj.FinalDeadline.HasValue && _obj.FinalDeadline.Value.Date > _obj.ReportDeadline)
          e.AddError(_obj.Info.Properties.FinalDeadline, ActionItemExecutionTasks.Resources.IncorrectValidFinalDeadlineDate, _obj.Info.Properties.ReportDeadline);
        
        if (_obj.ActionItemParts.Any(i => i.Deadline.HasValue && i.Deadline.Value.Date > _obj.ReportDeadline))
          e.AddError(_obj.Info.Properties.ActionItemParts, ActionItemExecutionTasks.Resources.IncorrectValidDeadlineDate, _obj.Info.Properties.ReportDeadline);
      }
      
      if (!_obj.IsCompoundActionItem.GetValueOrDefault())
      {
        if (_obj.Deadline.Value.Date > _obj.ReportDeadline)
          e.AddError(_obj.Info.Properties.Deadline, ActionItemExecutionTasks.Resources.IncorrectValidDeadlineDate, _obj.Info.Properties.ReportDeadline);
      }

    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      _obj.Initiator = Employees.As(Users.Current);
      _obj.EscalatedText = _obj.Info.Properties.EscalatedText.LocalizedName;
      _obj.IsEscalated = false;
      _obj.Deadline = null;
      _obj.IsRepeating = false;
      
      // Заполнение срока для подчинённых поручений.
      if (CallContext.CalledFrom(ActionItemExecutionAssignments.Info))
      {
        var actionItemAssignment = ActionItemExecutionAssignments.GetAll()
          .Where(a => a.Id == CallContext.GetCallerEntityId(ActionItemExecutionAssignments.Info))
          .FirstOrDefault();
        
        if (ActionItemExecutionAssignments.Equals(_obj.ParentAssignment, _obj.ParentAssignment))
        {
          var actionItemTask = ActionItemExecutionTasks.As(actionItemAssignment.Task);
          _obj.Deadline = actionItemTask.Deadline;
        }
      }
    }

    public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
    {
      #region Скопировано из стандартной разработки.
      
      if (!Sungero.Company.Employees.Is(_obj.Author))
        e.AddError(_obj.Info.Properties.Author, Sungero.Docflow.Resources.CantSendTaskByNonEmployee);
      
      // Проверить заполненость Общего срока (а также корректность), исполнителей, текста поручения у не составного поручения.
      var isCompoundActionItem = _obj.IsCompoundActionItem ?? false;
      if (_obj.IsEscalated == false && _obj.ActionItemType != ActionItemType.Additional)
      {
        if (!isCompoundActionItem)
        {
          // Проверить корректность срока.
          if (!Sungero.Docflow.PublicFunctions.Module.CheckDeadline(_obj.Deadline, Calendar.Now))
            e.AddError(_obj.Info.Properties.Deadline, Sungero.RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThenToday);
        }
        else
        {
          // Проверить корректность срока.
          if (_obj.ActionItemParts.Any(j => !Sungero.Docflow.PublicFunctions.Module.CheckDeadline(j.Deadline, Calendar.Now)))
            e.AddError(Sungero.RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThenToday);
          
          // Проверить корректность Общего срока.
          if (_obj.FinalDeadline != null && !Sungero.Docflow.PublicFunctions.Module.CheckDeadline(_obj.FinalDeadline, Calendar.Now))
            e.AddError(_obj.Info.Properties.FinalDeadline, Sungero.RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThenToday);
        }
      }
      
      // Проверить корректность заполнения свойства Выдал.
      //if (!(Employees.Current == null && Users.Current.IncludedIn(Roles.Administrators)) &&
      //    !Docflow.PublicFunctions.Module.Remote.IsUsersCanBeResolutionAuthor(_obj.DocumentsGroup.OfficialDocuments.SingleOrDefault(), _obj.AssignedBy))
      //  e.AddError(_obj.Info.Properties.AssignedBy, ActionItemExecutionTasks.Resources.ActionItemCanNotAssignedByUser);
      
      // Задать текст в переписке.
      if (_obj.IsCompoundActionItem == true)
        _obj.ActiveText = string.IsNullOrEmpty(_obj.ActionItem) ? Sungero.RecordManagement.ActionItemExecutionTasks.Resources.DefaultActionItem : _obj.ActionItem;

      if (_obj.ActionItemType == ActionItemType.Component)
      {
        _obj.ActiveText = _obj.ActionItem;
        // При рестарте поручения обновляется текст, срок и исполнитель в табличной части составного поручения.
        var actionItem = ActionItemExecutionTasks.As(_obj.ParentTask).ActionItemParts.FirstOrDefault(s => Equals(s.ActionItemPartExecutionTask, _obj));
        // Обновить текст поручения, если изменен индивидуальный текст или указан общий текст вместо индивидуального.
        if (actionItem.ActionItemExecutionTask.ActionItem != _obj.ActionItem && actionItem.ActionItemPart != _obj.ActionItem ||
            actionItem.ActionItemExecutionTask.ActionItem == _obj.ActionItem && !string.IsNullOrEmpty(actionItem.ActionItemPart))
          actionItem.ActionItemPart = _obj.ActionItem;
        // Обновить срок поручения, если изменен индивидуальный срок или указан общий срок вместо индивидуального.
        if (actionItem.ActionItemExecutionTask.FinalDeadline != _obj.Deadline && actionItem.Deadline != _obj.Deadline ||
            actionItem.ActionItemExecutionTask.FinalDeadline == _obj.Deadline && actionItem.Deadline.HasValue)
          actionItem.Deadline = _obj.Deadline;
        // Обновить исполнителя, если он изменен при рестарте.
        if (actionItem.ActionItemExecutionTask.Assignee != _obj.Assignee && actionItem.Assignee != _obj.Assignee)
          actionItem.Assignee = _obj.Assignee;
      }
      
      if (_obj.ActionItemType == ActionItemType.Additional)
        _obj.ActiveText = ActionItemExecutionTasks.Resources.SentToCoAssignee;
      
      // Выдать права на изменение для возможности прекращения задачи.
      ActionItems.PublicFunctions.Module.GrantAccessRightToTask(_obj, _obj);
      
      if (_obj.IsDraftResolution == true && !_obj.DocumentsGroup.OfficialDocuments.Any())
        if (Sungero.RecordManagement.ReviewDraftResolutionAssignments.Is(_obj.ParentAssignment))
          _obj.DocumentsGroup.OfficialDocuments.Add(Sungero.RecordManagement.ReviewDraftResolutionAssignments.As(_obj.ParentAssignment).DocumentForReviewGroup.OfficialDocuments.FirstOrDefault());
        else
          _obj.DocumentsGroup.OfficialDocuments.Add(Sungero.RecordManagement.PreparingDraftResolutionAssignments.As(_obj.ParentAssignment).DocumentForReviewGroup.OfficialDocuments.FirstOrDefault());
      
      #endregion
      
      foreach (var subscriber in _obj.Subscribers.Select(s => s.Subscriber))
        _obj.AccessRights.Grant(subscriber, DefaultAccessRightsTypes.Read);
      
      _obj.StartedBy = _obj.Initiator;
      
      if (!_obj.IsCompoundActionItem.GetValueOrDefault())
        Functions.Module.GrantAccesRightsForManagers(_obj, _obj.Assignee);
      
      _obj.InitialDeadline = _obj.Deadline;
    }
  }
}