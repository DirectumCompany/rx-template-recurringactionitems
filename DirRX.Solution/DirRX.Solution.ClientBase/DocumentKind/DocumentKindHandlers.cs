using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.DocumentKind;

namespace DirRX.Solution
{
  partial class DocumentKindClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);
      
      Functions.DocumentKind.ChangeVisibility(_obj);
    }
  }
}
