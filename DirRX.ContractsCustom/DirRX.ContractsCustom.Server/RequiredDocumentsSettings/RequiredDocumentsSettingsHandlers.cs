using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.RequiredDocumentsSettings;

namespace DirRX.ContractsCustom
{
  partial class RequiredDocumentsSettingsServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.YearLabel = DirRX.ContractsCustom.ContractSettingses.Resources.YearLabelText;
    }
  }

}