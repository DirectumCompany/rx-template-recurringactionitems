using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ActionItemExecutionTask;

namespace DirRX.Solution
{
  partial class ActionItemExecutionTaskActionItemPartsClientHandlers
  {

    public override void ActionItemPartsDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      base.ActionItemPartsDeadlineValueInput(e);
      var validateError = Functions.ActionItemExecutionTask.ValidateChildTaskDeadline(DirRX.Solution.ActionItemExecutionTasks.As(_obj.ActionItemExecutionTask),
                                                                                      e.NewValue);
      if (!string.IsNullOrEmpty(validateError))
        e.AddWarning(validateError);
    }
  }

  partial class ActionItemExecutionTaskClientHandlers
  {
    public override void DeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      base.DeadlineValueInput(e);
      var validateError = Functions.ActionItemExecutionTask.ValidateChildTaskDeadline(_obj, e.NewValue);
      if (!string.IsNullOrEmpty(validateError))
        e.AddWarning(validateError);
      _obj.State.Controls.Control.Refresh();
    }

    public override void FinalDeadlineValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      base.FinalDeadlineValueInput(e);
      var validateError = Functions.ActionItemExecutionTask.ValidateChildTaskDeadline(_obj, e.NewValue);
      if (!string.IsNullOrEmpty(validateError))
        e.AddWarning(validateError);
      _obj.State.Controls.Control.Refresh();
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      Functions.ActionItemExecutionTask.SetStateProperties(_obj);
    }

  }
}