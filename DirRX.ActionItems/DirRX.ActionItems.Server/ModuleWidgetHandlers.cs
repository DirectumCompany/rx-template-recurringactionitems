using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ActionItems.Server
{
  partial class ActionItemExecutionStatusWidgetHandlers
  {

    public virtual void GetActionItemExecutionStatusChartValue(Sungero.Domain.GetWidgetBarChartValueEventArgs e)
    {
      var isAuthor = _parameters.Roles == Widgets.ActionItemExecutionStatus.Roles.Author;
      var isInitiator = _parameters.Roles == Widgets.ActionItemExecutionStatus.Roles.Initiator;
      var isPerformer = _parameters.Roles == Widgets.ActionItemExecutionStatus.Roles.Performer;
      var isManager = _parameters.Roles == Widgets.ActionItemExecutionStatus.Roles.Manager;
      
      int overdueTask = 0;
      int taskDeadlineLess1Day = 0;
      int taskDeadlineMore1Day = 0;
      int executedTask = 0;
      var users = new List<IUser>();
      users.Add(Users.Current);
      
      if (_parameters.WithSubstitution)
      {
        users.AddRange(Functions.Module.GetSubstitutionUsers(Users.Current));
      }

      
      foreach (var priority in DirRX.ActionItems.Priorities.GetAll())
      {
        if (isAuthor)
        {
          overdueTask = ActionItems.Functions.Module.GetOverdueTaskByAuthor(users, priority.PriorityValue.Value, null).Count();
          taskDeadlineLess1Day = ActionItems.Functions.Module.GetTaskDeadlineLess1DayByAuthor(users, priority.PriorityValue.Value, null).Count();
          taskDeadlineMore1Day = ActionItems.Functions.Module.GetTaskDeadlineMore1DayByAuthor(users, priority.PriorityValue.Value, null).Count();
          executedTask = ActionItems.Functions.Module.GetExecutedTaskByAuthor(users, priority.PriorityValue.Value, null).Count();
        }
        else if (isInitiator)
        {
          overdueTask = ActionItems.Functions.Module.GetOverdueTaskByInitiator(users, priority.PriorityValue.Value, null).Count();
          taskDeadlineLess1Day = ActionItems.Functions.Module.GetTaskDeadlineLess1DayByInitiator(users, priority.PriorityValue.Value, null).Count();
          taskDeadlineMore1Day = ActionItems.Functions.Module.GetTaskDeadlineMore1DayByInitiator(users, priority.PriorityValue.Value, null).Count();
          executedTask = ActionItems.Functions.Module.GetExecutedTaskByInitiator(users, priority.PriorityValue.Value, null).Count();
        }
        else if (isPerformer)
        {
          overdueTask = ActionItems.Functions.Module.GetOverdueTaskByPerformer(users, priority.PriorityValue.Value, null).Count();
          taskDeadlineLess1Day = ActionItems.Functions.Module.GetTaskDeadlineLess1DayByPerformer(users, priority.PriorityValue.Value, null).Count();
          taskDeadlineMore1Day = ActionItems.Functions.Module.GetTaskDeadlineMore1DayByPerformer(users, priority.PriorityValue.Value, null).Count();
          executedTask = ActionItems.Functions.Module.GetExecutedTaskByPerformer(users, priority.PriorityValue.Value, null).Count();
        }
        else if (isManager)
        {
          overdueTask = ActionItems.Functions.Module.GetOverdueTaskByManager(users, priority.PriorityValue.Value, null).Count();
          taskDeadlineLess1Day = ActionItems.Functions.Module.GetTaskDeadlineLess1DayByManager(users, priority.PriorityValue.Value, null).Count();
          taskDeadlineMore1Day = ActionItems.Functions.Module.GetTaskDeadlineMore1DayByManager(users, priority.PriorityValue.Value, null).Count();
          executedTask = ActionItems.Functions.Module.GetExecutedTaskByManager(users, priority.PriorityValue.Value, null).Count();
        }
        
        var series = e.Chart.AddNewSeries(priority.PriorityValue.ToString(), string.Format("{0} ({1})", priority.Name, priority.PriorityValue.Value.ToString()));
        series.AddValue(DirRX.ActionItems.Constants.Module.OverdueTaskWidgetValue, "Выполнение просрочено", (double)overdueTask, Colors.Charts.Red);
        series.AddValue(DirRX.ActionItems.Constants.Module.TaskDeadlineLess1DayWidgetValue, "<=1 день на выполнение", (double)taskDeadlineLess1Day, Colors.Common.DarkOrange);
        series.AddValue(DirRX.ActionItems.Constants.Module.TaskDeadlineMore1DayWidgetValue, ">1 дня на выполнение", (double)taskDeadlineMore1Day, Colors.Common.Orange);
        series.AddValue(DirRX.ActionItems.Constants.Module.ExecutedTaskWidgetValue, "Выполнены. На контроле", (double)executedTask, Colors.Charts.Green);
      }
      
      if (isAuthor)
      {
        overdueTask = ActionItems.Functions.Module.GetOverdueTaskByAuthor(users, null, true).Count();
        taskDeadlineLess1Day = ActionItems.Functions.Module.GetTaskDeadlineLess1DayByAuthor(users, null, true).Count();
        taskDeadlineMore1Day = ActionItems.Functions.Module.GetTaskDeadlineMore1DayByAuthor(users, null, true).Count();
        executedTask = ActionItems.Functions.Module.GetExecutedTaskByAuthor(users, null, true).Count();
      }
      else if (isInitiator)
      {
        overdueTask = ActionItems.Functions.Module.GetOverdueTaskByInitiator(users, null, true).Count();
        taskDeadlineLess1Day = ActionItems.Functions.Module.GetTaskDeadlineLess1DayByInitiator(users, null, true).Count();
        taskDeadlineMore1Day = ActionItems.Functions.Module.GetTaskDeadlineMore1DayByInitiator(users, null, true).Count();
        executedTask = ActionItems.Functions.Module.GetExecutedTaskByInitiator(users, null, true).Count();
      }
      else if (isPerformer)
      {
        overdueTask = ActionItems.Functions.Module.GetOverdueTaskByPerformer(users, null, true).Count();
        taskDeadlineLess1Day = ActionItems.Functions.Module.GetTaskDeadlineLess1DayByPerformer(users, null, true).Count();
        taskDeadlineMore1Day = ActionItems.Functions.Module.GetTaskDeadlineMore1DayByPerformer(users, null, true).Count();
        executedTask = ActionItems.Functions.Module.GetExecutedTaskByPerformer(users, null, true).Count();
      }
      else if (isManager)
      {
        overdueTask = ActionItems.Functions.Module.GetOverdueTaskByManager(users, null, true).Count();
        taskDeadlineLess1Day = ActionItems.Functions.Module.GetTaskDeadlineLess1DayByManager(users, null, true).Count();
        taskDeadlineMore1Day = ActionItems.Functions.Module.GetTaskDeadlineMore1DayByManager(users, null, true).Count();
        executedTask = ActionItems.Functions.Module.GetExecutedTaskByManager(users, null, true).Count();
      }
      
      var escalatedSeries = e.Chart.AddNewSeries(DirRX.ActionItems.Resources.EscalatedWidgetSeries, "Эскалировано");
      escalatedSeries.AddValue(DirRX.ActionItems.Constants.Module.OverdueTaskWidgetValue, "Выполнение просрочено", (double)overdueTask, Colors.Charts.Red);
      escalatedSeries.AddValue(DirRX.ActionItems.Constants.Module.TaskDeadlineLess1DayWidgetValue, "<=1 день на выполнение", (double)taskDeadlineLess1Day, Colors.Common.DarkOrange);
      escalatedSeries.AddValue(DirRX.ActionItems.Constants.Module.TaskDeadlineMore1DayWidgetValue, ">1 дня на выполнение", (double)taskDeadlineMore1Day, Colors.Common.Orange);
      escalatedSeries.AddValue(DirRX.ActionItems.Constants.Module.ExecutedTaskWidgetValue, "Выполнены. На контроле", (double)executedTask, Colors.Charts.Green);
    }
  }
}