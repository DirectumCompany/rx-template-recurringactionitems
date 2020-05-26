using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Brands.ProductName;

namespace DirRX.Brands.Server
{
  partial class ProductNameFunctions
  {
    /// <summary>
    /// Получение всех действующих наименований продуктов.
    /// </summary>
    /// <returns>Наименования продуктов.</returns>
    [Public, Remote(IsPure = true)]
    public static IQueryable<DirRX.Brands.IProductName> GetProductNames()
    {
      return ProductNames.GetAll(n => n.Status == Brands.ProductName.Status.Active);
    }
  }
}