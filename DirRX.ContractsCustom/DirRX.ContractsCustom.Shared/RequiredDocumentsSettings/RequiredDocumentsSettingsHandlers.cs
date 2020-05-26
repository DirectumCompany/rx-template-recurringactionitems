using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.RequiredDocumentsSettings;

namespace DirRX.ContractsCustom
{
  partial class RequiredDocumentsSettingsSharedHandlers
  {

    public virtual void TransactionAmountChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
    {
      if (e.NewValue == null || e.NewValue == e.OldValue)
        return;
      
      // Подставить валюту по умолчанию.
      if (_obj.Currency == null)
      {
        var defaultCurrency = Sungero.Commons.PublicFunctions.Currency.Remote.GetDefaultCurrency();
        if (defaultCurrency != null)
          _obj.Currency = defaultCurrency;
      }
    }

    public virtual void DocumentValidityChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      if (e.NewValue == e.OldValue)
        return;
      // Заполнить метку в зависимости от значения поля.
      if (e.NewValue == null)
      {
        _obj.YearLabel = string.Empty;
        return;
      }
      var titles = new string[]{"год", "года", "лет"};
      var decCases = new int[]{2, 0, 1, 1, 1, 2};
      var index = e.NewValue.Value % 100 > 4 && e.NewValue.Value % 100 < 20 ? 2 : decCases[Math.Min(e.NewValue.Value % 10, 5)];
      _obj.YearLabel = titles[index];
    }

  }
}