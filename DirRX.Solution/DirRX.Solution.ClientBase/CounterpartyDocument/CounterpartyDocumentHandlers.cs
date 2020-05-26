using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.CounterpartyDocument;

namespace DirRX.Solution
{
  partial class CounterpartyDocumentClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);
      if (!_obj.HasVersions && !string.IsNullOrEmpty(_obj.BodyKSSSLink))
        e.AddInformation(CounterpartyDocuments.Resources.GetBodyInformation);
    }

  }
}