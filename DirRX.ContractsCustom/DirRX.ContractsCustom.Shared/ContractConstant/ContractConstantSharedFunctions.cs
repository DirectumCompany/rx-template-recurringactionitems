using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ContractConstant;

namespace DirRX.ContractsCustom.Shared
{
	partial class ContractConstantFunctions
	{
		/// <summary>
		/// Установить состояние свойств в зависимости от типа константы.
		/// </summary>
		public void SetPropertiesDependsTypeConst()
		{
			var isPeriod = _obj.TypeConst == TypeConst.Period;
			
			_obj.State.Properties.Period.IsRequired = isPeriod;
			_obj.State.Properties.Period.IsVisible = isPeriod;
			
			_obj.State.Properties.Unit.IsRequired = isPeriod;
			_obj.State.Properties.Unit.IsVisible = isPeriod;
			
			var isDoc = _obj.TypeConst == TypeConst.Document;
			_obj.State.Properties.Document.IsRequired = isDoc;
			_obj.State.Properties.Document.IsVisible = isDoc;
			
			var isAmount = _obj.TypeConst == TypeConst.Amount;
			_obj.State.Properties.Amount.IsRequired = isAmount;
			_obj.State.Properties.Amount.IsVisible = isAmount;
			_obj.State.Properties.Currency.IsRequired = isAmount;
			_obj.State.Properties.Currency.IsVisible = isAmount;
			
			_obj.State.Properties.Unit.IsEnabled = _obj.Sid != Constants.Module.OriginalsControlTaskDeadlineConstantGuid.ToString() 
			  && _obj.Sid != Constants.Module.TermDevelopNewContractFormGuid.ToString();
			
		}

	}
}