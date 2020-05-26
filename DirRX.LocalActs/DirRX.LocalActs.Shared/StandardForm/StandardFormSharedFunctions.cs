using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.StandardForm;

namespace DirRX.LocalActs.Shared
{
  partial class StandardFormFunctions
  {

    /// <summary>
    /// Задание доступности и видимости свойств.
    /// </summary>
    public void SetStateProperties()
    {
      if (_obj.DocumentType != null)
      {
        var isOrder = _obj.DocumentType.DocumentTypeGuid == DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.Order;
        var isMemo = _obj.DocumentType.DocumentTypeGuid == DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.Memo;

        _obj.State.Properties.Subject.IsEnabled = isOrder;
        _obj.State.Properties.Supervisor.IsEnabled = isOrder;
        _obj.State.Properties.IsBPOwner.IsEnabled = isOrder;
        _obj.State.Properties.NeedTaxMonitoring.IsEnabled = isOrder;
        _obj.State.Properties.NeedRegulatoryDocument.IsEnabled = isOrder;
        _obj.State.Properties.NeedCheckTrademarkRegistration.IsEnabled = isOrder;
        _obj.State.Properties.NeedPaperSigning.IsEnabled = isOrder;
        _obj.State.Properties.NeedPersonalSignatureAcquaintance.IsEnabled = isOrder;
        _obj.State.Properties.AcquaintanceList.IsEnabled = isOrder;
        _obj.State.Properties.Addressee.IsEnabled = isMemo;
      }
    }
  }
}