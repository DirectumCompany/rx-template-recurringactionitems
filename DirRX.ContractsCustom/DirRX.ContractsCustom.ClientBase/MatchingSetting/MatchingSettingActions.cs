using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.MatchingSetting;

namespace DirRX.ContractsCustom.Client
{
  partial class MatchingSettingActions
  {
    public virtual void ShowDuplicate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
    	var duplicates = Functions.MatchingSetting.Remote.GetDoubles(_obj);
      
      if (duplicates.Any())
        duplicates.Show();
      else
      	Dialogs.NotifyMessage(Sungero.Docflow.ApprovalRuleBases.Resources.DuplicateNotFound);
    }

    public virtual bool CanShowDuplicate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

}