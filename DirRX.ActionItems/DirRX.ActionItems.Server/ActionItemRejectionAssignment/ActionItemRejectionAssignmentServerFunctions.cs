using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.ActionItemRejectionAssignment;

namespace DirRX.ActionItems.Server
{
  partial class ActionItemRejectionAssignmentFunctions
  {
    [Public]
    public string GetChangedParamsInfo()
    {
      var task = ActionItemRejectionTasks.As(_obj.Task);
      
      var result = new System.Text.StringBuilder("Изменённые параметры:").AppendLine();
      
      if (task != null)
      {
        if (!Categories.Equals(_obj.Category, task.Category))
          result.AppendLine(ActionItemRejectionAssignments.Resources.NewValueFormat(_obj.Info.Properties.Category.LocalizedName, _obj.Category.DisplayValue));
        if (!DirRX.Solution.Employees.Equals(_obj.Author, task.Author))
          result.AppendLine(ActionItemRejectionAssignments.Resources.NewValueFormat(_obj.Info.Properties.Author.LocalizedName, _obj.Author.DisplayValue));
        if (!DirRX.Solution.Employees.Equals(_obj.Initiator, task.Initiator))
          result.AppendLine(ActionItemRejectionAssignments.Resources.NewValueFormat(_obj.Info.Properties.Initiator.LocalizedName, _obj.Initiator.DisplayValue));
        if (!DirRX.Solution.Employees.Equals(_obj.Assignee, task.Assignee))
          result.AppendLine(ActionItemRejectionAssignments.Resources.NewValueFormat(_obj.Info.Properties.Assignee.LocalizedName, _obj.Assignee.DisplayValue));
        if (_obj.Supervisor != null && !DirRX.Solution.Employees.Equals(_obj.Supervisor, task.Supervisor))
          result.AppendLine(ActionItemRejectionAssignments.Resources.NewValueFormat(_obj.Info.Properties.Supervisor.LocalizedName, _obj.Supervisor.DisplayValue));
        if (_obj.ActionItemDeadline != task.ActionItemDeadline)
          result.AppendLine(ActionItemRejectionAssignments.Resources.NewValueFormat(_obj.Info.Properties.ActionItemDeadline.LocalizedName, _obj.ActionItemDeadline.GetValueOrDefault().ToString("d")));
        if (_obj.ReportDeadline.HasValue && _obj.ReportDeadline != task.ReportDeadline)
          result.AppendLine(ActionItemRejectionAssignments.Resources.NewValueFormat(_obj.Info.Properties.ReportDeadline.LocalizedName, _obj.ReportDeadline.GetValueOrDefault().ToString("d")));
        if (_obj.ActionItem != task.ActionItem)
          result.AppendLine(ActionItemRejectionAssignments.Resources.NewValueFormat(_obj.Info.Properties.ActionItem.LocalizedName, string.Empty)).AppendLine(_obj.ActionItem);
      }
      
      return result.ToString();
    }
  }
}