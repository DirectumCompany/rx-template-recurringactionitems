using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.ActionItemRejectionTask;

namespace DirRX.ActionItems
{
  partial class ActionItemRejectionTaskServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.ActionItemChanged = false;
    }
  }

}