using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.DeadlineExtensionAssignment;

namespace DirRX.Solution
{
  partial class DeadlineExtensionAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      #region Из базовой. Убрана проверка заполненности причины отказа.
      if (_obj.Task.ParentAssignment.Status != Sungero.Workflow.AssignmentBase.Status.InProcess)
      {
        // Добавить автотекст.
        e.Result = DeadlineExtensionAssignments.Resources.Complete;
        return;
      }
      
      if (_obj.Result.Value == Result.ForRework)
      {
        // Добавить автотекст.
        e.Result = DeadlineExtensionAssignments.Resources.Denied;
      }
      else
      {
        // Новый срок должен быть больше старого.
        if (!Sungero.Docflow.PublicFunctions.Module.CheckDeadline(_obj.NewDeadline, _obj.ScheduledDate))
          e.AddError(_obj.Info.Properties.NewDeadline, DeadlineExtensionTasks.Resources.DesiredDeadlineIsNotCorrect);
        
        // Добавить автотекст.
        var desiredDeadlineLabel = Functions.DeadlineExtensionAssignment.GetDesiredDeadlineLabel(_obj.NewDeadline.Value);
        e.Result = DeadlineExtensionAssignments.Resources.DeadlineExtendedFormat(desiredDeadlineLabel);
      }
      #endregion
    }
  }

}