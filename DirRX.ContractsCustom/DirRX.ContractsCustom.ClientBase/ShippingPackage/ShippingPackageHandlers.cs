using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ShippingPackage;

namespace DirRX.ContractsCustom
{
  partial class ShippingPackageClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      // Изменять документы в пакете можно только в статусе Новый.
      _obj.State.Properties.Documents.IsEnabled = _obj.PackageStatus == PackageStatus.Init;
    }

  }
}