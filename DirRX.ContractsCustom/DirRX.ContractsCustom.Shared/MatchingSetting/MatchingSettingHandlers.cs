using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.MatchingSetting;

namespace DirRX.ContractsCustom
{
  partial class MatchingSettingSharedHandlers
  {

    public virtual void DocumentGroupChanged(DirRX.ContractsCustom.Shared.MatchingSettingDocumentGroupChangedEventArgs e)
    {
    	if (e.NewValue == e.OldValue)
    		return;
    	
    	Functions.MatchingSetting.SetRequiredProperties(_obj);
    	
    	if (e.NewValue != null && _obj.ContractSubcategory != null && !e.NewValue.CounterpartySubcategories.Any(x => ContractSubcategories.Equals(x.Subcategories, _obj.ContractSubcategory)))
    		_obj.ContractSubcategory = null;
    }

    public virtual void DocumentKindChanged(DirRX.ContractsCustom.Shared.MatchingSettingDocumentKindChangedEventArgs e)
    {
    	if (e.NewValue == e.OldValue)
    		return;
    	
    	Functions.MatchingSetting.SetRequiredProperties(_obj);

    	if (e.NewValue != null && _obj.DocumentGroup != null && !_obj.DocumentGroup.DocumentKinds.Any(x => Equals(x.DocumentKind, e.NewValue)))
    	{
    		_obj.DocumentGroup = null;
    		_obj.ContractSubcategory = null;
    	}
    }

  }
}