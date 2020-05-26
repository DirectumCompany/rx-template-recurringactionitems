using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;
using DirRX.ActionItems.ActionItemEscalatedTask;

namespace DirRX.ActionItems.Server
{
  partial class ActionItemEscalatedTaskRouteHandlers
  {

    public virtual void StartNotice3(DirRX.ActionItems.IEscalatedNotice notice, DirRX.ActionItems.Server.EscalatedNoticeArguments e)
    {
      // Рассылка уведомлений по событию "Поручение эскалировано".
      var assignment = _obj.AllAttachments.Select(t => DirRX.Solution.ActionItemExecutionAssignments.As(t)).FirstOrDefault();
      if (assignment != null && assignment.Task != null)
      {
        try
        {
          ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("EscalateEvent", DirRX.Solution.ActionItemExecutionTasks.As(assignment.Task));
        }
        catch (Exception ex)
        {
          Logger.Error("Error in EscalateEvent", ex);
        }
      }
    }

    public virtual void StartBlock3(DirRX.ActionItems.Server.EscalatedNoticeArguments e)
    {
      e.Block.Performers.Add(_obj.Performer);
    }

  }
}