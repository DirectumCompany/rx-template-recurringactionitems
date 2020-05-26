using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.OutgoingLetter;

namespace DirRX.Solution.Client
{
  partial class OutgoingLetterActions
  {
    public override void ChangeManyAddressees(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ChangeManyAddressees(e);
      
      Functions.OutgoingLetter.ShowCorresnpondentField(_obj);
      _obj.CorrespondentManyAddressDirRX = OutgoingLetters.Resources.ToMailingList;
      
    }

    public override bool CanChangeManyAddressees(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanChangeManyAddressees(e);
    }

    public override void Register(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.Register(e);
      
    }

    public override bool CanRegister(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanRegister(e);
    }

  }

}