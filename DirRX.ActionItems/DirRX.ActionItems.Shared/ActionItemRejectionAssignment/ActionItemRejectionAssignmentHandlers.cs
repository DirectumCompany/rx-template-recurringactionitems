using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.ActionItemRejectionAssignment;

namespace DirRX.ActionItems
{
  partial class ActionItemRejectionAssignmentSharedHandlers
  {

    public virtual void AssigneeChanged(DirRX.ActionItems.Shared.ActionItemRejectionAssignmentAssigneeChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue &&
          _obj.Initiator != null && _obj.IsUnderControl.GetValueOrDefault() == true)
      {
        if (_obj.Category != null)
        {
          _obj.Supervisor = DirRX.ActionItems.PublicFunctions.Module.Remote.GetSupervisor(_obj.Initiator, _obj.Category,
                                                                                          Solution.Employees.As(e.NewValue));
        }
        if (!_obj.State.Properties.Supervisor.IsChanged || _obj.Supervisor == null)
          _obj.Supervisor = _obj.Initiator;
      }
    }

    public virtual void IsUnderControlChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue && e.NewValue.GetValueOrDefault() == true &&
          _obj.Initiator != null)
      {
        if (_obj.Category != null)
        {
          if (_obj.Assignee != null)
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

    public virtual void InitiatorChanged(DirRX.ActionItems.Shared.ActionItemRejectionAssignmentInitiatorChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue && _obj.IsUnderControl.GetValueOrDefault() == true)
      {
        if (_obj.Category != null)
        {
          if (_obj.Assignee != null)
            _obj.Supervisor = DirRX.ActionItems.PublicFunctions.Module.Remote.GetSupervisor(e.NewValue, _obj.Category,
                                                                                            Solution.Employees.As(_obj.Assignee));
          else
            _obj.Supervisor = DirRX.ActionItems.PublicFunctions.Module.Remote.GetSupervisor(e.NewValue, _obj.Category, null);
        }
        
        if (!_obj.State.Properties.Supervisor.IsChanged || _obj.Supervisor == null)
          _obj.Supervisor = _obj.Initiator;
      }
    }

    public virtual void PriorityChanged(DirRX.ActionItems.Shared.ActionItemRejectionAssignmentPriorityChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue)
      {
        if (_obj.IsUnderControl == false)
          _obj.IsUnderControl = e.NewValue.NeedsControl.GetValueOrDefault();
      }
    }

    public virtual void CategoryChanged(DirRX.ActionItems.Shared.ActionItemRejectionAssignmentCategoryChangedEventArgs e)
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
                if (_obj.Assignee != null)
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
      
      Functions.ActionItemRejectionAssignment.SetStateProperties(_obj);
    }

  }
}