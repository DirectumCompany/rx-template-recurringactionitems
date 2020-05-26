using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ActionItemSupervisorAssignment;

namespace DirRX.Solution.Server
{
  partial class ActionItemSupervisorAssignmentFunctions
  {

    /// <summary>
    /// Отображение результатов исполнения поручения.
    /// </summary>
    [Remote]
    public StateView GetActionItemResultState()
    {
      var stateView = Sungero.Core.StateView.Create();
      var stateViewBlock = stateView.AddBlock();
      stateViewBlock.ShowBorder = false;
      
      var actionItemAssignment = ActionItemExecutionAssignments.GetAll(a => DirRX.Solution.ActionItemExecutionTasks.Equals(a.Task, _obj.Task) && a.Completed <= _obj.Created)
        .OrderByDescending(a => a.Completed).FirstOrDefault();
      var actionItemResult = new System.Text.StringBuilder();
      
      // Вычисление исполнителя с учётом замещения.
      var performer = Users.Equals(actionItemAssignment.Performer, actionItemAssignment.CompletedBy) ?
        Sungero.Company.PublicFunctions.Employee.GetShortName(Sungero.Company.Employees.As(actionItemAssignment.Performer), false) :
        Solution.ActionItemSupervisorAssignments.Resources.CompletedForFormat(Sungero.Company.PublicFunctions.Employee.GetShortName(
          Sungero.Company.Employees.As(actionItemAssignment.CompletedBy), false),
                                                                              Sungero.Company.PublicFunctions.Employee.GetShortName(
                                                                                Sungero.Company.Employees.As(actionItemAssignment.Performer), DeclensionCase.Accusative, false));
      actionItemResult.AppendLine(Solution.ActionItemSupervisorAssignments.Resources.StageViewPerformerFormat(performer));
      
      actionItemResult.AppendLine(Solution.ActionItemSupervisorAssignments.Resources.StageViewCompletedFormat(actionItemAssignment.Completed.Value.ToString("G")));
      actionItemResult.AppendLine(Solution.ActionItemSupervisorAssignments.Resources.StageViewActiveText);
      stateViewBlock.AddLabel(actionItemResult.ToString());
      
      AddLargeText(stateViewBlock, actionItemAssignment.ActiveText);
      
      return stateView;
    }

    /// <summary>
    /// Отображение параметров поручения.
    /// </summary>
    [Remote]
    public StateView GetActionItemParamsState()
    {
      var stateView = Sungero.Core.StateView.Create();
      var actionItemTask = DirRX.Solution.ActionItemExecutionTasks.As(_obj.Task);
      var stateViewBlock = stateView.AddBlock();
      stateViewBlock.ShowBorder = false;
      
      var actionItemParams = new System.Text.StringBuilder();
      actionItemParams.AppendLine(Solution.ActionItemSupervisorAssignments.Resources.StateViewCategoryFormat(_obj.Category.Name));
      actionItemParams.AppendLine(Solution.ActionItemSupervisorAssignments.Resources.StateViewPriorityFormat(_obj.Priority.Name));
      actionItemParams.AppendLine(Solution.ActionItemSupervisorAssignments.Resources.StateViewAuthorFormat(Sungero.Company.PublicFunctions.Employee.GetShortName(
        Sungero.Company.Employees.As(actionItemTask.Author), false)));
      stateViewBlock.AddLabel(actionItemParams.ToString());
      
      // Добавление даты через разделитель, т.к. категория может отображаться в 2 строки и дата скроется.
      stateViewBlock.AddLineBreak();
      stateViewBlock.AddLabel(actionItemTask.Deadline.Value.HasTime() ?
                              Solution.ActionItemSupervisorAssignments.Resources.StageViewDeadlineFormat(actionItemTask.Deadline.Value.ToString("G")) :
                              Solution.ActionItemSupervisorAssignments.Resources.StageViewDeadlineFormat(actionItemTask.Deadline.Value.ToString("d")));
      
      stateViewBlock.AddLineBreak();
      stateViewBlock.AddLabel(Solution.ActionItemSupervisorAssignments.Resources.StageViewActionItem);

      AddLargeText(stateViewBlock, actionItemTask.ActionItem);
      
      return stateView;
    }
    
    /// <summary>
    /// Отображение текста поручения и отчёта исполнителя.
    /// </summary>
    /// <param name="stateViewBlock"></param>
    /// <param name="text"></param>
    public void AddLargeText(Sungero.Core.StateBlock stateViewBlock, string text)
    {      
      int lengthString = DirRX.Solution.Constants.RecordManagement.ActionItemSupervisorAssignment.MaxLengthString;
      var textCollection = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
      foreach (var textLine in textCollection)
      {
        if (textLine == null)
          continue;
        
        if (textLine.Length <= lengthString)
        {
          stateViewBlock.AddLineBreak();
          stateViewBlock.AddLabel(textLine.Trim());
        }
        else
        {
          var line = textLine;
          int indexSpace = 0;
          while (line.Length > lengthString)
          {
            indexSpace = line.LastIndexOf(' ', lengthString, lengthString);
            stateViewBlock.AddLineBreak();
            stateViewBlock.AddLabel(line.Substring(0, indexSpace).Trim());
            line = line.Substring(indexSpace);
          }
          stateViewBlock.AddLineBreak();
          stateViewBlock.AddLabel(line.Trim());
        }
      }
    }
  }
}