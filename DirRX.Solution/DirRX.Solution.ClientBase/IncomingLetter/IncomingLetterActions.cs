using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.IncomingLetter;

namespace DirRX.Solution.Client
{
  partial class IncomingLetterActions
  {
    public override void SendActionItem(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.SendActionItem(e);
    }

    public override bool CanSendActionItem(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSendActionItem(e);
    }

  }

}