using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.DeadlineRejectionAssignment;

namespace DirRX.Solution
{
  partial class DeadlineRejectionAssignmentServerHandlers
  {
    #region Из базовой. Убрана проверка заполненности комментария к повторному запросу.
    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      if (_obj.Task.ParentAssignment.Status != Sungero.Workflow.AssignmentBase.Status.InProcess)
      {
        // Добавить автотекст.
        e.Result = DeadlineRejectionAssignments.Resources.Complete;
        return;
      }
      
      if (_obj.Result.Value == Result.ForRework)
      {
        // Новый срок должен быть позже старого.
        if (!Sungero.Docflow.PublicFunctions.Module.CheckDeadline(_obj.NewDeadline, _obj.CurrentDeadline))
          e.AddError(_obj.Info.Properties.NewDeadline, DeadlineExtensionTasks.Resources.DesiredDeadlineIsNotCorrect);
        
        // Добавить автотекст.
        e.Result = DeadlineRejectionAssignments.Resources.RequestedRepeatedly;
      }
      else
        // Добавить автотекст.
        e.Result = DeadlineRejectionAssignments.Resources.RequestedAccepted;
    }
    #endregion
  }

}