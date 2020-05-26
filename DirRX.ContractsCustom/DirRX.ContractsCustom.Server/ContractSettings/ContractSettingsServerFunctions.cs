using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ContractSettings;

namespace DirRX.ContractsCustom.Server
{
  partial class ContractSettingsFunctions
  {
    /// <summary>
    /// Получить настройку по параметрам.
    /// </summary>
    /// <param name="isStandard">Признак "Типовой".</param>
    /// <param name="documentTypeGuid">Гуид типа документа.</param>
    /// <param name="documentKind">Вид документа.</param>
    /// <param name="category">Категория.</param>
    /// <param name="subcategory">Дополнительные условия.</param>
    /// <param name="isLukoilGroup">Контрагент - организация Группы "ЛУКОЙЛ".</param>
    /// <returns>Настройка.</returns>
    [Public, Remote(IsPure = true)]
    public static IContractSettings GetContractSetting(bool isStandard,
                                                       string documentTypeGuid,
                                                       Sungero.Docflow.IDocumentKind documentKind,
                                                       Sungero.Docflow.IDocumentGroupBase category,
                                                       DirRX.ContractsCustom.IContractSubcategory subcategory,
                                                       bool isLukoilGroup)
    {
      return ContractSettingses.GetAll().Where(s => s.FormType == null || isStandard && s.FormType == ContractsCustom.ContractSettings.FormType.Typical ||
                                               !isStandard && s.FormType == ContractsCustom.ContractSettings.FormType.Recommended)
        .Where(s => s.DocumentType.DocumentTypeGuid == documentTypeGuid)
        .Where(s => Sungero.Docflow.DocumentKinds.Equals(s.DocumentKind, documentKind))
        .Where(s => s.Category == null || Sungero.Contracts.ContractCategories.Equals(s.Category, category))
        .Where(s => s.Subcategory == null || DirRX.ContractsCustom.ContractSubcategories.Equals(s.Subcategory, subcategory))
        .Where(s => !s.LukoilGroupCompany.HasValue ||
               isLukoilGroup == (s.LukoilGroupCompany.Value == DirRX.ContractsCustom.ContractSettings.LukoilGroupCompany.IsLukoil))
        .Where(s => s.Status == DirRX.ContractsCustom.ContractSettings.Status.Active)
        .ToList()
        .OrderByDescending(s => s.FormType.HasValue)
        .ThenBy(s => s.Category == null)
        .ThenBy(s => s.Subcategory == null)
        .ThenByDescending(s => s.LukoilGroupCompany.HasValue)
        .FirstOrDefault();
    }
    /// <summary>
    /// Получить все категории по виду договора.
    /// </summary>
    /// <returns>Категории договора.</returns>
    [Remote(IsPure = true)]
    public IQueryable<DirRX.Solution.IContractCategory> GetCategoriesByKind()
    {
      return DirRX.Solution.ContractCategories.GetAll(x => x.DocumentKinds.Any(
        dk => Sungero.Docflow.DocumentKinds.Equals(dk.DocumentKind, _obj.DocumentKind)));
    }

    /// <summary>
    /// Получить все виды договора по типу договора.
    /// </summary>
    /// <returns>Виды договора.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<Sungero.Docflow.IDocumentKind> GetDocumentKindsByType(Sungero.Docflow.IDocumentType docType)
    {
      return Sungero.Docflow.DocumentKinds.GetAll(x => Sungero.Docflow.DocumentTypes.Equals(x.DocumentType, docType));
    }

    /// <summary>
    /// Получить все категории договора, в которых присутствует дополнительное условие сущности.
    /// </summary>
    /// <returns>Категории договора.</returns>
    [Remote(IsPure = true)]
    public IQueryable<DirRX.Solution.IContractCategory> GetCategoriesBySubcategory()
    {
      return DirRX.Solution.ContractCategories.GetAll(x => x.CounterpartySubcategories.Any(
        cs => DirRX.ContractsCustom.ContractSubcategories.Equals(cs.Subcategories, _obj.Subcategory)));
    }
  }
}