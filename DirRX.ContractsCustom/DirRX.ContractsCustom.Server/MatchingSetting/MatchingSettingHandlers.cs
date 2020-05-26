using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.MatchingSetting;

namespace DirRX.ContractsCustom
{
	partial class MatchingSettingDocumentKindPropertyFilteringServerHandler<T>
	{

		public virtual IQueryable<T> DocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
		{
			query = query.Where(x => x.DocumentFlow == DirRX.Solution.DocumentKind.DocumentFlow.Contracts);
			return query;
		}
	}

	partial class MatchingSettingServerHandlers
	{

		public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
		{
			var existSettings = Functions.MatchingSetting.GetDoubles(_obj);
			if (existSettings.Any())
				e.AddError(Sungero.Docflow.ApprovalRuleBases.Resources.DuplicateDetected, _obj.Info.Actions.ShowDuplicate);
			
			_obj.Name = _obj.DocumentKind.DisplayValue;
			if (_obj.DocumentGroup != null)
				_obj.Name = string.Format("{0}. {1}", _obj.Name, _obj.DocumentGroup.DisplayValue);
			if (_obj.ContractSubcategory != null)
				_obj.Name = string.Format("{0}. {1}", _obj.Name, _obj.ContractSubcategory.DisplayValue);
			// Обрезать наименование, если больше допустимых символов.
			if (_obj.Name.Length > _obj.Info.Properties.Name.Length)
        _obj.Name = _obj.Name.Substring(0, _obj.Info.Properties.Name.Length); 
		}
	}

	partial class MatchingSettingContractSubcategoryPropertyFilteringServerHandler<T>
	{

		public virtual IQueryable<T> ContractSubcategoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
		{
			var subcategories = new List<DirRX.ContractsCustom.IContractSubcategory>();
			if (_obj.DocumentGroup != null)
				subcategories = _obj.DocumentGroup.CounterpartySubcategories.Select(s => s.Subcategories).ToList();

			return query.Where(s => subcategories.Contains(s));
		}
	}

	partial class MatchingSettingDocumentGroupPropertyFilteringServerHandler<T>
	{

		public virtual IQueryable<T> DocumentGroupFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
		{
			if (_obj.DocumentKind != null)
				query = query.Where(x => x.DocumentKinds.Any(y => Equals(y.DocumentKind, _obj.DocumentKind)));
			return query;
		}
	}

}