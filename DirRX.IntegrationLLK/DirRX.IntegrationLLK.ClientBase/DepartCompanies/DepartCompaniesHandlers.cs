using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.IntegrationLLK.DepartCompanies;
using System.Text.RegularExpressions;

namespace DirRX.IntegrationLLK
{
  partial class DepartCompaniesClientHandlers
  {

    public virtual void CodeValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      #region Скопировано из стандартной разработки (Department.Code)
      if (string.IsNullOrWhiteSpace(e.NewValue) || e.NewValue == e.OldValue)
        return;
      
      // Использование пробелов в середине кода запрещено.
      var newCode = e.NewValue.Trim();
      if (Regex.IsMatch(newCode, @"\s"))
        e.AddError(Sungero.Company.Resources.NoSpacesInCode);
      #endregion
    }

  }
}