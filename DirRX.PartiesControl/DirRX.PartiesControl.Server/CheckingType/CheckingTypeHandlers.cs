using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.CheckingType;

namespace DirRX.PartiesControl
{
  partial class CheckingTypeServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.DefaultChecking.GetValueOrDefault() && CheckingTypes.GetAll(t => t.DefaultChecking == true && !CheckingTypes.Equals(t, _obj)).Any())
      {
        e.AddError(PartiesControl.CheckingTypes.Resources.DefaultCheckTypeAlreadyExists);
        return;
      }
      
      if (_obj.LukoilChecking.GetValueOrDefault() && CheckingTypes.GetAll(t => t.LukoilChecking == true && !CheckingTypes.Equals(t, _obj)).Any())
      {
        e.AddError(PartiesControl.CheckingTypes.Resources.LukoilCheckTypeAlreadyExists);
        return;
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.DefaultChecking = false;
      
      if (!_obj.State.IsCopied)
        _obj.LukoilChecking = false;
    }
  }

}