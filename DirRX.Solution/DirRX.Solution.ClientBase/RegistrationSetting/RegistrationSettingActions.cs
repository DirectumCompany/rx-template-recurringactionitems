using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.RegistrationSetting;

namespace DirRX.Solution.Client
{
  partial class RegistrationSettingActions
  {
    public virtual void AddDepartments(Sungero.Domain.Client.ExecuteActionArgs e)
    {      
      var departmentsList = Sungero.Company.PublicFunctions.Department.Remote.GetDepartments();  
      var selectedDepartments = _obj.Departments.Select(d => d.Department);
      
      var departments = departmentsList.ToList().Where(d => !selectedDepartments.Contains(d)).ShowSelectMany();
      
      foreach (var department in departments)
      {
        var departmentItem = _obj.Departments.AddNew();
        departmentItem.Department = department;
      }
    }

    public virtual bool CanAddDepartments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

}