using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Order;

namespace DirRX.Solution.Client
{
  partial class OrderFunctions
  {
    /// <summary>
    /// Добавление текстовой информации о незарегистрированных товарных знаках.
    /// </summary>
    /// <param name="information"></param>
    /// <param name="missBrands"></param>
    /// <param name="title"></param>
    public static void AddInformation(System.Text.StringBuilder information,
                                      System.Collections.Generic.IEnumerable<Structures.RecordManagement.Order.MissBrands> missBrands,
                                      string title)
    {
      information.AppendLine().AppendLine(title);
      
      foreach (var missBrand in missBrands)
      {
        var countries = string.Join("; ", missBrand.Countries.Select(c => c.Country.Name));
        information.AppendLine(Orders.Resources.MissBrandInfoFormat(missBrand.WordMark.Name, countries));
      }
    }
    
    public void AddOrChangeBrand(IOrderBrands brandItem)
    {
      #region Формирование диалога.
      
      var dialog = Dialogs.CreateInputDialog(Solution.Orders.Resources.BrandDialogTitle);
      var productNames = DirRX.Brands.PublicFunctions.ProductName.Remote.GetProductNames();
      var isWeb = ClientApplication.ApplicationType == ApplicationType.Web;
      
      var firstName = dialog.AddSelect(Solution.Orders.Resources.FirstName, true, Brands.WordMarks.Null)
        .Where(w => w.Status == Brands.WordMark.Status.Active && productNames.Any(n => Brands.ProductNames.Equals(w, n.FirstName)));
      var secondName = dialog.AddSelect(Solution.Orders.Resources.SecondName, false, Brands.WordMarks.Null).Where(w => w.Status == Brands.WordMark.Status.Active);
      secondName.IsEnabled = false;
      var thirdName = dialog.AddSelect(Solution.Orders.Resources.ThirdName, false, Brands.WordMarks.Null).Where(w => w.Status == Brands.WordMark.Status.Active);
      thirdName.IsEnabled = false;
      var otherName = dialog.AddSelect(Solution.Orders.Resources.OtherName, false, Brands.WordMarks.Null).Where(w => w.Status == Brands.WordMark.Status.Active);
      otherName.IsEnabled = false;
      
      var countriesD = new List<DirRX.Solution.ICountry>();
      var countriesP = new List<DirRX.Solution.ICountry>();
      if (brandItem != null)
      {
        countriesD = _obj.CountriesOfDelivery.Where(c => c.IdBrand == brandItem.IdBrand).Select(c => c.Country).ToList();
        countriesP = _obj.CountriesOfProduction.Where(c => c.IdBrand == brandItem.IdBrand).Select(c => c.Country).ToList();
      }
      var countriesOfDelivery = dialog.AddSelectMany(Solution.Orders.Resources.CountriesOfDelivery, !isWeb, countriesD.ToArray())
        .Where(c => c.Status == Country.Status.Active && !c.GroupFlag.GetValueOrDefault());
      var countriesOfProduction = dialog.AddSelectMany(Solution.Orders.Resources.CountriesOfProduction, !isWeb, countriesP.ToArray())
        .Where(c => c.Status == Country.Status.Active && !c.GroupFlag.GetValueOrDefault());
      
      if (isWeb)
      {
        countriesOfDelivery = countriesOfDelivery.From(countriesD.ToArray());
        countriesOfProduction = countriesOfProduction.From(countriesP.ToArray());
      }
      
      var saveButton = dialog.Buttons.AddCustom(Solution.Orders.Resources.ButtonSave);
      dialog.Buttons.AddCancel();
      
      #endregion
      
      #region Заполнение словестных обозначений.
      
      // Реквизиты доступны только, если заполнено предыдущее слово.
      firstName.SetOnValueChanged(
        (sc) =>
        {
          if (sc.NewValue != null)
          {
            if (sc.NewValue != sc.OldValue)
            {
              secondName.IsEnabled = true;
              secondName = secondName.Where(w => productNames.Any(n => DirRX.Brands.ProductNames.Equals(w, n.SecondName) &&
                                                                  DirRX.Brands.ProductNames.Equals(firstName.Value, n.FirstName)));
              secondName.Value = null;
              thirdName.Value = null;
              otherName.Value = null;
            }
          }
          else
          {
            secondName.IsEnabled = false;
            secondName.Value = null;
            thirdName.IsEnabled = false;
            thirdName.Value = null;
            otherName.IsEnabled = false;
            otherName.Value = null;
          }
        });
      
      secondName.SetOnValueChanged(
        (sc) =>
        {
          if (sc.NewValue != null)
          {
            if (sc.NewValue != sc.OldValue)
            {
              thirdName.IsEnabled = true;
              thirdName = thirdName.Where(w => productNames.Any(n => DirRX.Brands.ProductNames.Equals(w, n.ThirdName) &&
                                                                DirRX.Brands.ProductNames.Equals(firstName.Value, n.FirstName) &&
                                                                DirRX.Brands.ProductNames.Equals(secondName.Value, n.SecondName)));
              thirdName.Value = null;
              otherName.Value = null;
            }
          }
          else
          {
            thirdName.IsEnabled = false;
            thirdName.Value = null;
            otherName.IsEnabled = false;
            otherName.Value = null;
          }
        });
      
      thirdName.SetOnValueChanged(
        (sc) =>
        {
          if (sc.NewValue != null)
          {
            if (sc.NewValue != sc.OldValue)
            {
              otherName.IsEnabled = true;
              otherName = otherName.Where(w => productNames.Any(n => DirRX.Brands.ProductNames.Equals(w, n.OtherName) &&
                                                                DirRX.Brands.ProductNames.Equals(firstName.Value, n.FirstName) &&
                                                                DirRX.Brands.ProductNames.Equals(secondName.Value, n.SecondName) &&
                                                                DirRX.Brands.ProductNames.Equals(thirdName.Value, n.ThirdName)));
              otherName.Value = null;
            }
          }
          else
          {
            otherName.IsEnabled = false;
            otherName.Value = null;
          }
        });
      
      #endregion
      
      if (brandItem != null)
      {
        firstName.Value = brandItem.FirstName;
        secondName.Value = brandItem.SecondName;
        thirdName.Value = brandItem.ThirdName;
        otherName.Value = brandItem.OtherName;
      }
      
      if (dialog.Show() == saveButton)
      {
        var idBarnd = 0;
        if (brandItem == null)
        {
          brandItem = _obj.Brands.AddNew();
          // Id для связи записи в табличной части товарных знаков и скрытых коллекций для стран поставки и производства. Стандартный Id на данном этапе ещё не сформирован.
          idBarnd = _obj.Brands.Where(b => b.IdBrand.HasValue).Select(b => b.IdBrand.Value).DefaultIfEmpty(-1).Max() + 1;
          brandItem.IdBrand = idBarnd;
        }
        else
        {
          idBarnd = brandItem.IdBrand.Value;
          Functions.Order.ClearCountries(_obj, brandItem.IdBrand.Value, true, true);
        }
        
        brandItem.FirstName = firstName.Value;
        brandItem.SecondName = secondName.Value;
        brandItem.ThirdName = thirdName.Value;
        brandItem.OtherName = otherName.Value;
        
        // Формирование текстового отображения стран поставки для визуальной табличной части и заполнение соответствующей скрытой коллекции.
        foreach (var countryOfDelivery in countriesOfDelivery.Value)
        {
          var countryOfDeliveryItem = _obj.CountriesOfDelivery.AddNew();
          countryOfDeliveryItem.IdBrand = idBarnd;
          countryOfDeliveryItem.Country = countryOfDelivery;
        }
        brandItem.CountryOfDelivery = string.Join("; ", countriesOfDelivery.Value.Select(c => c.Name)).Trim();
        
        // Формирование текстового отображения стран производства для визуальной табличной части и заполнение соответствующей скрытой коллекции.
        foreach (var countryOfProduction in countriesOfProduction.Value)
        {
          var countryOfProductionItem = _obj.CountriesOfProduction.AddNew();
          countryOfProductionItem.IdBrand = idBarnd;
          countryOfProductionItem.Country = countryOfProduction;
        }
        brandItem.CountryOfProduction = string.Join("; ", countriesOfProduction.Value.Select(c => c.Name)).Trim();
      }
    }
    
