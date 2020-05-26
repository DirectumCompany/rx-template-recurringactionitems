using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ContractSettings;

namespace DirRX.ContractsCustom
{
  partial class ContractSettingsCategoryPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CategoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      // Фильтрация по дополнительным условиям.
      if (_obj.Subcategory != null)
        query = query.Where(x => x.CounterpartySubcategories.Any(
          cs => DirRX.ContractsCustom.ContractSubcategories.Equals(cs.Subcategories, _obj.Subcategory)));
      // Фильтрация по видам договора.
      if (_obj.DocumentKind != null)
        query = query.Where(x => x.DocumentKinds.Any(dk => Sungero.Docflow.DocumentKinds.Equals(dk.DocumentKind, _obj.DocumentKind)));
      
      return query;
    }
  }


  partial class ContractSettingsSubcategoryPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> SubcategoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      // Фильтрация по категории.
      if (_obj.Category != null)
      {
        var subcategories = _obj.Category.CounterpartySubcategories.Select(s => s.Subcategories).ToList();
        query = query.Where(x => subcategories.Contains(DirRX.ContractsCustom.ContractSubcategories.As(x)));
      }
      else
      {
        // Если категория не указана, то отфильтруем по виду.
        if (_obj.DocumentKind != null)
        { 
          var categories  = DirRX.ContractsCustom.Functions.ContractSettings.GetCategoriesByKind(_obj);
          query = query.Where(x => categories.Any(s => s.CounterpartySubcategories.Select(cs => cs.Subcategories).Contains(x)));
        }
      }
      
      return query;
    }
  }

  partial class ContractSettingsServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      /*if (!_obj.State.IsCopied)
        _obj.LukoilApproval = false;*/
      _obj.LukoilApproval = false;
      
      _obj.IsAnalysisRequired = false;
      _obj.ContractActivate = DirRX.ContractsCustom.ContractSettings.ContractActivate.Original;
      _obj.YearLabel = DirRX.ContractsCustom.ContractSettingses.Resources.YearLabelText;
    }
  }

  partial class ContractSettingsDocumentKindPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      // Фильтрация по типу документа.
      if (_obj.DocumentType != null)
        query = query.Where(x => Sungero.Docflow.DocumentTypes.Equals(x.DocumentType, _obj.DocumentType));
      // Фильтрация по видам документа категории.
      if (_obj.Category != null)
      {
        var documentKinds = _obj.Category.DocumentKinds.Select(s => s.DocumentKind).ToList();
        query = query.Where(x => documentKinds.Contains(Sungero.Docflow.DocumentKinds.As(x)));
      }
      
      return query.Where(x => x.DocumentType.DocumentFlow.Value == Sungero.Docflow.DocumentType.DocumentFlow.Contracts);
    }
  }

  partial class ContractSettingsTemplatePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> TemplateFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.DocumentType != null)
        query = query.Where(x => x.DocumentType.ToString() == _obj.DocumentType.DocumentTypeGuid);
      return query;
    }
  }

  partial class ContractSettingsDocumentTypePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentTypeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(x => x.DocumentFlow.Value == Sungero.Docflow.DocumentType.DocumentFlow.Contracts);
    }
  }

}