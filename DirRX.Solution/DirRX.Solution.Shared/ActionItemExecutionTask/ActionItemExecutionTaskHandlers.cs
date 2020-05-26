using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ActionItemExecutionTask;

namespace DirRX.Solution
{

  partial class ActionItemExecutionTaskActionItemPartsSharedCollectionHandlers
  {

    public override void ActionItemPartsDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e)
    {
      base.ActionItemPartsDeleted(e);
      if (_obj.Category != null && _obj.Category.NeedsReportDeadline.Value)
        Functions.ActionItemExecutionTask.GetReportDeadline(_obj);
    }
  }

  partial class ActionItemExecutionTaskSharedHandlers
  {

    public override void DocumentsGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      base.DocumentsGroupAdded(e);
      
      var documentKind = DirRX.Solution.DocumentKinds.Null;
      var incomingLetter = DirRX.Solution.IncomingLetters.As(e.Attachment);
      if (incomingLetter != null)
        documentKind = DirRX.Solution.DocumentKinds.As(incomingLetter.DocumentKind);
      
      var order = DirRX.Solution.Orders.As(e.Attachment);
      if (order != null)
        documentKind = DirRX.Solution.DocumentKinds.As(order.StandardForm.DocumentKind);
      
      if (documentKind != null && documentKind.AssignmentsCategories.Count == 1)
        _obj.Category = documentKind.AssignmentsCategories.First().AssignmentCategory;
    }

    public override void ActionItemPartsChanged(Sungero.Domain.Shared.CollectionPropertyChangedEventArgs e)
    {
      base.ActionItemPartsChanged(e);
      if (_obj.Category != null && _obj.Category.NeedsReportDeadline.Value)
        Functions.ActionItemExecutionTask.GetReportDeadline(_obj);
    }

    public override void FinalDeadlineChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.FinalDeadlineChanged(e);
      if (_obj.Category != null && _obj.Category.NeedsReportDeadline.Value)
        Functions.ActionItemExecutionTask.GetReportDeadline(_obj);
    }

    public override void DeadlineChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.DeadlineChanged(e);
      if (_obj.Category != null && _obj.Category.NeedsReportDeadline.Value)
        Functions.ActionItemExecutionTask.GetReportDeadline(_obj);
    }

    public override void AssigneeChanged(Sungero.RecordManagement.Shared.ActionItemExecutionTaskAssigneeChangedEventArgs e)
    {
      base.AssigneeChanged(e);
      if (e.NewValue != null && e.NewValue != e.OldValue &&
          _obj.Initiator != null && _obj.IsUnderControl.GetValueOrDefault() == true)
      {
        if (_obj.Category != null)
        {
          if (_obj.IsCompoundActionItem == false)
            _obj.Supervisor = DirRX.ActionItems.PublicFunctions.Module.Remote.GetSupervisor(_obj.Initiator, _obj.Category,
                                                                                            Solution.Employees.As(e.NewValue));
          else
            _obj.Supervisor = DirRX.ActionItems.PublicFunctions.Module.Remote.GetSupervisor(_obj.Initiator, _obj.Category, null);
        }
        if (!_obj.State.Properties.Supervisor.IsChanged || _obj.Supervisor == null)
          _obj.Supervisor = _obj.Initiator;
      }
    }

    public override void ParentAssignmentChanged(Sungero.Workflow.Shared.TaskParentAssignmentChangedEventArgs e)
    {
      base.ParentAssignmentChanged(e);
      
      if (e.NewValue != null)
      {
        var parentAssignmentTask = DirRX.Solution.ActionItemExecutionTasks.As(e.NewValue.Task);
        if (parentAssignmentTask != null)
        {
          _obj.Category = parentAssignmentTask.Category;
          if (parentAssignmentTask.Supervisor != null)
            _obj.Initiator = DirRX.Solution.Employees.As(parentAssignmentTask.Supervisor);
        }
      }
    }

    public override void IsUnderControlChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      base.IsUnderControlChanged(e);
      if (e.NewValue != e.OldValue && e.NewValue.GetValueOrDefault() == true &&
          _obj.Initiator != null)
      {
        if (_obj.Category != null)
        {
          if (_obj.IsCompoundActionItem == false && _obj.Assignee != null)
            _obj.Supervisor = DirRX.ActionItems.PublicFunctions.Module.Remote.GetSupervisor(_obj.Initiator, _obj.Category,
                                                                                            Solution.Employees.As(_obj.Assignee));
          else
            _obj.Supervisor = DirRX.ActionItems.PublicFunctions.Module.Remote.GetSupervisor(_obj.Initiator, _obj.Category, null);
        }
        if (!_obj.State.Properties.Supervisor.IsChanged || _obj.Supervisor == null)
          _obj.Supervisor = _obj.Initiator;
      }
      
      if (e.NewValue.GetValueOrDefault() == false)
        _obj.Supervisor = null;
    }

    public virtual void InitiatorChanged(DirRX.Solution.Shared.ActionItemExecutionTaskInitiatorChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue)
      {
        // Заполнение контролёра в зависимости от настроек.
        if (_obj.IsUnderControl.GetValueOrDefault() == true)
        {
          if (_obj.Category != null)
          {
            if (_obj.Assignee != null && _obj.IsCompoundActionItem == false)
              _obj.Supervisor = DirRX.ActionItems.PublicFunctions.Module.Remote.GetSupervisor(e.NewValue, _obj.Category,
                                                                                              Solution.Employees.As(_obj.Assignee));
            else
              _obj.Supervisor = DirRX.ActionItems.PublicFunctions.Module.Remote.GetSupervisor(e.NewValue, _obj.Category, null);
          }
          
          if (!_obj.State.Properties.Supervisor.IsChanged || _obj.Supervisor == null)
            _obj.Supervisor = _obj.Initiator;
        }
        
        // Заполнение автора, если нужно явно указывать ГД.
        if (_obj.Category != null && _obj.Category.IsCEOActionItem.GetValueOrDefault())
          _obj.AssignedBy = DirRX.ActionItems.PublicFunctions.ActionItemsRole.Remote.GetCEO(e.NewValue);
      }
    }

    public virtual void PriorityChanged(DirRX.Solution.Shared.ActionItemExecutionTaskPriorityChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue)
      {
        if (_obj.IsUnderControl == false)
          _obj.IsUnderControl = e.NewValue.NeedsControl.GetValueOrDefault();
      }
    }

    public virtual void CategoryChanged(DirRX.Solution.Shared.ActionItemExecutionTaskCategoryChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        if (e.NewValue == null)
        {
          _obj.Priority = null;
          _obj.Supervisor = null;
        }
        else
        {
          _obj.Priority = e.NewValue.Priority;
          
          if (!e.NewValue.NeedsReportDeadline.GetValueOrDefault())
            _obj.ReportDeadline = null;
          
          if (_obj.Initiator != null)
          {
            if (_obj.IsUnderControl == true)
            {
              if (_obj.Priority != null)
              {
                if (_obj.Assignee != null && _obj.IsCompoundActionItem == false)
                  _obj.Supervisor = DirRX.ActionItems.PublicFunctions.Module.Remote.GetSupervisor(_obj.Initiator, e.NewValue,
                                                                                                  Solution.Employees.As(_obj.Assignee));
                else
                  _obj.Supervisor = DirRX.ActionItems.PublicFunctions.Module.Remote.GetSupervisor(_obj.Initiator, e.NewValue, null);
              }
              
              if (!_obj.State.Properties.Supervisor.IsChanged || _obj.Supervisor == null)
                _obj.Supervisor = _obj.Initiator;
            }
            
            // Заполнение автора, если нужно явно указывать ГД.
            if (_obj.Initiator != null && e.NewValue.IsCEOActionItem.GetValueOrDefault())
              _obj.AssignedBy = DirRX.ActionItems.PublicFunctions.ActionItemsRole.Remote.GetCEO(_obj.Initiator);
          }
        }
      }
      
      Functions.ActionItemExecutionTask.SetStateProperties(_obj);
    }
  }
}