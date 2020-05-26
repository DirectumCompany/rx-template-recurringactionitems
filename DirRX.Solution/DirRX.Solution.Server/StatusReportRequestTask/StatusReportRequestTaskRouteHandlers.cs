using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;
using DirRX.Solution.StatusReportRequestTask;

namespace DirRX.Solution.Server
{
  partial class StatusReportRequestTaskRouteHandlers
  {

    public override void StartAssignment4(Sungero.RecordManagement.IReportRequestCheckAssignment assignment, Sungero.RecordManagement.Server.ReportRequestCheckAssignmentArguments e)
    {
      base.StartAssignment4(assignment, e);
      
    	var task = Solution.StatusReportRequestTasks.As(assignment.Task);
      if (task.IsManyAssignees == true)
      {
      	// Для составного поручения инициатор задания на приемку отчета - система.
      	assignment.Author = Users.Current;
      }
    }

    public override void CompleteAssignment3(Sungero.RecordManagement.IReportRequestAssignment assignment, Sungero.RecordManagement.Server.ReportRequestAssignmentArguments e)
    {
      base.CompleteAssignment3(assignment, e);
      
      var task = Solution.StatusReportRequestTasks.As(assignment.Task);
      
      // Для отчета по составному поручению очистить свойство отчет, чтобы при отправке на доработку у всех исполнителей не появился отчет последнего исполнителя с предыдущего шага.
      if (task.IsManyAssignees == true)
      	_obj.Report = null;           
    }

    public override void StartAssignment3(Sungero.RecordManagement.IReportRequestAssignment assignment, Sungero.RecordManagement.Server.ReportRequestAssignmentArguments e)
    {
      base.StartAssignment3(assignment, e);
      
      var task = Solution.StatusReportRequestTasks.As(assignment.Task);
      // Опустошить текущий текст - в отличие от индивидуального поручения, отчет каждый раз придется писать заново.
      if (task.IsManyAssignees == true)
      	assignment.ActiveText = null;
      
      if (string.IsNullOrEmpty(task.ReportNote))
      	assignment.Subject = Functions.StatusReportRequestTask.GetStatusReportAssignmentSubject(task, assignment.Performer, StatusReportRequestTasks.Resources.ProvideReportByJob);
      else
      	assignment.Subject = Functions.StatusReportRequestTask.GetStatusReportAssignmentSubject(task, assignment.Performer, StatusReportRequestTasks.Resources.FinalizeReportByJob);            
    }

    public override void StartBlock3(Sungero.RecordManagement.Server.ReportRequestAssignmentArguments e)
    {
      base.StartBlock3(e);
      
      if (_obj.IsManyAssignees == true)
      {
      	e.Block.IsParallel = true;
      	
      	// Добавляем исполнителей только, если из задание поп поручению не выполнено.
      	var performers = Solution.PublicFunctions.ActionItemExecutionTask.Remote.GetActionItemsPerformersDir(DirRX.Solution.ActionItemExecutionTasks.As(_obj.ParentTask));      	
      	foreach (var performer in performers)
      	  e.Block.Performers.Add(performer);
      }
      else
      	e.Block.Performers.Add(_obj.Assignee);          
      
      Sungero.Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, _obj.DocumentsGroup.OfficialDocuments.FirstOrDefault());      
    }

  }
}