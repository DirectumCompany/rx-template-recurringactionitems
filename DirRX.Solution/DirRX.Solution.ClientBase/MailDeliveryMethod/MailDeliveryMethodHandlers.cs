using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.MailDeliveryMethod;

namespace DirRX.Solution
{
  partial class MailDeliveryMethodClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);
      _obj.State.Properties.Responsible.IsRequired = true;
    }

  }
}