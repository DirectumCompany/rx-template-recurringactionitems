using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Brands.BrandsRegistration;

namespace DirRX.Brands.Shared
{
  partial class BrandsRegistrationFunctions
  {

    /// <summary>
    /// Установить обязательность свойств.
    /// </summary>       
    public void SetPropertiesRequirements()
    {
      bool needFillDates = _obj.Status == Brands.BrandsRegistration.Status.Registered || _obj.Status == Brands.BrandsRegistration.Status.Overdue;
      _obj.State.Properties.RegistrationDate.IsRequired = needFillDates;
      _obj.State.Properties.ValidUntil.IsRequired = needFillDates;
    }

  }
}