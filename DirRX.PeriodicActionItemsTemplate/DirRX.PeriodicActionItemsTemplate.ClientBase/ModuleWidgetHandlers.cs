using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PeriodicActionItemsTemplate.Client
{
  partial class ActionItemExecutionStatusWidgetHandlers
  {

    public virtual void ExecuteActionItemExecutionStatusChartAction(Sungero.Domain.Client.ExecuteWidgetBarChartActionEventArgs e)
    {
      var isAuthor = _parameters.Roles == Widgets.ActionItemExecutionStatus.Roles.Author;
      var isInitiator = _parameters.Roles == Widgets.ActionItemExecutionStatus.Roles.Initiator;
      var isPerformer = _parameters.Roles == Widgets.ActionItemExecutionStatus.Roles.Performer;
      var isManager = _parameters.Roles == Widgets.ActionItemExecutionStatus.Roles.Manager;
      
      var users = new List<IUser>();
      users.Add(Users.Current);
      bool? isEscalated = null;
      int? priority = null;
      
      if (e.SeriesId == DirRX.ActionItems.Resources.EscalatedWidgetSeries)
        isEscalated = true;
      else
        priority = Convert.ToInt32(e.SeriesId);
      
      if (_parameters.WithSubstitution)
      {
        users.AddRange(ActionItems.Functions.Module.Remote.GetSubstitutionUsers(Users.Current));
      }
      
      if (isAuthor)
      {
        switch (e.ValueId)
        {
          case DirRX.ActionItems.Constants.Module.OverdueTaskWidgetValue:
            ActionItems.Functions.Module.Remote.GetOverdueTaskByAuthor(users, priority, isEscalated).Show();
            break;
          case DirRX.ActionItems.Constants.Module.TaskDeadlineLess1DayWidgetValue:
            ActionItems.Functions.Module.Remote.GetTaskDeadlineLess1DayByAuthor(users, priority, isEscalated).Show();
            break;
          case DirRX.ActionItems.Constants.Module.TaskDeadlineMore1DayWidgetValue:
            ActionItems.Functions.Module.Remote.GetTaskDeadlineMore1DayByAuthor(users, priority, isEscalated).Show();
            break;
          case DirRX.ActionItems.Constants.Module.ExecutedTaskWidgetValue:
            ActionItems.Functions.Module.Remote.GetExecutedTaskByAuthor(users, priority, isEscalated).Show();
            break;
          default:
            break;
        }
      }
      else if (isInitiator)
      {
        switch (e.ValueId)
        {
          case DirRX.ActionItems.Constants.Module.OverdueTaskWidgetValue:
            ActionItems.Functions.Module.Remote.GetOverdueTaskByInitiator(users, priority, isEscalated).Show();
            break;
          case DirRX.ActionItems.Constants.Module.TaskDeadlineLess1DayWidgetValue:
            ActionItems.Functions.Module.Remote.GetTaskDeadlineLess1DayByInitiator(users, priority, isEscalated).Show();
            break;
          case DirRX.ActionItems.Constants.Module.TaskDeadlineMore1DayWidgetValue:
            ActionItems.Functions.Module.Remote.GetTaskDeadlineMore1DayByInitiator(users, priority, isEscalated).Show();
            break;
          case DirRX.ActionItems.Constants.Module.ExecutedTaskWidgetValue:
            ActionItems.Functions.Module.Remote.GetExecutedTaskByInitiator(users, priority, isEscalated).Show();
            break;
          default:
            break;
        }
      }
      else if (isPerformer)
      {
        switch (e.ValueId)
        {
          case DirRX.ActionItems.Constants.Module.OverdueTaskWidgetValue:
            ActionItems.Functions.Module.Remote.GetOverdueTaskByPerformer(users, priority, isEscalated).Show();
            break;
          case DirRX.ActionItems.Constants.Module.TaskDeadlineLess1DayWidgetValue:
            ActionItems.Functions.Module.Remote.GetTaskDeadlineLess1DayByPerformer(users, priority, isEscalated).Show();
            break;
          case DirRX.ActionItems.Constants.Module.TaskDeadlineMore1DayWidgetValue:
            ActionItems.Functions.Module.Remote.GetTaskDeadlineMore1DayByPerformer(users, priority, isEscalated).Show();
            break;
          case DirRX.ActionItems.Constants.Module.ExecutedTaskWidgetValue:
            ActionItems.Functions.Module.Remote.GetExecutedTaskByPerformer(users, priority, isEscalated).Show();
            break;
          default:
            break;
        }
      }
      else if (isManager)
      {
        switch (e.ValueId)
        {
          case DirRX.ActionItems.Constants.Module.OverdueTaskWidgetValue:
            ActionItems.Functions.Module.Remote.GetOverdueTaskByManager(users, priority, isEscalated).Show();
            break;
          case DirRX.ActionItems.Constants.Module.TaskDeadlineLess1DayWidgetValue:
            ActionItems.Functions.Module.Remote.GetTaskDeadlineLess1DayByManager(users, priority, isEscalated).Show();
            break;
          case DirRX.ActionItems.Constants.Module.TaskDeadlineMore1DayWidgetValue:
            ActionItems.Functions.Module.Remote.GetTaskDeadlineMore1DayByManager(users, priority, isEscalated).Show();
            break;
          case DirRX.ActionItems.Constants.Module.ExecutedTaskWidgetValue:
            ActionItems.Functions.Module.Remote.GetExecutedTaskByManager(users, priority, isEscalated).Show();
            break;
          default:
            break;
        }
      }
    }
  }



}