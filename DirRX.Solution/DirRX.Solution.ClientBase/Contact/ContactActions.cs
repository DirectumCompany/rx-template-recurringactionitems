using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Contact;

namespace DirRX.Solution.Client
{
  partial class ContactActions
  {
    public virtual void CreateEmployee(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.Person == null)
      {
        e.AddError(DirRX.Solution.Contacts.Resources.PersonIsNotAvailble);
        return;
      }      
      else
      {
        // Проверить, создан ли сотрудник по данной персоне.
        var employee = DirRX.Solution.Functions.Contact.Remote.GetEmployeeByPerson(_obj);
        if (employee != null)
        {
          e.AddInformation(DirRX.Solution.Contacts.Resources.EmployeeIsCreated);
          return;
        }
      }
      
      // Иначе создать нового сотрудника.
      var dialog = Dialogs.CreateInputDialog(DirRX.Solution.Contacts.Resources.InputEmployeeInformation);
      var businessUnit = dialog.AddSelect(DirRX.Solution.Contacts.Resources.OurBusinessUnit, true, DirRX.Solution.BusinessUnits.Null);
      var department = dialog.AddSelect(DirRX.Solution.Contacts.Resources.Department, true, Sungero.Company.Departments.Null)
        .Where(d => DirRX.Solution.BusinessUnits.Equals(businessUnit.Value, d.BusinessUnit));
      if (dialog.Show() == DialogButtons.Ok)
      {
        var employee = DirRX.Solution.Functions.Contact.Remote.AddNewEmployee(_obj, businessUnit.Value, department.Value);
        employee.Show();
      }
    }

    public virtual bool CanCreateEmployee(Sungero.Domain.Client.CanExecuteActionArgs e)
    {      
      return !_obj.State.IsChanged;
    }

  }

}