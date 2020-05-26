using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.CurrencyRate;

namespace DirRX.ContractsCustom
{
  partial class CurrencyRateServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Наименование формируется по маске: <Валюта> на <Дата курса>.
      var currencyName = string.Empty;
      if (_obj.Currency != null)
        currencyName = _obj.Currency.Name;
      _obj.Name = DirRX.ContractsCustom.CurrencyRates.Resources.CurrencyRateFormatNameFormat(currencyName, _obj.Date.Value.ToShortDateString());
    }
  }

}