using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Solution.Shared
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Выдать права на сущность руководителям сотрудника.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <param name="employee">Сотрудник.</param>
    public void GrantAccesRightsForManagers(Sungero.Domain.Shared.IEntity entity, Sungero.Company.IEmployee employee)
    {
      var recipients = new System.Collections.Generic.List<IRecipient>();
      
      Functions.Module.Remote.GetManagers(DirRX.Solution.Employees.As(employee), recipients, 0);
      
      foreach (var recipient in recipients)
      {
        if (recipient != null)
          entity.AccessRights.Grant(recipient, DefaultAccessRightsTypes.Read);
      }
    }
    
    /// <summary>
    /// Обрезает строку до длины строки переданного строкового свойства property.
    /// </summary>
    /// <param name="text">Строка.</param>
    /// <param name="property">Строковое свойство.</param>
    /// <returns>Строка, обрезанная до длины строкового свойства.</returns>
    [Public]
    public string SubstringStringPropertyText(string text, Sungero.Domain.Shared.IStringPropertyInfo property)
    {
      if (text.Length > property.Length)
        return text.Remove(property.Length);
      else
        return text;
    }
  }
}