using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PeriodicActionItemsTemplate.Priority;

namespace DirRX.PeriodicActionItemsTemplate
{
  partial class PriorityManagerPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> ManagerFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
    	var possibleRoles = ActionItems.PublicFunctions.ActionItemsRole.GetPossibleRoles(_obj.Info);
    	
    	return query.Where(r => possibleRoles.Contains(r.Type));
    }
  }


  partial class PriorityServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
    	//HACK: Для отображения меток используется локализация свойства, а не отдельный ресурс.
    	_obj.RejectionDeadlineText = _obj.Info.Properties.RejectionDeadlineText.LocalizedName;
    	_obj.CompletionDeadlineText = _obj.Info.Properties.CompletionDeadlineText.LocalizedName;
    	_obj.EscalationPeriodWorkDaysText = _obj.Info.Properties.EscalationPeriodWorkDaysText.LocalizedName;
    	
      if (!_obj.State.IsCopied)
      {
        _obj.NeedsControl = false;
        _obj.AllowedExtendDeadline = false;
      }
    }
  }

}