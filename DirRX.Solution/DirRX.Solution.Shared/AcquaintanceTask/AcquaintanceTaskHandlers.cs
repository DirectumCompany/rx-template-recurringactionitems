using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.AcquaintanceTask;

namespace DirRX.Solution
{
  partial class AcquaintanceTaskSharedHandlers
  {

    public override void DocumentGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      base.DocumentGroupAdded(e);
      
      if (Orders.Is(_obj.DocumentGroup.OfficialDocuments.FirstOrDefault()))
      {
        var order = Orders.As(_obj.DocumentGroup.OfficialDocuments.FirstOrDefault());
        var task = DirRX.Solution.PublicFunctions.AcquaintanceTask.GetApprovalTasks(order).ToList().LastOrDefault();
        List<IRecipient> acquainteds = new List<IRecipient>();

        // Добавить подразделения, указанные в "Распространяется на" регламентирующего документа.
        if (order.RegulatoryDocument != null)
          foreach (var recipient in order.RegulatoryDocument.SpreadsOn.Select(x => x.Recepient).ToList())
            acquainteds.Add(recipient);
        
        // Добавить подразделения, указанные в "Распространяется на" отменяемого регламентирующего документа.
        var revokeDocuments = order.Relations.GetRelatedFrom(LocalActs.PublicConstants.Module.RegulatoryNewEditionRelationName).Where(d => DirRX.Solution.Orders.Is(d));
        foreach (var revokeDocument in revokeDocuments)
        {
          var revokeOrder = DirRX.Solution.Orders.As(revokeDocument);
          if (revokeOrder != null && revokeOrder.RegulatoryDocument != null)
          {
            foreach (var recipient in revokeOrder.RegulatoryDocument.SpreadsOn.Select(x => x.Recepient).ToList())
              if (!acquainteds.Any(x => Users.Equals(x, recipient)))
                acquainteds.Add(recipient);
          }
        }
        
        // Добавить сотрудников, указанных в списке ознакомления типовой формы данного приказа.
        if (order.StandardForm.AcquaintanceList != null)
        {
          foreach (var recipient in order.StandardForm.AcquaintanceList.Participants.Select(x => x.Participant).ToList())
            if (!acquainteds.Any(x => Users.Equals(x, recipient)))
              acquainteds.Add(recipient);
        }
        
        // Получить список ролей, исполнители, которых исключаются из списка ознакомления.
        var excludeFromAcquaintanceTaskRole = DirRX.LocalActs.PublicFunctions.Module.Remote.GetExcludeFromAcquaintanceTaskRole();
        if (task != null)
        {
          // Добавить сотрудников, которые участвовали в согласовании.
          foreach (var recipient in PublicFunctions.AcquaintanceTask.GetAllApproversAndSignatories(ApprovalTasks.As(task)))
            if (!acquainteds.Any(x => Users.Equals(x, recipient)) && !recipient.IncludedIn(excludeFromAcquaintanceTaskRole))
              acquainteds.Add(recipient);
        }
        
        // Добавить сотрудников, которым выданы поручения в рамках согласования приказа.
        var actionItemTasks = DirRX.ActionItems.PublicFunctions.Module.Remote.GetActionItemTaskWithMainTask(task).ToList();
        foreach (var actionItemTask in actionItemTasks)
        {
          if (actionItemTask.IsCompoundActionItem.GetValueOrDefault())
          {
            // Вычислить исполнителей составного поручения.
            foreach (var part in actionItemTask.ActionItemParts)
              if (!acquainteds.Any(x => Users.Equals(x, part.Assignee)))
                acquainteds.Add(part.Assignee);
          }
          else
          {
            // Вычислить исполнителей и соисполнителей простого поручения.
            if (!acquainteds.Any(x => Users.Equals(x, actionItemTask.Assignee)))
              acquainteds.Add(actionItemTask.Assignee);
            foreach (var coAssignee in actionItemTask.CoAssignees)
              if (!acquainteds.Any(x => Users.Equals(x, coAssignee.Assignee)))
                acquainteds.Add(coAssignee.Assignee);
          }
        }
        
        foreach (var acquainted in acquainteds)
        {
          if (!_obj.Performers.Any(x => Users.Equals(x.Performer, acquainted)))
          {
            var performer = _obj.Performers.AddNew();
            performer.Performer = acquainted;
          }
        }
        
        _obj.IsElectronicAcquaintance = !order.StandardForm.NeedPersonalSignatureAcquaintance;
      }
    }
  }
}