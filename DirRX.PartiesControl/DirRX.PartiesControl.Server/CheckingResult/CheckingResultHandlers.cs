using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.CheckingResult;

namespace DirRX.PartiesControl
{
  partial class CheckingResultServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      if (!_obj.State.IsCopied)
        _obj.ForOneDeal = false;
    }

    public override void AfterDelete(Sungero.Domain.AfterDeleteEventArgs e)
    {
      var status = _obj.CounterpartyStatus;
      CounterpartyStatuses.Delete(status);
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var duplicates = Functions.CheckingResult.GetDoubleResults(_obj);
      if (duplicates.Any())
        e.AddError(Sungero.Commons.Resources.DuplicateDetected, _obj.Info.Actions.ShowDuplicates);
      
      var counterpartyStatus = _obj.CounterpartyStatus;
      if (counterpartyStatus != null)
      {
        counterpartyStatus.Name = _obj.Name;
        counterpartyStatus.ForOneDeal = _obj.ForOneDeal.GetValueOrDefault();
        counterpartyStatus.Save();
      }
      else
        _obj.CounterpartyStatus = DirRX.PartiesControl.PublicFunctions.CounterpartyStatus.CreateCounterpartyStatus(_obj.Name, _obj.ForOneDeal.GetValueOrDefault());
    }
  }

}