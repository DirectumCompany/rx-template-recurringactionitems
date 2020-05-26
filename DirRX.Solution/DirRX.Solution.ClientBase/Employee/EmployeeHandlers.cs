using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Employee;

namespace DirRX.Solution
{
  partial class EmployeeClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);
      
      _obj.State.Properties.PersonnelNumber.IsVisible = Users.Current.IncludedIn(Roles.Administrators);
    }

  }
}