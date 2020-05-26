using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ContractCategory;

namespace DirRX.Solution.Shared
{
  partial class ContractCategoryFunctions
  {
    /// <summary>
    /// Установить доступность и обязательность свойств.
    /// </summary>
    public void SetPropertiesAvailabilityAndRequired()
    {
      _obj.State.Properties.Supervisor.IsEnabled = !_obj.IsSupervisorFunctionManager.GetValueOrDefault();
      _obj.State.Properties.Supervisor.IsRequired = !_obj.IsSupervisorFunctionManager.GetValueOrDefault();
    }
  }
}