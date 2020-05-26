using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.MatchingSetting;

namespace DirRX.ContractsCustom.Shared
{
  partial class MatchingSettingFunctions
  {

    /// <summary>
    /// Установить обязательность полей.
    /// </summary>       
    public void SetRequiredProperties()
    {
			_obj.State.Properties.DocumentGroup.IsRequired = _obj.DocumentKind != null && DirRX.Solution.ContractCategories.GetAllCached(g => g.DocumentKinds.Any(x => Equals(x.DocumentKind, _obj.DocumentKind))).Any();
      // убрали обязательность по просьбе Гришы
			// _obj.State.Properties.ContractSubcategory.IsRequired = _obj.DocumentGroup != null && _obj.DocumentGroup.CounterpartySubcategories.Any(x => x.Subcategories != null);
    }

  }
}