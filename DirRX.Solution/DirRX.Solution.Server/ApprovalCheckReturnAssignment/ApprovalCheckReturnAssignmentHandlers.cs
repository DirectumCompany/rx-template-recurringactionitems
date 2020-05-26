using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalCheckReturnAssignment;

namespace DirRX.Solution
{
  partial class ApprovalCheckReturnAssignmentServerHandlers
  {

    public override void BeforeComplete(Sungero.Workflow.Server.BeforeCompleteEventArgs e)
    {
      base.BeforeComplete(e);
     
      // Проверка наличия версии, созданной после формирования задания на контроль возврата скан-копии, при выполнении с результатом "Подписано".
      if (_obj.Result.Equals(Result.Signed))
      {
        var currentStage = Functions.ApprovalTask.GetStage(ApprovalTasks.As(_obj.Task), Sungero.Docflow.ApprovalStage.StageType.CheckReturn);
        var document = _obj.DocumentGroup.OfficialDocuments.First();
        var kindOfDocumentNeedReturnOriginal = currentStage.KindOfDocumentNeedReturn == Solution.ApprovalStage.KindOfDocumentNeedReturn.Original;
        if (!kindOfDocumentNeedReturnOriginal)
          if (!document.Versions.Where(x => x.Created > _obj.Created).Any())
            e.AddError(DirRX.Solution.ApprovalCheckReturnAssignments.Resources.ScanCopyErrorMessage);
      }
    }

    public override void Saved(Sungero.Domain.SavedEventArgs e)
    {
      base.Saved(e);
      if (_obj.State.IsInserted)
      {
        // Создание нового задания может изменить срок задачи.
        _obj.Task.MaxDeadline = Functions.ApprovalTask.GetExpectedDate(ApprovalTasks.As(_obj.Task));
      }
    }
  }

}