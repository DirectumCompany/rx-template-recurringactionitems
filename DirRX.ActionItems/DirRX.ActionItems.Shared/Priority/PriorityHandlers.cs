using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.Priority;

namespace DirRX.ActionItems
{
  partial class PrioritySharedHandlers
  {

    public virtual void PriorityValueChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
    	if (e.NewValue.HasValue && e.NewValue != e.OldValue)
    		_obj.DisplayName = e.NewValue.Value.ToString();
    }

  }
}