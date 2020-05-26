using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;
using DirRX.Solution.AcquaintanceTask;

namespace DirRX.Solution.Server
{
  partial class AcquaintanceTaskRouteHandlers
  {

    public override void StartBlock3(Sungero.RecordManagement.Server.AcquaintanceAssignmentArguments e)
    {
      if (_obj.Deadline.HasValue)
        e.Block.AbsoluteDeadline = _obj.Deadline.Value;
      
      e.Block.IsParallel = true;
      e.Block.Subject = AcquaintanceTasks.Resources.AcquaintanceAssignmentSubjectFormat(_obj.DocumentGroup.OfficialDocuments.First().DisplayValue);
      var recipients = Functions.AcquaintanceTask.GetActivePerformers(_obj);
      
      foreach (var recipient in recipients)
        e.Block.Performers.Add(recipient);
      
      // Синхронизировать приложения отправляемого документа.
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      Sungero.Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);      
    }

  }
}