using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Memo;

namespace DirRX.Solution
{
  partial class MemoSharedHandlers
  {

    public override void DocumentKindChanged(Sungero.Docflow.Shared.OfficialDocumentDocumentKindChangedEventArgs e)
    {
      base.DocumentKindChanged(e);
      if (e.NewValue != null && e.NewValue != e.OldValue)
      {
        var addressee = Solution.Functions.Memo.Remote.GetAddressee(_obj).FirstOrDefault();
        _obj.Addressee = addressee;
      }
    }

  }
}