using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ContractConstant;

namespace DirRX.ContractsCustom
{
	partial class ContractConstantClientHandlers
	{

    public virtual void AmountValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (e.NewValue <= 0)
        e.AddError(ContractConstants.Resources.AmountCheck);
    }

		public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
		{
			Functions.ContractConstant.SetPropertiesDependsTypeConst(_obj);
		}

	}
}