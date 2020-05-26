using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Memo;

namespace DirRX.Solution.Shared
{
  partial class MemoFunctions
  {

    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool isRepeatRegister)
    {
      base.ChangeDocumentPropertiesAccess(isEnabled, isRepeatRegister);
      
      _obj.State.Properties.Assignee.IsEnabled = false;
    }
  }
}