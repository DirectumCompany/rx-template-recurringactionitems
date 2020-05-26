using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ActionItemExecutionAssignment;

namespace DirRX.Solution
{
  partial class ActionItemExecutionAssignmentClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);
      
      var executionTask = ActionItemExecutionTasks.As(_obj.Task);
      if (executionTask == null || executionTask.Priority.Manager == null)
        return;
      
      var isEscalated = _obj.IsEscalated.GetValueOrDefault();
      
      // Действие "Изменить исполнителя" отображается только для руководителя по эскалации, если поручение эскалировано.
      if (!isEscalated)
      {
        e.HideAction(_obj.Info.Actions.ChangePerformer);
        _obj.State.Properties.EmployeePerformer.IsVisible = false;
        _obj.State.Properties.CoPerformers.IsVisible = false;
      }
      else
      {
        var currentEmployee = DirRX.Solution.Employees.As(Users.Current);
        var escalateManager = ActionItems.PublicFunctions.Module.Remote.GetEscalatedManager(Solution.ActionItemExecutionTasks.As(_obj.Task));
        var isEscalateManager = DirRX.Solution.Employees.Equals(escalateManager, currentEmployee);
        
        if (!isEscalateManager)
        {
          e.HideAction(_obj.Info.Actions.ChangePerformer);
          _obj.State.Properties.EmployeePerformer.IsVisible = false;
          _obj.State.Properties.CoPerformers.IsVisible = false;
        }
      }
    }
  }


}