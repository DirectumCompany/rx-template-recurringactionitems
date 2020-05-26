using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Brands.ProductName;

namespace DirRX.Brands
{
  partial class ProductNameServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      _obj.Name = string.Format("{0} {1} {2} {3}", _obj.FirstName, _obj.SecondName, _obj.ThirdName, _obj.OtherName).Trim();
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.Description = ProductNames.Resources.ProductNameDescription;
    }
  }

}