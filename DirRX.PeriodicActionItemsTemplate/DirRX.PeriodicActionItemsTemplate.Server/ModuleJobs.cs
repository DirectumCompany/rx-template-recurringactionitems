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
        var startActionItem = true;
        var closedEmployees = GetAllClosedParticipantsFromSetting(scheduleItem.RepeatSetting);
        if (closedEmployees.Any())
        {
          var assignedBy = scheduleItem.RepeatSetting.AssignedBy;
          startActionItem = !closedEmployees.Any(x => !Equals(x, assignedBy) &&
                                                 (x.Department.Manager == null ||
                                                  x.Department.Manager.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed));
          
          var sendTaskTo = Recipients.As(assignedBy);
          if (closedEmployees.Any(x => Equals(x, assignedBy)))
          {
            if (startActionItem == true)
            {
              sendTaskTo = Functions.Module.GetResponsibleForPeriodic();
            }
            else
            {
              var regGroups = Sungero.Docflow.RegistrationGroups.GetAll()
                .Where(g => g.Status == Sungero.CoreEntities.DatabookEntry.Status.Active &&
                       g.CanRegisterIncoming == true &&
                       g.Departments.Any(d => Equals(d.Department, assignedBy.Department)))
                .FirstOrDefault();
              sendTaskTo = regGroups?.ResponsibleEmployee == null || regGroups?.ResponsibleEmployee.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed
                ? Functions.Module.GetResponsibleForPeriodic() : regGroups?.ResponsibleEmployee;
            }
          }
          
          
          var adminTask = Sungero.Workflow.SimpleTasks.Null;
          if (startActionItem == true)
            adminTask = Sungero.Workflow.SimpleTasks.CreateWithNotices(DirRX.PeriodicActionItemsTemplate.Resources.AdminSubjectForClosedParticipantsError,
                                                                       sendTaskTo);
          else
          {
            adminTask = Sungero.Workflow.SimpleTasks.Create(DirRX.PeriodicActionItemsTemplate.Resources.AdminSubjectForClosedParticipantsError,
                                                            sendTaskTo);
            adminTask.Deadline = Calendar.Today.AddDays(1);
            adminTask.ActiveText = Resources.ClosedEmployeesTaskActiveText;
          }
          
          if (!scheduleItem.RepeatSetting.AccessRights.IsGranted(DefaultAccessRightsTypes.Change, sendTaskTo))
          {
            scheduleItem.RepeatSetting.AccessRights.Grant(sendTaskTo, DefaultAccessRightsTypes.Change);
            scheduleItem.RepeatSetting.AccessRights.Save();
          }
          
          adminTask.Attachments.Add(scheduleItem.RepeatSetting);
          foreach (var employee in closedEmployees)
            adminTask.Attachments.Add(employee);
          
          adminTask.Start();
          
          if (startActionItem != true)
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