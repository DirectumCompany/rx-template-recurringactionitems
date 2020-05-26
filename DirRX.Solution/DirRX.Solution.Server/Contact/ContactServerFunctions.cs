using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Contact;

namespace DirRX.Solution.Server
{
  partial class ContactFunctions
  {
    
    /// <summary>
    /// Найти сотрудника по персоне.
    /// </summary>
    /// <param name="contact">Контакт.</param>
    /// <returns>Сотрудник.</returns>
    [Remote(IsPure = true)]
    public static DirRX.Solution.IEmployee GetEmployeeByPerson(DirRX.Solution.IContact contact)
    {
      return DirRX.Solution.Employees.GetAll(e => Equals(e.Person, contact.Person)).FirstOrDefault();
    }

    /// <summary>
    /// Добавить нового сотрудника.
    /// </summary>
    /// <param name="contact">Контакт-будущий сотрудник.</param>
    /// <param name="businessUnit">Наша организация.</param>
    /// <param name="department">Подразделение.</param>
    /// <returns>Новая запись сотрудника с предзаполненными полями.</returns>
    [Remote]
    public static DirRX.Solution.IEmployee AddNewEmployee(DirRX.Solution.IContact contact, DirRX.Solution.IBusinessUnit businessUnit, Sungero.Company.IDepartment department)
    {
      var employee = DirRX.Solution.Employees.Create();
      employee.Person = contact.Person;
      // Найти учетную запись пользователя по логину. Иначе создать новую.
      var contactLogin = contact.Login;
      if (!string.IsNullOrWhiteSpace(contactLogin))
      {
        var login = Logins.GetAll(l => l.LoginName.Contains(contactLogin)).FirstOrDefault();
        if (login == null)
        {
          login = Logins.Create();
          login.LoginName = contactLogin;
          login.TypeAuthentication = Sungero.CoreEntities.Login.TypeAuthentication.Windows;
          login.Save();
        }
        employee.Login = login;     
      }
      
      
      employee.Department = department;
      employee.BusinessUnit = businessUnit;
      employee.PersonnelNumber = contact.PersonnelNumber;
      employee.Phone = contact.Phone;
      employee.Email = contact.Email;
      // Должность искать по наименованию. Если такой должности нет, то создавать.
      var jobTitleName = contact.JobTitle;
      if (!string.IsNullOrWhiteSpace(jobTitleName))
      {
        var jobTitle = Sungero.Company.JobTitles.GetAll(j => j.Name.Contains(jobTitleName)).FirstOrDefault();
        if (jobTitle == null)
        {
          jobTitle = Sungero.Company.JobTitles.Create();
          jobTitle.Name = jobTitleName;           
          jobTitle.Save();
        }
        employee.JobTitle = jobTitle;
      }
      return employee;
    }

  }
}