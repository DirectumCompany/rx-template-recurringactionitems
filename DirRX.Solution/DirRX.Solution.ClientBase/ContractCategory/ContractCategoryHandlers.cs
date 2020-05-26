using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ContractCategory;

namespace DirRX.Solution
{
  partial class ContractCategoryClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      Functions.ContractCategory.SetPropertiesAvailabilityAndRequired(_obj);
    }
  }

}