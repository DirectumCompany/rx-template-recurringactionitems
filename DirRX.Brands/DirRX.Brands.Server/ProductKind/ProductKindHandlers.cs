using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Brands.ProductKind;

namespace DirRX.Brands
{
  partial class ProductKindServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.Name = DirRX.Brands.ProductKinds.Resources.AutoGenerateText;
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      _obj.Name = string.Format("{0}/{1}", _obj.InLocalLanguage, _obj.InEnglishLanguage);
    }
  }

}