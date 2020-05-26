using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.BusinessProcessGroup;

namespace DirRX.LocalActs
{
  partial class BusinessProcessGroupServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (Functions.BusinessProcessGroup.HaveDuplicates(_obj))
        e.AddError(Sungero.Commons.Resources.DuplicateDetected, _obj.Info.Actions.ShowDuplicates);
    }
  }

  partial class BusinessProcessGroupOwnersOwnerPropertyFilteringServerHandler<T>
  {
    public virtual IQueryable<T> OwnersOwnerFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(x => x.IsSingleUser == true);
    }
  }
}