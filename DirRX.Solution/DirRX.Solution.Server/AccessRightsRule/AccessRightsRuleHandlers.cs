using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.AccessRightsRule;

namespace DirRX.Solution
{
  partial class AccessRightsRuleServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      _obj.IsSigned = false;
      _obj.IsRegistered = false;
    }
  }

}