using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.IntegrationLLK.DepartCompanies;
using DirRX.Solution.Contact;

namespace DirRX.IntegrationLLK.Server
{
  partial class DepartCompaniesFunctions
  {

    /// <summary>
    /// Получить список контактных лиц подразделения.
    /// </summary>
    /// <param name="subdivision">Подразделение организации.</param>
    /// <returns>Список контактов.</returns>
    [Remote(IsPure = true)]
    public static List<Solution.IContact> GetContactsFromSubdivision(IDepartCompanies subdivision)
    {      
      return Solution.Contacts.GetAll(c => Equals(subdivision, c.Subdivision)).ToList();
    }

  }
}