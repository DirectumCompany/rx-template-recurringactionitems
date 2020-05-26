using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.BusinessProcessGroup;

namespace DirRX.LocalActs.Server
{
  partial class BusinessProcessGroupFunctions
  {
    /// <summary>
    /// Получить дубли группы бп.
    /// </summary>
    /// <returns>Группы бп, дублирующие текущую.</returns>
    [Remote(IsPure = true)]
    public IQueryable<IBusinessProcessGroup> GetDuplicates()
    {
      return BusinessProcessGroups.GetAll()
        .Where(g => g.Status != DirRX.LocalActs.BusinessProcessGroup.Status.Closed)
        .Where(g => Equals(g.Code, _obj.Code))
        .Where(g => !Equals(g, _obj));
    }
  }
}