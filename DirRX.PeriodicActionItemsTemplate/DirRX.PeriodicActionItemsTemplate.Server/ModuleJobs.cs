using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Metadata.Services;
using Sungero.Domain.Shared;

namespace DirRX.PeriodicActionItemsTemplate.Server
{
  public class ModuleJobs
  {

    /// <summary>
    /// Актуализация расписания отправки периодических поручений.
    /// </summary>
    public virtual void UpdatingScheduleForPeriodicActionItems()
    {
      Functions.Module.CheckAndUpdateDatesInActiveScheduleItems();
    }
    
    /// <summary>
    /// Отправление повторяющихся поручений.
    /// </summary>
    public virtual void RepeatActionItemExecutionTasks()
    {
      var logger = Logger.WithLogger("RepeatActionItemExecutionTasks");
      logger.Debug("Start");
      
      var scheduleItemsToProcess = ScheduleItems.GetAll(si => si.StartDate <= Calendar.Today &&
                                                        si.ActionItemExecutionTask == null &&
                                                        si.Status == DirRX.PeriodicActionItemsTemplate.ScheduleItem.Status.Active);
      foreach (var scheduleItem in scheduleItemsToProcess)
      {
        var closedEmployees = GetAllClosedParticipantsFromSetting(scheduleItem.RepeatSetting);
        
        if (closedEmployees.Any())
        {
          // Отправляем уведомление администратору.
          var adminTask = Sungero.Workflow.SimpleTasks.CreateWithNotices(DirRX.PeriodicActionItemsTemplate.Resources.AdminSubjectForClosedParticipantsError,
                                                                         Functions.Module.GetResponsibleForPeriodic());
          adminTask.Attachments.Add(scheduleItem);
          foreach (var employee in closedEmployees)
            adminTask.Attachments.Add(employee);
          
          adminTask.Start();
          continue;
        }
        
        var asyncHandler = AsyncHandlers.StartPeriodicActionItem.Create();
        asyncHandler.ScheduleItemId = scheduleItem.Id;
        asyncHandler.ExecuteAsync();
      }
      
      logger.Debug("Finish");
    }
    
    private List<Sungero.Company.IEmployee> GetAllClosedParticipantsFromSetting(IRepeatSetting setting)
    {
      var closedEmployees = new List<Sungero.Company.IEmployee>();
      AddEmployeeToListIfClosed(closedEmployees, setting.Assignee);
      AddEmployeeToListIfClosed(closedEmployees, setting.Supervisor);
      AddEmployeeToListIfClosed(closedEmployees, setting.AssignedBy);
      
      foreach (var employee in setting.CoAssignees.Select(x => x.Assignee))
        AddEmployeeToListIfClosed(closedEmployees, employee);
      
      foreach (var employee in setting.ActionItemsParts.Select(x => x.Assignee))
        AddEmployeeToListIfClosed(closedEmployees, employee);
      
      foreach (var employee in setting.PartsCoAssignees.Select(x => x.CoAssignee))
        AddEmployeeToListIfClosed(closedEmployees, employee);
      
      return closedEmployees;
    }
    
    private void AddEmployeeToListIfClosed(List<Sungero.Company.IEmployee> closedEmployees, Sungero.Company.IEmployee employee)
    {
      if (employee != null && employee.Status == Sungero.Company.Employee.Status.Closed)
        closedEmployees.Add(employee);
    }
  }
}