using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Department;

namespace DirRX.Solution
{
  partial class DepartmentSharedHandlers
  {

    public override void HeadOfficeChanged(Sungero.Company.Shared.DepartmentHeadOfficeChangedEventArgs e)
    {
      base.HeadOfficeChanged(e);
      _obj.IsHeadOfficeChanged = e.NewValue != e.OriginalValue;
    }

  }
}