using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.RequestInitiatorTask;

namespace DirRX.LocalActs
{
  partial class RequestInitiatorTaskServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.NeedsReview = false;
    }
  }

}