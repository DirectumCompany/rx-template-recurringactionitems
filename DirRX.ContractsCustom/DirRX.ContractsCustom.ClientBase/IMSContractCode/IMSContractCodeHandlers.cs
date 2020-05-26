using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.IMSContractCode;

namespace DirRX.ContractsCustom
{
  partial class IMSContractCodeClientHandlers
  {

    public virtual void NameValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue && 
          e.NewValue.Length != DirRX.ContractsCustom.Constants.IMSContractCode.CorrectNameLength)
        e.AddError(DirRX.ContractsCustom.IMSContractCodes.Resources.IncorrectNameLength);
    }
  }

}