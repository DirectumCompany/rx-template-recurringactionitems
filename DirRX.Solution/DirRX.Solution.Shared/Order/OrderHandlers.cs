using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Order;

namespace DirRX.Solution
{
  partial class OrderBrandsSharedCollectionHandlers
  {

    public virtual void BrandsDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e)
    {
      Functions.Order.ClearCountries(_obj, _deleted.IdBrand.HasValue ? _deleted.IdBrand.Value : -1, true, true);
    }
  }


  partial class OrderSharedHandlers
  {

    public override void RegistrationDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.RegistrationDateChanged(e);

      e.Params.AddOrUpdate("ChangeRegistrationDateFlag", true);
    }

    public override void RegistrationNumberChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      base.RegistrationNumberChanged(e);
      
      e.Params.AddOrUpdate("ChangeRegistrationNumberFlag", true);
    }
    
    public virtual void RegulatoryDocumentChanged(DirRX.Solution.Shared.OrderRegulatoryDocumentChangedEventArgs e)
    {
      _obj.Relations.AddOrUpdate(LocalActs.PublicConstants.Module.RegulatoryOrderRelationName, e.OldValue, e.NewValue);
    }

    public virtual void RightHolderChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue == e.OldValue)
        return;
      
      if (e.OldValue == null || e.NewValue == Order.RightHolder.ThirdParty)
      {
        _obj.ProductKind = null;
        _obj.Brands.Clear();
        _obj.CountriesOfDelivery.Clear();
        _obj.CountriesOfProduction.Clear();
      }
      
      Functions.Order.SetVisibilityAndRequiredProperties(_obj);
    }

    public virtual void BPGroupChanged(DirRX.Solution.Shared.OrderBPGroupChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        if (e.NewValue != null)
        {
          var subjects = Functions.Order.Remote.GetOrderSubjects(e.NewValue);
          if (_obj.Theme != null && !subjects.Contains(_obj.Theme))
            _obj.Theme = null;
          
          if (e.NewValue.Owners.Count == 1 && _obj.StandardForm != null && _obj.StandardForm.IsBPOwner.GetValueOrDefault())
            _obj.Supervisor = Functions.Order.Remote.GetRoleEmployees(new List<IRole>() {e.NewValue.Owners.FirstOrDefault().Owner}).FirstOrDefault();
        }
        else
          _obj.Theme = null;
      }
    }

    public virtual void ThemeChanged(DirRX.Solution.Shared.OrderThemeChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        if (e.NewValue != null)
        {
          // Получить типовые формы.
          var standardForms = Functions.Order.Remote.GetStandardForms(_obj.Theme);
          if (standardForms.Count() == 1)
            _obj.StandardForm = standardForms.FirstOrDefault();
          else if (_obj.StandardForm != null && !standardForms.Contains(_obj.StandardForm))
            _obj.StandardForm = null;
          
          // Получить группы бизнесс-процессов.
          var groups = Functions.Order.Remote.GetBusinessProcessGroups(_obj.Theme);
          if (groups.Count() == 1)
            _obj.BPGroup = groups.FirstOrDefault();
        }
        else
          _obj.StandardForm = null;
      }
    }

    public virtual void StandardFormChanged(DirRX.Solution.Shared.OrderStandardFormChangedEventArgs e)
    {
      if (e.NewValue == null)
      {
        _obj.Supervisor = null;
        _obj.NeedTaxMonitoring = false;
      }
      else if (e.NewValue != e.OldValue)
      {
        _obj.NeedTaxMonitoring = e.NewValue.NeedTaxMonitoring;
        _obj.DocumentKind = e.NewValue.DocumentKind;
        if (!LocalActs.OrderSubjects.Equals(_obj.Theme, e.NewValue.Subject))
          _obj.Theme = e.NewValue.Subject;
        if (e.NewValue.Supervisor != null)
          _obj.Supervisor = Functions.Order.Remote.GetRoleEmployees(new List<IRole>() {e.NewValue.Supervisor}).FirstOrDefault();
        else if (e.NewValue.IsBPOwner == true && _obj.BPGroup != null && _obj.BPGroup.Owners.Count == 1)
          _obj.Supervisor = Functions.Order.Remote.GetRoleEmployees(new List<IRole>() {_obj.BPGroup.Owners.FirstOrDefault().Owner}).FirstOrDefault();
        else
          _obj.Supervisor = null;
        
        if (!e.NewValue.NeedCheckTrademarkRegistration.GetValueOrDefault())
        {
          _obj.RightHolder = null;
          _obj.ProductKind = null;
          _obj.Brands.Clear();
          _obj.CountriesOfDelivery.Clear();
          _obj.CountriesOfProduction.Clear();
        }
        
        var needRegulatoryDocument = _obj.StandardForm != null ? _obj.StandardForm.NeedRegulatoryDocument.GetValueOrDefault() : false;
        if (!needRegulatoryDocument && _obj.RegulatoryDocument != null)
          _obj.RegulatoryDocument = null;
        
        if (string.IsNullOrEmpty(_obj.Subject) || (!string.IsNullOrEmpty(e.NewValue.Content) && !_obj.Subject.Contains(e.NewValue.Content)))
          _obj.Subject = e.NewValue.Content;
      }
      
      Functions.Order.SetVisibilityAndRequiredProperties(_obj);
    }
  }
}