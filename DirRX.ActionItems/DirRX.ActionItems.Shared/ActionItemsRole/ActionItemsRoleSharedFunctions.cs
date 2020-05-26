using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.ActionItemsRole;

namespace DirRX.ActionItems.Shared
{
  partial class ActionItemsRoleFunctions
  {
    /// <summary>
    /// Получить список ролей, доступных для типа сущности.
    /// </summary>
    /// <param name="info">Информация о типе сущности.</param>
    /// <returns>Список ролей.</returns>
    [Public]
    public static List<Enumeration?> GetPossibleRoles(Sungero.Domain.Shared.IEntityInfo info)
    {
      var roleTypes = new List<Enumeration?>();
      
      if (info == Priorities.Info)
      {
        roleTypes.Add(ActionItems.ActionItemsRole.Type.CEO);
        roleTypes.Add(ActionItems.ActionItemsRole.Type.InitCEOManager);
        roleTypes.Add(ActionItems.ActionItemsRole.Type.InitManager);
      }
      
      if (info == ControlSettings.Info)
      {
        roleTypes.Add(ActionItems.ActionItemsRole.Type.CEO);
        roleTypes.Add(ActionItems.ActionItemsRole.Type.InitCEOManager);
        roleTypes.Add(ActionItems.ActionItemsRole.Type.Secretary);
        roleTypes.Add(ActionItems.ActionItemsRole.Type.InitManager);
        roleTypes.Add(ActionItems.ActionItemsRole.Type.CEOAssistant);
      }
      
      if (info == Categories.Info)
      {
        roleTypes.Add(ActionItems.ActionItemsRole.Type.CEO);
        roleTypes.Add(ActionItems.ActionItemsRole.Type.CEOAssistant);
      }
      
      return roleTypes;
    }
  }
}