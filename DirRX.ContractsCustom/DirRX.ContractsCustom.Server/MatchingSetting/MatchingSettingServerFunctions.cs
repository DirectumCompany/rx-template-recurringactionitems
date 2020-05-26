using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.MatchingSetting;

namespace DirRX.ContractsCustom.Server
{
  partial class MatchingSettingFunctions
  {

    /// <summary>
    /// Получить дублирующие настройки.
    /// </summary>
    /// <returns>Правила, конфликтующие с текущим.</returns> 
    [Remote(IsPure=true)]
    public IQueryable<IMatchingSetting> GetDoubles()
    {
    	return MatchingSettings.GetAll(x => x.Id != _obj.Id &&
    	                               DirRX.Solution.DocumentKinds.Equals(x.DocumentKind, _obj.DocumentKind) &&
    	                               DirRX.Solution.ContractCategories.Equals(x.DocumentGroup, _obj.DocumentGroup) &&
    	                               DirRX.ContractsCustom.ContractSubcategories.Equals(x.ContractSubcategory, _obj.ContractSubcategory));
    }

  }
}