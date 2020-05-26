using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.BusinessProcessGroup;

namespace DirRX.LocalActs.Shared
{
  partial class BusinessProcessGroupFunctions
  {
    /// <summary>
    /// Проверить дубли группы бп.
    /// </summary>
    /// <returns>True, если дубликаты имеются, иначе - false.</returns>
    public bool HaveDuplicates()
    {
      if (_obj.Status == Sungero.Commons.Currency.Status.Closed)
        return false;
      
      return Functions.BusinessProcessGroup.Remote.GetDuplicates(_obj).Any();
    }
  }
}