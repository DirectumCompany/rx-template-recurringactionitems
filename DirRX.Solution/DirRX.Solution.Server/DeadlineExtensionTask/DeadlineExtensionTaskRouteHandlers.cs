using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;
using DirRX.Solution.DeadlineExtensionTask;

namespace DirRX.Solution.Server
{
  partial class DeadlineExtensionTaskRouteHandlers
  {

    public override void StartBlock4(Sungero.Docflow.Server.DeadlineRejectionAssignmentArguments e)
    {
      base.StartBlock4(e);
      
      // Рассылка уведомлений по событию "Принято решение по запросу продления срока исполнения поручения".
      var assignmentTask = Solution.ActionItemExecutionTasks.As(_obj.MainTask);
      if (assignmentTask != null)
      {
        try
        {
          ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("TimeDeclineEvent", assignmentTask);
        }
        catch (Exception ex)
        {
          Logger.Error("Error in TimeDeclineEvent", ex);
        }
      }
    }

    public override void StartBlock6(Sungero.Docflow.Server.DeadlineExtensionNotificationArguments e)
    {
      base.StartBlock6(e);
      
      // Рассылка уведомлений по событию "Принято решение по запросу продления срока исполнения поручения".
      var assignmentTask = Solution.ActionItemExecutionTasks.As(_obj.MainTask);
      if (assignmentTask != null)
      {
        try
        {
          ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("TimeAcceptEvent", assignmentTask);
        }
        catch (Exception ex)
        {
          Logger.Error("Error in TimeAcceptEvent", ex);
        }
      }
    }

    public override void StartAssignment3(Sungero.Docflow.IDeadlineExtensionAssignment assignment, Sungero.Docflow.Server.DeadlineExtensionAssignmentArguments e)
    {
      base.StartAssignment3(assignment, e);
      
      // Рассылка уведомлений по событию "Исполнитель запросил продление срока исполнения поручения".
      var assignmentTask = Solution.ActionItemExecutionTasks.As(_obj.MainTask);
      if (assignmentTask != null)
      {
        try
        {
          ActionItems.PublicFunctions.NoticeSetting.CollectAndSendNoticesByEvent("AddTimeEvent", assignmentTask);
        }
        catch (Exception ex)
        {
          Logger.Error("Error in AddTimeEvent", ex);
        }
      }
    }

    public override void StartAssignment4(Sungero.Docflow.IDeadlineRejectionAssignment assignment, Sungero.Docflow.Server.DeadlineRejectionAssignmentArguments e)
    {
      base.StartAssignment4(assignment, e);
      DirRX.Solution.DeadlineRejectionAssignments.As(assignment).Priority = Solution.ActionItemExecutionTasks.As(_obj.MainTask).Priority;
      DirRX.Solution.DeadlineRejectionAssignments.As(assignment).Deadline = Solution.ActionItemExecutionTasks.As(_obj.MainTask).Deadline;
    }

  }
}