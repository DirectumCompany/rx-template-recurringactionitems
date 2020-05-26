using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.CheckingDocumentList;

namespace DirRX.PartiesControl
{
  partial class CheckingDocumentListServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      Functions.CheckingDocumentList.FillName(_obj);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.Name = CheckingDocumentLists.Resources.AutoGenerateText;
    }
  }

}