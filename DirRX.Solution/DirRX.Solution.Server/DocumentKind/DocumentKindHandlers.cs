using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.DocumentKind;

namespace DirRX.Solution
{
  partial class DocumentKindServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      _obj.NotCheckCounterparty = false;
    }
  }

}