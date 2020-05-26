using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.AcquaintanceAssignment;

namespace DirRX.Solution.Client
{
  partial class AcquaintanceAssignmentActions
  {
    public override void Acquainted(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var task = AcquaintanceTasks.As(_obj.Task);
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var acquationVersion = task.AcquaintanceVersions.FirstOrDefault(x => x.IsMainDocument.Value);
      var acquationVersionNumber = acquationVersion.Number.Value;
      
      // Требовать оставить комментарий при выполнении по замещению.
      if (!Equals(_obj.Performer, Users.Current))
      {
        if (_obj.Performer.Status == Sungero.CoreEntities.User.Status.Closed)
        {
          if (string.IsNullOrWhiteSpace(_obj.ActiveText))
            e.AddError(AcquaintanceTasks.Resources.CompletedBySubstitution);
        }
        else
          e.AddError(DirRX.Solution.AcquaintanceAssignments.Resources.AcquaintedValidError);
        return;
      }
      
      #region Скопировано из стандартной
      // Проверка отсутствия документа (если нет прав на документ).
      if (document == null)
        return;
      
      var isElectronicAcquaintance = task.IsElectronicAcquaintance.Value;
      if (isElectronicAcquaintance)
      {
        // Требовать прочтение отправленной версии документа.
        if (!Sungero.RecordManagement.PublicFunctions.AcquaintanceTask.Remote.IsDocumentVersionReaded(document, acquationVersionNumber))
        {
          var error = document.LastVersion.Number == acquationVersionNumber
            ? AcquaintanceTasks.Resources.DocumentNotReadedLastVersion
            : AcquaintanceTasks.Resources.DocumentNotReadedFormat(acquationVersionNumber);
          e.AddError(error);
          return;
        }
      }
      #endregion
    }

    public override bool CanAcquainted(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return base.CanAcquainted(e);
    }

  }

}