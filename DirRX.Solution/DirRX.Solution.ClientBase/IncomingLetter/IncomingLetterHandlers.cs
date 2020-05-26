using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.IncomingLetter;

namespace DirRX.Solution
{
  partial class IncomingLetterClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      var state = _obj.State.Properties.Correspondent.IsEnabled;
      _obj.State.Properties.CorrespondentDepDirRX.IsEnabled = state;
      _obj.State.Properties.RequirementNumberDirRX.IsEnabled = state;
    }
  }

}