    /// <summary>
    /// Проверка регистрации товарных знаков.
    /// </summary>
    public bool CheckBrandsRegistration(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.RightHolder == null)
        _obj.AllWordMarksRegistred = true;
      else
      {
        if (_obj.RightHolder != RightHolder.ThirdParty)
        {          
          if (_obj.Brands.Any(b => string.IsNullOrEmpty(b.CountryOfDelivery) || string.IsNullOrEmpty(b.CountryOfProduction)))
          {
            e.AddError(Solution.Orders.Resources.BrandCountriesError);
            return false;
          }
          
          var missBrands = Functions.Order.Remote.GetMissBrands(_obj);
          if (missBrands.Any())
          {
            _obj.AllWordMarksRegistred = false;
            var information = new System.Text.StringBuilder();
            
            if (missBrands.Any(b => !b.IsAppeal))
              Functions.Order.AddInformation(information, missBrands.Where(b => !b.IsAppeal), Solution.Orders.Resources.MissRegistrationFormat(_obj.ProductKind));
            
            if (missBrands.Any(b => b.IsAppeal))
              Functions.Order.AddInformation(information, missBrands.Where(b => b.IsAppeal), Solution.Orders.Resources.IsAppeal);
            
            var dialog = Dialogs.CreateTaskDialog(Solution.Orders.Resources.CheckBrandsRegistration, information.ToString().TrimStart(), MessageType.Warning);
            var saveButton = dialog.Buttons.AddCustom(Solution.Orders.Resources.ButtonSave);
            dialog.Buttons.AddCancel();
            
            if (dialog.Show() != saveButton)
              return false;
          }
          else
            _obj.AllWordMarksRegistred = true;
        }
        else
          _obj.AllWordMarksRegistred = false;
      }
      return true;
    }
  }
}