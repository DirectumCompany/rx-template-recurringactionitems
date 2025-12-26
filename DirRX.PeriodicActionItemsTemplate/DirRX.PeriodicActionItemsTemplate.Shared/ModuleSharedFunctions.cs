using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PeriodicActionItemsTemplate.Shared
{
  public partial class ModuleFunctions
  {
    
    /// <summary>
    /// Создать график отправки периодических поручений по поручению-основанию.
    /// </summary>
    /// <param name="actionItem">Поручение-основание.</param>
    /// <returns>График отправки периодических поручений по поручению-основанию.</returns>
    [Public]
    public virtual IRepeatSetting CreateScheduleFromActionItem(Sungero.RecordManagement.IActionItemExecutionTask actionItem)
    {
      var schedule = Functions.RepeatSetting.Remote.CreateRepeatSetting();
      
      schedule.MainActionItem = actionItem;
      schedule.MainDocument = actionItem.DocumentsGroup.OfficialDocuments.FirstOrDefault();
      
      schedule.AssignedBy = actionItem.AssignedBy;
      schedule.IsUnderControl = actionItem.IsUnderControl;
      schedule.Supervisor = actionItem.Supervisor;
      schedule.HasIndefiniteDeadline = actionItem.HasIndefiniteDeadline == true;
      schedule.ActionItem = actionItem.ActiveText;
      schedule.IsCompoundActionItem = actionItem.IsCompoundActionItem == true;
      
      if (schedule.IsCompoundActionItem == true)
      {
        foreach (var part in actionItem.ActionItemParts)
        {
          var schedulePartRow = schedule.ActionItemsParts.AddNew();
          schedulePartRow.Assignee = part.Assignee;
          schedulePartRow.ActionItemPart = part.ActionItemPart;
          schedulePartRow.CoAssignees = part.CoAssignees;
          schedulePartRow.Supervisor = part.Supervisor;
          
          foreach (var coAssigneePart in actionItem.PartsCoAssignees.Where(pca => pca.PartGuid == part.PartGuid))
          {
            var scheduleCoAssigneePartRow = schedule.PartsCoAssignees.AddNew();
            scheduleCoAssigneePartRow.PartGuid = schedulePartRow.PartGuid;
            scheduleCoAssigneePartRow.CoAssignee = coAssigneePart.CoAssignee;
          }
        }
      }
      else
      {
        schedule.Assignee = actionItem.Assignee;
        foreach (var coAssigneeRow in actionItem.CoAssignees)
          schedule.CoAssignees.AddNew().Assignee = coAssigneeRow.Assignee;
      }
      
      return schedule;
    }
    
    /// <summary>
    /// Создать график отправки периодических поручений по документу-основанию.
    /// </summary>
    /// <param name="document">Документ-основание.</param>
    /// <returns>График отправки периодических поручений по документу-основанию.</returns>
    [Public]
    public virtual IRepeatSetting CreateScheduleFromDocument(Sungero.Docflow.IOfficialDocument document)
    {
      var schedule = Functions.RepeatSetting.Remote.CreateRepeatSetting();
      
      schedule.MainDocument = document;
      
      return schedule;
    }
  }
}