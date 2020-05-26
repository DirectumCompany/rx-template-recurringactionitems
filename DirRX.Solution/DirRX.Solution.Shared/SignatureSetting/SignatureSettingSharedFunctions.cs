using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.SignatureSetting;

namespace DirRX.Solution.Shared
{
  partial class SignatureSettingFunctions
  {
    /// <summary>
    /// Сменить доступность реквизитов.
    /// </summary>
    public override void ChangePropertiesAccess()
    {
      base.ChangePropertiesAccess();
      
      // Валюта всегда рубль и не доступна для редактирования.
      _obj.State.Properties.Currency.IsEnabled = false;
    }
  }
}