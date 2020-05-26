using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ActionItems
{
  partial class CustomActionItemsExecutionReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      // Удалить временные данные из таблицы.
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(DirRX.ActionItems.Constants.CustomActionItemsExecutionReport.SourceTableName, CustomActionItemsExecutionReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      #region Параметры и дата выполнения отчета
      
      CustomActionItemsExecutionReport.ReportSessionId = Guid.NewGuid().ToString();
      CustomActionItemsExecutionReport.ReportDate = Calendar.Now;
      
      // Подзаголовок "Срок с ______ по ______".
      CustomActionItemsExecutionReport.Subheader = DirRX.ActionItems.Reports.Resources.CustomActionItemsExecutionReport
        .HeaderDeadlineFormat(CustomActionItemsExecutionReport.BeginDate.Value.ToShortDateString(),
                              CustomActionItemsExecutionReport.ClientEndDate.Value.ToShortDateString());
      // Распечатал.
      CustomActionItemsExecutionReport.Printed = string.Format("/ {0} / {1}", Users.Current.DisplayValue, Calendar.Now.ToUserTime().ToString("d"));
      
      // Описание примененных фильтров (в верхнем левом углу отчета).
      if (CustomActionItemsExecutionReport.Author != null)
        CustomActionItemsExecutionReport.ParamsDescriprion += Sungero.RecordManagement.Reports.Resources.ActionItemsExecutionReport
          .FilterAuthorFormat(CustomActionItemsExecutionReport.Author.Person.ShortName, System.Environment.NewLine);
      
      if (CustomActionItemsExecutionReport.BusinessUnit != null)
        CustomActionItemsExecutionReport.ParamsDescriprion += Sungero.RecordManagement.Reports.Resources.ActionItemsExecutionReport
          .FilterBusinessUnitFormat(CustomActionItemsExecutionReport.BusinessUnit.Name, System.Environment.NewLine);
      
      if (CustomActionItemsExecutionReport.Department != null)
        CustomActionItemsExecutionReport.ParamsDescriprion += Sungero.RecordManagement.Reports.Resources.ActionItemsExecutionReport
          .FilterDepartmentFormat(CustomActionItemsExecutionReport.Department.Name, System.Environment.NewLine);
      
      if (CustomActionItemsExecutionReport.Performer != null)
      {
        var performerName = Solution.Employees.Is(CustomActionItemsExecutionReport.Performer) ?
          Solution.Employees.As(CustomActionItemsExecutionReport.Performer).Person.ShortName :
          CustomActionItemsExecutionReport.Performer.Name;
        CustomActionItemsExecutionReport.ParamsDescriprion += DirRX.ActionItems.Reports.Resources.CustomActionItemsExecutionReport
          .FilterResponsibleFormat(performerName, System.Environment.NewLine);
      }
      // Постановщик.
      if (CustomActionItemsExecutionReport.Initiator != null)
      {
        var initiatorName = Solution.Employees.Is(CustomActionItemsExecutionReport.Initiator) ?
          Solution.Employees.As(CustomActionItemsExecutionReport.Initiator).Person.ShortName :
          CustomActionItemsExecutionReport.Initiator.Name;
        CustomActionItemsExecutionReport.ParamsDescriprion += DirRX.ActionItems.Reports.Resources.CustomActionItemsExecutionReport
          .FilterInitiatorFormat(initiatorName, System.Environment.NewLine);
      }
      
      // Приоритет.
      if (CustomActionItemsExecutionReport.Priority != null)
        CustomActionItemsExecutionReport.ParamsDescriprion += DirRX.ActionItems.Reports.Resources.CustomActionItemsExecutionReport
          .FilterPriorityFormat(CustomActionItemsExecutionReport.Priority.Name, System.Environment.NewLine);
      
      // Категория.
      if (CustomActionItemsExecutionReport.Category != null)
        CustomActionItemsExecutionReport.ParamsDescriprion += DirRX.ActionItems.Reports.Resources.CustomActionItemsExecutionReport
          .FilterCategoryFormat(CustomActionItemsExecutionReport.Priority.Name, System.Environment.NewLine);
      
      // Эскалированные поручения.
      if (CustomActionItemsExecutionReport.IsEscalated == true)
        CustomActionItemsExecutionReport.ParamsDescriprion += DirRX.ActionItems.Reports.Resources.CustomActionItemsExecutionReport
          .EscalatedItemsFormat(System.Environment.NewLine);
      
      #endregion
      
      // Получить данные по поручениям.
      var actionItems = Functions.Module.GetActionItemCompletionData(CustomActionItemsExecutionReport.BeginDate,
                                                                     CustomActionItemsExecutionReport.EndDate,
                                                                     CustomActionItemsExecutionReport.Author,
                                                                     CustomActionItemsExecutionReport.BusinessUnit,
                                                                     CustomActionItemsExecutionReport.Department,
                                                                     CustomActionItemsExecutionReport.Performer,
                                                                     CustomActionItemsExecutionReport.Initiator,
                                                                     CustomActionItemsExecutionReport.Priority,
                                                                     CustomActionItemsExecutionReport.Category,
                                                                     CustomActionItemsExecutionReport.IsEscalated,
                                                                     true);
      
      var dataTable = new List<Structures.CustomActionItemsExecutionReport.TableLine>();
      
      var сompletedOverdueCount = 0;
      var inProcessOverdueCount = 0;
      
      foreach (var actionItem in actionItems.OrderBy(a => a.Deadline))
      {        
        var tableLine = Structures.CustomActionItemsExecutionReport.TableLine.Create();
        
        // ИД и ссылка.
        tableLine.Id = actionItem.Id;
        tableLine.Hyperlink = Sungero.Core.Hyperlinks.Get(Solution.ActionItemExecutionTasks.Info, actionItem.Id);
        
        // Поручение.
        tableLine.ActionItemText = actionItem.ActionItem;
        
        // Автор.
        var author = Solution.Employees.As(actionItem.Author);
        if (author != null && author.Person != null)
          tableLine.Author = author.Person.ShortName;
        else
          tableLine.Author = actionItem.Author.Name;
        
        tableLine.Author = tableLine.Author.Replace("\u00A0", " ");
        
        // Статус.
        tableLine.State = string.Empty;
        if (actionItem.ExecutionState != null)
          tableLine.State = Solution.ActionItemExecutionTasks.Info.Properties.ExecutionState.GetLocalizedValue(actionItem.ExecutionState.Value);
        
        // Даты.
        tableLine.PlanDate = string.Empty;
        if (actionItem.Deadline.HasValue)
        {
          var deadline = Calendar.ToUserTime(actionItem.Deadline.Value);
          tableLine.PlanDate = Sungero.Docflow.PublicFunctions.Module.ToShortDateShortTime(deadline);
          
          // Дата для сортировки.
          tableLine.PlanDateSort = actionItem.Deadline.Value;
        }
        
        tableLine.ActualDate = string.Empty;
        var isCompleted = actionItem.Status == Sungero.Workflow.Task.Status.Completed;
        if (isCompleted)
        {
          var endDate = actionItem.ActualDate.HasValue ? actionItem.ActualDate.Value : Calendar.Now;
          tableLine.ActualDate = Sungero.Docflow.PublicFunctions.Module.ToShortDateShortTime(endDate.ToUserTime(actionItem.Assignee));
        }
        
        // Время в работе.
        tableLine.TimeInWork = Functions.Module.CalculateTimeInWork(actionItem.Id);
        
        // Подсчет  "Исполнено с нарушением срока".
        if ((actionItem.Status == Sungero.Workflow.Task.Status.Completed ||
             actionItem.Status == Sungero.Workflow.Task.Status.InProcess && actionItem.ExecutionState == Solution.ActionItemExecutionTask.ExecutionState.OnControl) &&
            WorkingTime.GetDurationInWorkingDays(actionItem.StartedDate.Value.Date, actionItem.Deadline.Value.Date, actionItem.Assignee) < tableLine.TimeInWork)
        {          
          сompletedOverdueCount++;
          tableLine.IsOverdue = true;
        }
        
        // Подсчет "В работе с нарушением срока".
        if (actionItem.Status == Sungero.Workflow.Task.Status.InProcess && actionItem.ExecutionState != Solution.ActionItemExecutionTask.ExecutionState.OnControl &&
            WorkingTime.GetDurationInWorkingDays(actionItem.StartedDate.Value.Date, actionItem.Deadline.Value.Date, actionItem.Assignee) < tableLine.TimeInWork)
        {          
          inProcessOverdueCount++;
          tableLine.IsOverdue = true;
        }
        
        // Исполнители.
        tableLine.Assignee = actionItem.Assignee.Person.ShortName;
        
        // Соисполнители.
        tableLine.CoAssignees = string.Join(", ", actionItem.CoAssigneesShortNames);
        
        // Постановщик.
        tableLine.Initiator = actionItem.Initiator.Person.ShortName;
        
        // Категория.
        tableLine.Category = actionItem.Category.Name;
        
        // Приоритет
        tableLine.Priority = actionItem.Priority.PriorityValue.ToString();
        
        tableLine.ReportSessionId = CustomActionItemsExecutionReport.ReportSessionId;
        dataTable.Add(tableLine);
      }
      
      #region Расчет итогов
      
      // Общее количество поручений.
      CustomActionItemsExecutionReport.TotalCount = actionItems.Count();
      
      // Исполнено всего.
      CustomActionItemsExecutionReport.Completed = actionItems
        .Where(j => j.Status == Sungero.Workflow.Task.Status.Completed ||
               (j.Status == Sungero.Workflow.Task.Status.InProcess && j.ExecutionState == Solution.ActionItemExecutionTask.ExecutionState.OnControl)).Count();
      
      // Исполнено с нарушением срока.
      CustomActionItemsExecutionReport.CompletedOverdue = сompletedOverdueCount;
      
      // На приемке.
      CustomActionItemsExecutionReport.OnControl = actionItems.Where(j => j.ExecutionState == Solution.ActionItemExecutionTask.ExecutionState.OnControl).Count();
      
      // В работе всего.
      CustomActionItemsExecutionReport.InProcess = actionItems
        .Where(j => j.Status == Sungero.Workflow.Task.Status.InProcess && j.ExecutionState != Solution.ActionItemExecutionTask.ExecutionState.OnControl).Count();
      
      // В работе с нарушением срока.
      CustomActionItemsExecutionReport.InProcessOverdue = inProcessOverdueCount;
      
      // Подсчет соотношения в процентах.
      if (CustomActionItemsExecutionReport.TotalCount != 0)
      {
        var inTimeActionItems = CustomActionItemsExecutionReport.TotalCount - CustomActionItemsExecutionReport.CompletedOverdue - CustomActionItemsExecutionReport.InProcessOverdue;
        CustomActionItemsExecutionReport.ExecutiveDisciplineLevel =
          string.Format("{0:P2}", inTimeActionItems / (double)CustomActionItemsExecutionReport.TotalCount);
      }
      else
        CustomActionItemsExecutionReport.ExecutiveDisciplineLevel = Sungero.RecordManagement.Reports.Resources.ActionItemsExecutionReport.NoAnyActionItems;
      
      #endregion
      
      Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.CustomActionItemsExecutionReport.SourceTableName, dataTable);
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        // Заполнить таблицу именами документов.
        command.CommandText = string.Format(Queries.CustomActionItemsExecutionReport.PasteDocumentNames, Constants.CustomActionItemsExecutionReport.SourceTableName, CustomActionItemsExecutionReport.ReportSessionId);
        command.ExecuteNonQuery();
      }
    }

  }
}