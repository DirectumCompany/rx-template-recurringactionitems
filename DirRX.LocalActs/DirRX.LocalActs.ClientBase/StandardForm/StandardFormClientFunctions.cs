using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.StandardForm;

namespace DirRX.LocalActs.Client
{
  partial class StandardFormFunctions
  {
    /// <summary>
    /// Валидация при сохранении.
    /// </summary>
    /// <param name="e">Аргумент события</param>
    /// <returns>Валидация пройдена успешно</returns>
    public bool Validation(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var stdForms = DirRX.LocalActs.PublicFunctions.StandardForm.Remote.FindStandardForms(_obj.DocumentKind).Where(x => !StandardForms.Equals(_obj, x) && x.Status == StandardForm.Status.Active);
      if (stdForms.Any() && _obj.Status == StandardForm.Status.Active)
      {
        e.AddError(DirRX.LocalActs.StandardForms.Resources.StandardFormValidationMessage, _obj.Info.Actions.ShowExistingForms, 
                                                                                               _obj.Info.Actions.SaveDuplicate);
        return false;
      }
      else
        return true;
    }
    
  }
}