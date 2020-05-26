using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Order;

namespace DirRX.Solution.Client
{

  partial class OrderBrandsActions
  {
    public virtual void ChangeBrandsItem(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      Functions.Order.AddOrChangeBrand(Solution.Orders.As(_obj.RootEntity), _obj);
    }

    public virtual bool CanChangeBrandsItem(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return _obj.IdBrand.HasValue && _obj.Order.InternalApprovalState != Order.InternalApprovalState.Signed;
    }

    public virtual bool CanAddCountryOfDelivery(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return true;
    }

    public virtual void AddCountryOfDelivery(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var order = Solution.Orders.As(e.Entity);
      var countriesList = DirRX.Solution.Functions.Order.Remote.GetCountries(order.CountriesOfDelivery.Select(c => c.Country).ToList());
      var countries = countriesList.ShowSelectMany();
      
      if (countries.Any())
      {        
        // Формирование текстового отображения стран поставки для визуальной табличной части и заполнение соответствующей скрытой коллекции.
        foreach (var countryOfDelivery in countries)
        {
          var countryOfDeliveryItem = order.CountriesOfDelivery.AddNew();
          countryOfDeliveryItem.IdBrand = _obj.IdBrand;
          countryOfDeliveryItem.Country = countryOfDelivery;
        }
        
        if (_obj.CountryOfDelivery != null && _obj.CountryOfDelivery.Any())
          _obj.CountryOfDelivery += "; ";
        _obj.CountryOfDelivery += string.Join("; ", countries.Select(c => c.Name)).Trim();
      }
      
    }

    public virtual bool CanAddCountryOfProduction(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return true;
    }

    public virtual void AddCountryOfProduction(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var order = Solution.Orders.As(e.Entity);
      var countriesList = DirRX.Solution.Functions.Order.Remote.GetCountries(order.CountriesOfProduction.Select(c => c.Country).ToList());
      var countries = countriesList.ShowSelectMany();
      
      if (countries.Any())
      {
        // Формирование текстового отображения стран производства для визуальной табличной части и заполнение соответствующей скрытой коллекции.
        foreach (var countryOfProduction in countries)
        {
          var countryOfProductionItem = order.CountriesOfProduction.AddNew();
          countryOfProductionItem.IdBrand = _obj.IdBrand;
          countryOfProductionItem.Country = countryOfProduction;
        }
        
        if (_obj.CountryOfProduction != null && _obj.CountryOfProduction.Any())
          _obj.CountryOfProduction += "; ";
        _obj.CountryOfProduction += string.Join("; ", countries.Select(c => c.Name)).Trim();
      }
      
    }
  }

  partial class OrderAnyChildEntityActions
  {
    public override void AddChildEntity(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var brands = _all as Sungero.Domain.Shared.IChildEntityCollection<DirRX.Solution.IOrderBrands>;
      if (brands != null)
      {
        Functions.Order.AddOrChangeBrand(Solution.Orders.As(e.Entity), null);
        return;
      }
      
      base.AddChildEntity(e);
    }

    public override bool CanAddChildEntity(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return base.CanAddChildEntity(e);
    }

  }

  partial class OrderActions
  {


    public virtual void ConvertAndSetStamp(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      string message = Functions.Order.Remote.CustomConvertToPdfWithSignatureMark(_obj);
      
      if (message == Sungero.Docflow.OfficialDocuments.Resources.ConvertionDone)
        Dialogs.ShowMessage(message, MessageType.Information);
      else
      {
        string title = Sungero.Docflow.OfficialDocuments.Resources.ConvertionErrorTitleBase;
        Dialogs.ShowMessage(title, message, MessageType.Information);
      }
    }

    public virtual bool CanConvertAndSetStamp(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanConvertToPdf(e);
    }


    public override void SendForApproval(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.SendForApproval(e);
    }

    public override bool CanSendForApproval(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSendForApproval(e) && !_obj.State.IsInserted && !_obj.State.IsChanged;
    }

    public virtual void CheckBrandRegistration(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var report = DirRX.Solution.Reports.GetBrandRegistrationReport();
      report.Order = _obj;
      report.Open();
    }

    public virtual bool CanCheckBrandRegistration(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return (_obj.RightHolder == RightHolder.Lukoil || _obj.RightHolder == RightHolder.Unknow) && !_obj.State.IsInserted;
    }



    public override void SaveAndClose(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      this.Save(e);
      base.SaveAndClose(e);
    }

    public override bool CanSaveAndClose(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSaveAndClose(e);
    }

    public override void Save(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      #region Проверка регистрации товарных знаков.
      
      if (e.Validate())
      {
        if (!Functions.Order.CheckBrandsRegistration(_obj, e))
          return;
      }
      
      #endregion
      
      base.Save(e);
    }

    public override bool CanSave(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSave(e);
    }
    
    public override void Register(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Проверить заполненность даты начала в регламентирующем документе.
      if (_obj.RegulatoryDocument != null)
      {
        var regulatoryDocument = LocalActs.PublicFunctions.RegulatoryDocument.Remote.GetRegulatoryDocument(_obj.RegulatoryDocument.Id);
        if (regulatoryDocument != null && !regulatoryDocument.StartDate.HasValue)
        {
          e.AddError(LocalActs.RegulatoryDocuments.Resources.StartDateMustBeFilled);
          return;
        }
      }
      
      // Проверить заполненность даты отмены в отменяемом приказе.
      var revokeDocuments = _obj.Relations.GetRelatedFrom(LocalActs.PublicConstants.Module.RegulatoryNewEditionRelationName).Where(d => DirRX.Solution.Orders.Is(d)).ToList();
      foreach (var revokeDocument in revokeDocuments)
      {
        var order = DirRX.Solution.Orders.As(revokeDocument);
        if (!order.RevokeDate.HasValue)
        {
          e.AddError(LocalActs.RegulatoryDocuments.Resources.EndDateMustBeFilled);
          return;
        }
      }
      
      base.Register(e);
    }

    public override bool CanRegister(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanRegister(e);
    }

    public override void CreateFromTemplate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      FromTemplate(e);
    }

    public override bool CanCreateFromTemplate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return (base.CanCreateFromTemplate(e) && _obj.StandardForm != null && _obj.StandardForm.Template != null &&
              _obj.Name != null && _obj.BPGroup != null && _obj.Theme != null && _obj.Subject != null && _obj.BusinessUnit != null &&
              _obj.Department != null && _obj.PreparedBy != null);
    }

    public virtual void FromTemplate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (e.Validate())
      {
        if (!Functions.Order.CheckBrandsRegistration(_obj, e))
          return;
      }
      
      var template = _obj.StandardForm.Template;
      using (var body = template.LastVersion.Body.Read())
      {
        var newVersion = _obj.CreateVersionFrom(body, template.AssociatedApplication.Extension);
        
        var exEntity = (Sungero.Domain.Shared.IExtendedEntity)_obj;
        exEntity.Params[Sungero.Content.Shared.ElectronicDocumentUtils.FromTemplateIdKey] = template.Id;

        _obj.Save();
        _obj.Edit();
      }
    }

    public virtual bool CanFromTemplate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return (base.CanCreateFromTemplate(e) && _obj.StandardForm != null && _obj.StandardForm.Template != null &&
              _obj.Name != null && _obj.BPGroup != null && _obj.Theme != null && _obj.Subject != null && _obj.BusinessUnit != null &&
              _obj.Department != null && _obj.PreparedBy != null);
    }

  }

}