using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.StandardForm;

namespace DirRX.LocalActs
{
  partial class StandardFormClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      _obj.State.Properties.Supervisor.IsEnabled = !_obj.IsBPOwner.Value;
      Functions.StandardForm.SetStateProperties(_obj);
    }

  }
}