using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.DocumentKind;

namespace DirRX.Solution
{
  partial class DocumentKindSharedHandlers
  {

    public override void DocumentFlowChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      base.DocumentFlowChanged(e);
      
      if (e.NewValue != e.OldValue)
      {
        if (e.NewValue != DocumentFlow.Contracts && _obj.NotCheckCounterparty == true)
          _obj.NotCheckCounterparty = false;
        
        Functions.DocumentKind.ChangeVisibility(_obj);
      }
    }
  }

}