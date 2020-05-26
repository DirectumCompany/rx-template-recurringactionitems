using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ContractSettings;

namespace DirRX.ContractsCustom
{
  partial class ContractSettingsBindingDocumentConditionSharedCollectionHandlers
  {

    public virtual void BindingDocumentConditionAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.DocumentsForTender = false;
    }
  }

  partial class ContractSettingsBindingDocumentSharedCollectionHandlers
  {

    public virtual void BindingDocumentAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.DocumentsForTender = false;
    }
  }

  partial class ContractSettingsSharedHandlers
  {

    public virtual void IsAnalysisRequiredChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      Functions.ContractSettings.SetStateProperties(_obj);
      
      if (e.NewValue == false)
      {
        _obj.ContractTermAnalysisRequired = null;
        _obj.TransactionAmountAnalysisRequired = null;
        _obj.CurrencyAnalysisRequired = null;
      }
    }

    public virtual void DocumentTypeChanged(DirRX.ContractsCustom.Shared.ContractSettingsDocumentTypeChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue && e.NewValue != null)
      {
        var documentKinds = DirRX.ContractsCustom.Functions.ContractSettings.Remote.GetDocumentKindsByType(e.NewValue);
        // Если есть только один вид для выбранного типа договора, то он подставляется.
        if (documentKinds.Count() == 1)
          _obj.DocumentKind = documentKinds.FirstOrDefault();
        else
        {
          if (_obj.DocumentKind != null && !Sungero.Docflow.DocumentTypes.Equals(_obj.DocumentKind.DocumentType, e.NewValue))
            _obj.DocumentKind = null;
        }
      }
    }

    public virtual void CategoryChanged(DirRX.ContractsCustom.Shared.ContractSettingsCategoryChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue && e.NewValue != null && _obj.DocumentKind == null)
      {
        var documentKinds = e.NewValue.DocumentKinds.Select(dk => dk.DocumentKind).ToList();
        if (documentKinds.Count == 1)
          _obj.DocumentKind = documentKinds.FirstOrDefault();
      }
    }

    public virtual void DocumentKindChanged(DirRX.ContractsCustom.Shared.ContractSettingsDocumentKindChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue && e.NewValue != null && _obj.DocumentType == null)
        _obj.DocumentType = e.NewValue.DocumentType;
    }

    public virtual void SubcategoryChanged(DirRX.ContractsCustom.Shared.ContractSettingsSubcategoryChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue && _obj.Category == null)
      {
        var categories = DirRX.ContractsCustom.Functions.ContractSettings.Remote.GetCategoriesBySubcategory(_obj);
        if (categories.Count() == 1)
          _obj.Category = categories.FirstOrDefault();
      }
    }

    public virtual void ContractTermChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
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

    public virtual void LukoilApprovalChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      // Установить видимость полей группы "Согласование с ПАО "Лукойл".
      Functions.ContractSettings.SetStateProperties(_obj);
      
      if (e.NewValue == false)
      {
        _obj.ContractTerm = null;
        _obj.TransactionAmount = null;
        _obj.Currency = null;
      }
    }

    public virtual void FormTypeChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      _obj.State.Properties.Template.IsRequired = e.NewValue.HasValue;
    }

  }
}