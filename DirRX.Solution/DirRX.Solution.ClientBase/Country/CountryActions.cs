using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Country;

namespace DirRX.Solution.Client
{
  partial class CountryActions
  {
    public virtual void GroupCountries(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var countries = Functions.Country.Remote.GetGroupCountries(_obj);
      if (countries.Any())
        countries.Show();
      else
        Dialogs.NotifyMessage(Countries.Resources.GroupCountriesNotFound);
    }

    public virtual bool CanGroupCountries(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.GroupFlag.HasValue ? _obj.GroupFlag.Value : false;
    }

    public override void ShowDuplicates(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Скопировано из стандартной разработки, проверка дублей осуществляется по наименованию на английском.
      // Проверка по коду не актуальна, из-за снятия обязательности указания кода.
      var duplicates = Functions.Country.Remote.GetDuplicatesByName(_obj);
      if (duplicates.Any())
        duplicates.Show();
      else
        Dialogs.NotifyMessage(Sungero.Commons.Resources.DuplicateNotFound);
    }

    public override bool CanShowDuplicates(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanShowDuplicates(e);
    }

  }

}