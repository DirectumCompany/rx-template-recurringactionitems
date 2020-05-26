using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProcessSubstitutionModule.ProcessSubstitution;

namespace DirRX.ProcessSubstitutionModule
{
  partial class ProcessSubstitutionClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.ProcessSubstitution.SetPropertiesAvailabilityAndVisibility(_obj);
    }

  }
}