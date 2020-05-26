using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ContractConstant;

namespace DirRX.ContractsCustom
{
	partial class ContractConstantServerHandlers
	{

		public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
		{
			if(_obj.TypeConst == TypeConst.Period)
			{
				
				if (_obj.Period <= 0)
				{
					e.AddError("Срок должен быть больше нуля.");
					return;
				}
				
			}
		}

	}
}