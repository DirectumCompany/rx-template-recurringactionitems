using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Order;

namespace DirRX.Solution
{
  partial class OrderClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      Functions.Order.SetVisibilityAndRequiredProperties(_obj);
      _obj.State.Properties.NeedTaxMonitoring.IsEnabled = Users.Current.IncludedIn(DirRX.LocalActs.PublicConstants.Module.RoleGuid.TaxMonitoringGuid);
    }
  }
}