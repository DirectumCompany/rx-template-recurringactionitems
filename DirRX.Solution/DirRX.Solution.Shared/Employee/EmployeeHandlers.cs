using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Employee;

namespace DirRX.Solution
{
  partial class EmployeeSharedHandlers
  {

    public override void DepartmentChanged(Sungero.Company.Shared.EmployeeDepartmentChangedEventArgs e)
    {
      base.DepartmentChanged(e);
      
      _obj.BusinessUnit = (e.NewValue != null && e.NewValue.BusinessUnit != null) ? DirRX.Solution.BusinessUnits.As(e.NewValue.BusinessUnit) : null;
      _obj.HeadOffice = (e.NewValue != null && e.NewValue.HeadOffice != null) ? DirRX.Solution.Departments.As(e.NewValue.HeadOffice) : null;
    }

  }
}