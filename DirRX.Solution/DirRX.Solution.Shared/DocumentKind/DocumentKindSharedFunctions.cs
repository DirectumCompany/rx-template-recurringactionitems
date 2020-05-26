using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.DocumentKind;

namespace DirRX.Solution.Shared
{
  partial class DocumentKindFunctions
  {
    /// <summary>
    /// Установить видимость признака "Проверка контрагента не требуется" для договорного документопотока.
    /// </summary>
    /// <param name="documentFlow">Документопоток.</param>
    public void ChangeVisibility()
    {
      _obj.State.Properties.NotCheckCounterparty.IsVisible = _obj.DocumentFlow == Sungero.Docflow.DocumentKind.DocumentFlow.Contracts;
    }
  }
}