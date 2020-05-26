using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProcessSubstitutionModule.Client
{
  public class ModuleFunctions
  {

    /// <summary>
    /// Обновить поле Риск в заданиях.
    /// </summary>
    public void UpdateRiskConfirmationField()
    {
      Functions.Module.Remote.UpdateRiskConfirmationField();
    }
    
  }
}