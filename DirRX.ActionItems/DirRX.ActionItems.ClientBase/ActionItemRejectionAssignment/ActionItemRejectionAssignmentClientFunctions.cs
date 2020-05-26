using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.ActionItemRejectionAssignment;

namespace DirRX.ActionItems.Client
{
  partial class ActionItemRejectionAssignmentFunctions
  {
    public bool ParamsChanged()
    {
      var task = ActionItemRejectionTasks.As(_obj.Task);
      
      if (task != null)
      {
        // Сначала проверяем все реквизиты и количество соисполнителей, затем состав соисполнителей.        
        if (!Categories.Equals(_obj.Category, task.Category) ||
            !DirRX.Solution.Employees.Equals(_obj.Author, task.Author) ||
            !DirRX.Solution.Employees.Equals(_obj.Initiator, task.Initiator) ||
            !DirRX.Solution.Employees.Equals(_obj.Assignee, task.Assignee) ||
            !DirRX.Solution.Employees.Equals(_obj.Supervisor, task.Supervisor) ||
            _obj.ActionItemDeadline != task.ActionItemDeadline ||
            _obj.ReportDeadline != task.ReportDeadline ||
            _obj.ActionItem != task.ActionItem ||
            _obj.CoAssignees.Count() != task.CoAssignees.Count())
        {
          return true;
        }
        
        foreach (var assignee in _obj.CoAssignees.Select(c => c.Assignee))
        {
          if (!task.CoAssignees.Any(c => DirRX.Solution.Employees.Equals(c.Assignee, assignee)))
            return true;
        }          
      }
      
      return false;
    }
  }
}