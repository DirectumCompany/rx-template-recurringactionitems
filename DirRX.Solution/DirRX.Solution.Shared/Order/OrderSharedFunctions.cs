using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Order;

namespace DirRX.Solution.Shared
{
  partial class OrderFunctions
  {
    /// <summary>
    /// Установить видимость и обязательность свойств.
    /// </summary>
    public void SetVisibilityAndRequiredProperties()
    {
      var needBrands = _obj.StandardForm != null ? _obj.StandardForm.NeedCheckTrademarkRegistration.GetValueOrDefault() : false;
      var needRegulatoryDocument = _obj.StandardForm != null ? _obj.StandardForm.NeedRegulatoryDocument.GetValueOrDefault() : false;
      
      var isThirdParty = _obj.RightHolder == Solution.Order.RightHolder.ThirdParty;
      var properties = _obj.State.Properties;
      
      bool isEnable = _obj.State.Properties.Subject.IsEnabled;
      properties.BPGroup.IsEnabled = isEnable;
      properties.Theme.IsEnabled = isEnable;
      properties.NeedTaxMonitoring.IsEnabled = isEnable;
      properties.StandardForm.IsEnabled = isEnable;
      properties.Supervisor.IsEnabled = isEnable;
      properties.RightHolder.IsEnabled = isEnable;
      properties.ProductKind.IsEnabled = isEnable;
      properties.Brands.IsEnabled = isEnable;
      properties.RegulatoryDocument.IsEnabled = isEnable;
      properties.OurSignatory.IsEnabled = Users.Current.IncludedIn(DirRX.LocalActs.PublicConstants.Module.RoleGuid.RegulatoryDocumentsUpdaterRoleGuid);
      
      properties.RightHolder.IsVisible = needBrands;
      properties.ProductKind.IsVisible = needBrands && !isThirdParty;
      properties.Brands.IsVisible = needBrands && !isThirdParty;
      properties.RegulatoryDocument.IsVisible = needRegulatoryDocument;
      properties.SigningInfo.IsVisible = _obj.SigningInfo != null;
      
      properties.RightHolder.IsRequired = needBrands;
      properties.ProductKind.IsRequired = needBrands && !isThirdParty;
      properties.Brands.IsRequired = needBrands && !isThirdParty;
      properties.RegulatoryDocument.IsRequired = !_obj.State.IsInserted && needRegulatoryDocument;
    }
    
    /// <summary>
    /// Очистка стран для табличной части товарных знаков.
    /// </summary>
    /// <param name="idBrand">ИД строки.</param>
    /// <param name="forDelivery">Страны поставки.</param>
    /// <param name="forProduction">Страны производства.</param>
    public void ClearCountries(int idBrand, bool forDelivery, bool forProduction)
    {
      if (forDelivery)
      {
        while (_obj.CountriesOfDelivery.Any(c => c.IdBrand == idBrand))
          _obj.CountriesOfDelivery.Remove(_obj.CountriesOfDelivery.First(c => c.IdBrand == idBrand));
      }
      
      if (forProduction)
      {
        while (_obj.CountriesOfProduction.Any(c => c.IdBrand == idBrand))
          _obj.CountriesOfProduction.Remove(_obj.CountriesOfProduction.First(c => c.IdBrand == idBrand));
      }
    }
    
    public override void FillName()
    {
      var documentKind = _obj.DocumentKind;
      
      if (documentKind != null && !documentKind.GenerateDocumentName.Value && _obj.Name == Sungero.Docflow.Resources.DocumentNameAutotext)
        _obj.Name = string.Empty;
      
      if (documentKind == null || !documentKind.GenerateDocumentName.Value)
        return;
      
      var name = string.Empty;
      
      /* Имя в формате:
        <Вид документа> №<номер> от <дата> "<содержание>".
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += Sungero.Docflow.OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (_obj.RegistrationDate != null)
          name += Sungero.Docflow.OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
        
        if (!string.IsNullOrWhiteSpace(_obj.Subject))
          name += " " + _obj.Subject;
        
        if (_obj.RegulatoryDocument != null)
          name += string.Format(" \"{0}\"", _obj.RegulatoryDocument.Name);
      }
      
      if (string.IsNullOrWhiteSpace(name))
        name = Sungero.Docflow.Resources.DocumentNameAutotext;
      else if (documentKind != null)
        name = documentKind.ShortName + name;
      
      name = Sungero.Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Sungero.Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(name, _obj);
    }
  }
}