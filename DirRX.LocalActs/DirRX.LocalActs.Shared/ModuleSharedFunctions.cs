using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.LocalActs.Shared
{
  public class ModuleFunctions
  {
    
    [Public]
    /// <summary>
    /// Выдать права на сущность руководителям сотрудника.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <param name="employee">Сотрудник.</param>
    /// <param name="accessRightsType">Тип прав.</param>
    public void GrantAccesRightsForManagers(Sungero.Domain.Shared.IEntity entity, Sungero.Company.IEmployee employee, Guid accessRightsType)
    {
      var recipients = new System.Collections.Generic.List<IRecipient>();
      
      Solution.PublicFunctions.Module.Remote.GetManagers(DirRX.Solution.Employees.As(employee), recipients, 0);
      
      foreach (var recipient in recipients)
      {
        if (recipient != null)
          entity.AccessRights.Grant(recipient, accessRightsType);
      }
    }

  }
}