using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Brands.BrandsRegistration;

namespace DirRX.Brands
{
  partial class BrandsRegistrationClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.BrandsRegistration.SetPropertiesRequirements(_obj);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      e.HideAction(_obj.Info.Actions.CreateFromFile);
      e.HideAction(_obj.Info.Actions.CreateFromScanner);
      e.HideAction(_obj.Info.Actions.CreateFromTemplate);
    }
  }

}