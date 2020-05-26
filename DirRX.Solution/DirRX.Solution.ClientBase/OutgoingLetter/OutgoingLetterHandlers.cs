using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.OutgoingLetter;

namespace DirRX.Solution
{
  partial class OutgoingLetterClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);
      Functions.OutgoingLetter.ShowCorresnpondentField(_obj);
    }
  }

}