using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.CheckingResult;

namespace DirRX.PartiesControl.Client
{
  partial class CheckingResultActions
  {
    public virtual void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var duplicates = Functions.CheckingResult.Remote.GetDoubleResults(_obj);
      if (duplicates.Any())
        duplicates.Show();
      else
        Dialogs.NotifyMessage(Sungero.Commons.Resources.DuplicateNotFound);
    }

    public virtual bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

}