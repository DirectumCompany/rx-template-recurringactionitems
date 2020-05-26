using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Order;
using System.IO;

namespace DirRX.Solution.Server
{
  partial class OrderFunctions
  {

    #region Скопировано из стандартной. Простановка штампа подписи в PublicBody.
    
    /// <summary>
    /// Преобразовать документ в PDF с наложением отметки об ЭП.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="versionId">ID версии.</param>
    /// <param name="signatureMark">Строка с HTML разметкой.</param>
    /// <returns>Результат преобразования.</returns>
    private Sungero.Docflow.Structures.OfficialDocument.СonversionToPdfResult GeneratePublicBodyWithStamp(Sungero.Docflow.IOfficialDocument document, int versionId, string signatureMark)
    {
      var info = Sungero.Docflow.Structures.OfficialDocument.СonversionToPdfResult.Create();
      info.HasErrors = true;
      var version = document.Versions.SingleOrDefault(v => v.Id == versionId);
      if (version == null)
      {
        info.HasConvertionError = true;
        info.ErrorMessage = Sungero.Docflow.OfficialDocuments.Resources.NoVersionWithNumberErrorFormat(versionId);
        return info;
      }
      
      System.IO.Stream pdfDocumentStream = null;
      using (var inputStream = new System.IO.MemoryStream())
      {
        // Изменен источник для преобразования и штампа, в стандартной тело берется из Body.
        version.PublicBody.Read().CopyTo(inputStream);
        try
        {
          var pdfConverter = new Sungero.AsposeExtensions.Converter();
          var extension = version.BodyAssociatedApplication.Extension;
          pdfDocumentStream = pdfConverter.GeneratePdf(inputStream, extension);
          var htmlStampString = signatureMark;
          pdfDocumentStream = pdfConverter.AddSignatureMark(pdfDocumentStream, extension, htmlStampString, Sungero.Docflow.Resources.SignatureMarkAnchorSymbol,
                                                            Sungero.Docflow.Constants.Module.SearchablePagesLimit);
        }
        catch (Exception e)
        {
          if (e is Sungero.AsposeExtensions.PdfConvertException)
            Logger.Error(Sungero.Docflow.Resources.PdfConvertErrorFormat(document.Id), e.InnerException);
          else
            Logger.Error(string.Format("{0} {1}", Sungero.Docflow.Resources.PdfConvertErrorFormat(document.Id), e.Message));
          
          info.HasConvertionError = true;
          info.HasLockError = false;
          info.ErrorMessage = Sungero.Docflow.Resources.DocumentBodyNeedsRepair;
        }
      }
      
      if (!string.IsNullOrWhiteSpace(info.ErrorMessage))
        return info;
      
      version.PublicBody.Write(pdfDocumentStream);
      version.AssociatedApplication = Sungero.Content.AssociatedApplications.GetByExtension(Constants.Module.SignatureInfo.PdfDocumentExtension);
      pdfDocumentStream.Close();

      try
      {
        document.Save();
        info.HasErrors = false;
      }
      catch (Sungero.Domain.Shared.Exceptions.RepeatedLockException e)
      {
        Logger.Error(e.Message);
        info.HasConvertionError = false;
        info.HasLockError = true;
        info.ErrorMessage = e.Message;
      }
      catch (Exception e)
      {
        Logger.Error(e.Message);
        info.HasConvertionError = true;
        info.HasLockError = false;
        info.ErrorMessage = e.Message;
      }
      
      return info;
    }
    
    /// <summary>
    /// Преобразовать документ в PDF с наложением отметки об ЭП.
    /// </summary>
    /// <returns>Сообщение с результатом преобразования.</returns>
    [Remote]
    public string CustomConvertToPdfWithSignatureMark()
    {
      var versionId = _obj.LastVersion.Id;
      var info = base.ValidateDocumentBeforeConvertion(versionId);
      if (info.HasErrors || info.IsOnConvertion)
        return info.ErrorMessage;
      
      if (base.CanConvertToPdfInteractively())
      {
        var signatureMark = Sungero.Docflow.PublicFunctions.Module.GetSignatureMarkAsHtml(_obj, versionId);
        info = GeneratePublicBodyWithStamp(_obj, versionId, signatureMark);
      }
      else
        return Orders.Resources.CantConvertInteractively;
      
      if (info.HasErrors || info.IsOnConvertion)
        return info.ErrorMessage;
      
      return Sungero.Docflow.OfficialDocuments.Resources.ConvertionDone;
    }
    
    #endregion
    
    #region Проверка регистрации товарных знаков.
    
    /// <summary>
    /// Получить информацию о незарегистрированных товарных знаках.
    /// </summary>
    /// <returns>Коллекция структур с информацией.</returns>
    [Remote(IsPure = true)]
    public List<Structures.RecordManagement.Order.MissBrands> GetMissBrands()
    {
      var missBrands = new List<Structures.RecordManagement.Order.MissBrands>();
      
      // Зарегистрированные товарные знаки с учётом вхождения вида товара в товарные группы.
      AccessRights.AllowRead(
        () =>
        {
          var brandRegistrations = Brands.BrandsRegistrations.GetAll(r => r.Status == Brands.BrandsRegistration.Status.Registered).ToList()
            .Where(b => _obj.ProductKind.ProductGroups.Any(g => Brands.ProductGroups.Equals(g.ProductGroup, b.ProductGroup)));
          
          
          // Группировка регистраций товарных знаков для последующей обработке в цикле.
          var brandRegistrationGroups = brandRegistrations.GroupBy(g => g.Brand);
          
          foreach (var brand in _obj.Brands)
          {
            // Получение стран поставки и производства по идентификатору строки в табличной части.
            var countriesOfDelivery = _obj.CountriesOfDelivery.Where(c => c.IdBrand == brand.IdBrand).Select(i => i.Country).ToList();
            var countriesOfProduction = _obj.CountriesOfProduction.Where(c => c.IdBrand == brand.IdBrand).Select(i => i.Country).ToList();
            
            AddMissBrands(missBrands, brand.FirstName, brandRegistrationGroups, countriesOfDelivery, countriesOfProduction);
            AddMissBrands(missBrands, brand.SecondName, brandRegistrationGroups, countriesOfDelivery, countriesOfProduction);
            AddMissBrands(missBrands, brand.ThirdName, brandRegistrationGroups, countriesOfDelivery, countriesOfProduction);
            AddMissBrands(missBrands, brand.OtherName, brandRegistrationGroups, countriesOfDelivery, countriesOfProduction);
          }
        });
      
      return missBrands;
    }
    
    /// <summary>
    /// Заполнить информацию о незарегистрированных товарных знаках.
    /// </summary>
    /// <param name="missBrands">Список незарегистрированных товарных знаков.</param>
    /// <param name="workMark">Товарный знак.</param>
    /// <param name="brandRegistrationGroups">Сгруппированные данные о регистрации товарных знаков.</param>
    /// <param name="countriesOfDelivery">Страны поставки.</param>
    /// <param name="countriesOfProduction">Страны производства.</param>
    public void AddMissBrands(System.Collections.Generic.IList<Structures.RecordManagement.Order.MissBrands> missBrands,
                              Brands.IWordMark workMark,
                              System.Collections.Generic.IEnumerable<System.Linq.IGrouping<DirRX.Brands.IWordMark, DirRX.Brands.IBrandsRegistration>> brandRegistrationGroups,
                              System.Collections.Generic.IList<Solution.ICountry> countriesOfDelivery,
                              System.Collections.Generic.IList<Solution.ICountry> countriesOfProduction)
    {
      if (workMark != null && !workMark.IsUnregistered.GetValueOrDefault())
      {
        var markBrandRegistration = brandRegistrationGroups.FirstOrDefault(g => Brands.WordMarks.Equals(g.Key, workMark));
        
        // Если для товарного знака нет ни одной записи о регистрации, то заполняем все страны без проверки оспаривания, иначе фильтруем и проверяем признак перед записью.
        if (markBrandRegistration == null)
        {
          var missCountriesOfDelivery = new List<Structures.RecordManagement.Order.Countries>();
          foreach (var countryOfDelivery in countriesOfDelivery)
            missCountriesOfDelivery.Add(Structures.RecordManagement.Order.Countries.Create(string.Empty, countryOfDelivery, true, false));
          AddMissBrandInfo(missBrands, workMark, missCountriesOfDelivery, false);
          
          var missCountriesOfProduction = new List<Structures.RecordManagement.Order.Countries>();
          foreach (var countryOfProduction in countriesOfProduction)
            missCountriesOfProduction.Add(Structures.RecordManagement.Order.Countries.Create(string.Empty, countryOfProduction, false, true));
          AddMissBrandInfo(missBrands, workMark, missCountriesOfProduction, false);
        }
        else
        {
          AddMissBrandsAndAppeal(missBrands, workMark, markBrandRegistration.ToList(), countriesOfDelivery, true);
          AddMissBrandsAndAppeal(missBrands, workMark, markBrandRegistration.ToList(), countriesOfProduction, false);
        }
      }
    }
    
    /// <summary>
    /// Заполнить информацию о незарегистрированных товарных знаках с учётом признака оспаривания.
    /// </summary>
    /// <param name="missBrands">Список незарегистрированных товарных знаков.</param>
    /// <param name="workMark">Товарный знак.</param>
    /// <param name="barndRegistrations">Данные о регистрации товарных знаков.</param>
    /// <param name="countries">Проверяемые страны.</param>
    /// <param name="isDeliveryCountries">Признак того, что страны отностятся к странам поставки.</param>
    public void AddMissBrandsAndAppeal(System.Collections.Generic.IList<Structures.RecordManagement.Order.MissBrands> missBrands,
                                       Brands.IWordMark workMark,
                                       System.Collections.Generic.IList<Brands.IBrandsRegistration> barndRegistrations,
                                       System.Collections.Generic.IList<Solution.ICountry> countries,
                                       bool isDeliveryCountries)
    {
      // Проверка наличия стран в записях о регистрации, а также вхождение страны в группу стран, если последняя указана в регистрации для данного товарного знака.
      var missBrandsForCountries = countries.Where(c => !barndRegistrations.Any(r => Solution.Countries.Equals(r.Country, c) ||
                                                                                (r.Country.GroupFlag.GetValueOrDefault() &&
                                                                                 c.CountriesGroup.Any(gc => Solution.Countries.Equals(gc.GroupCountry, r.Country)))));
      if (missBrandsForCountries.Any())
      {
        var missBrandsForCountriesCollection = new List<Structures.RecordManagement.Order.Countries>();
        foreach (var missBrandsForCountry in missBrandsForCountries)
          missBrandsForCountriesCollection.Add(Structures.RecordManagement.Order.Countries.Create(string.Empty, missBrandsForCountry, isDeliveryCountries, !isDeliveryCountries));
        AddMissBrandInfo(missBrands, workMark, missBrandsForCountriesCollection, false);
      }
      
      // Отбор данных о регистрации с признаком "Оспаривание" для данного товарного знака.
      var appealBrandRegistrations = barndRegistrations.Where(r => r.IsAppeal.GetValueOrDefault() && countries.Any(c => Solution.Countries.Equals(r.Country, c)||
                                                                                                                   (r.Country.GroupFlag.GetValueOrDefault() &&
                                                                                                                    c.CountriesGroup.Any(gc => Solution.Countries.Equals(gc.GroupCountry, r.Country)))));
      if (appealBrandRegistrations.Any())
      {
        var appealBrandsForCountriesCollection = new List<Structures.RecordManagement.Order.Countries>();
        foreach (var appealBrandRegistration in appealBrandRegistrations)
          appealBrandsForCountriesCollection.Add(Structures.RecordManagement.Order.Countries.Create(appealBrandRegistration.RegistrationNumber,
                                                                                                    appealBrandRegistration.Country,
                                                                                                    isDeliveryCountries,
                                                                                                    !isDeliveryCountries));
        AddMissBrandInfo(missBrands, workMark, appealBrandsForCountriesCollection, true);
      }
    }
    
    /// <summary>
    /// Заполнить информацию о незарегистрированных товарных знаках.
    /// </summary>
    /// <param name="missBrands">Список незарегистрированных товарных знаков.</param>
    /// <param name="workMark">Товарный знак.</param>
    /// <param name="countries">Страны.</param>
    /// <param name="isAppeal">Признак оспаривания.</param>
    public void AddMissBrandInfo(System.Collections.Generic.IList<Structures.RecordManagement.Order.MissBrands> missBrands,
                                 Brands.IWordMark workMark,
                                 System.Collections.Generic.IList<Structures.RecordManagement.Order.Countries> countries,
                                 bool isAppeal)
    {
      // Если для товарного знака и определённого признака оспаривания нет записи, то создаём новую, иначе дополняем существующую.
      if (!missBrands.Any(b => Brands.WordMarks.Equals(b.WordMark, workMark) && b.IsAppeal == isAppeal))
      {
        missBrands.Add(Structures.RecordManagement.Order.MissBrands.Create(workMark, countries, isAppeal));
      }
      else
      {
        var missBrand = missBrands.FirstOrDefault(b => Brands.WordMarks.Equals(b.WordMark, workMark) && b.IsAppeal == isAppeal);
        foreach (var country in countries)
        {
          var missCountry = missBrand.Countries.FirstOrDefault(c => DirRX.Solution.Countries.Equals(c.Country, country.Country));
          if (missCountry == null)
            missBrand.Countries.Add(country);
          else
          {
            if (country.IsDelivery)
              missCountry.IsDelivery = true;
            if (country.IsProduction)
              missCountry.IsProduction = true;
          }
        }
      }
    }
    
    #endregion
    
    /// <summary>
    /// Получение списка типовых форм по теме.
    /// </summary>
    /// <param name="theme">Тема.</param>
    /// <returns>Типовая форма.</returns>
    [Remote(IsPure = true)]
    public static List<LocalActs.IStandardForm> GetStandardForms(LocalActs.IOrderSubject theme)
    {
      return LocalActs.StandardForms.GetAll(x => LocalActs.OrderSubjects.Equals(x.Subject, theme)).ToList();
    }
    
    /// <summary>
    /// Получение списка групп бизнес-процессов по теме.
    /// </summary>
    /// <param name="theme">Тема.</param>
    /// <returns>Список групп бизнес-процессов.</returns>
    [Remote(IsPure = true)]
    public static List<LocalActs.IBusinessProcessGroup> GetBusinessProcessGroups(LocalActs.IOrderSubject theme)
    {
      return LocalActs.BusinessProcessGroups.GetAll(x => x.OrderSubjects.Any(s => LocalActs.OrderSubjects.Equals(s.OrderSubject, theme))).ToList();
    }
    
    /// <summary>
    /// Получение списка тем по группе бизнес-процесса.
    /// </summary>
    /// <param name="group">Группа бизнес-процесса.</param>
    /// <returns>Список тем.</returns>
    [Remote(IsPure = true)]
    public static List<LocalActs.IOrderSubject> GetOrderSubjects(LocalActs.IBusinessProcessGroup group)
    {
      return group.OrderSubjects.Select(s => s.OrderSubject).ToList();
    }
    
    /// <summary>
    /// Получить исполнителей по ролям.
    /// </summary>
    /// <param name="roles">Роли.</param>
    /// <returns>Сотрудники.</returns>
    [Remote(IsPure = true)]
    public static List<Solution.IEmployee> GetRoleEmployees(List<IRole> roles)
    {
      List<IEmployee> members = new List<IEmployee>();
      foreach (var role in roles)
        members.AddRange(role.RecipientLinks.Select(r => Employees.As(r.Member)).Where(m => m != null).ToList());
      return members;
    }
    
    /// <summary>
    /// Получение коллекции действующих записей справочника стран, буз групп стран.
    /// </summary>
    /// <param name="selectedCountries">Исключаемые страны.</param>
    /// <returns>Список стран.</returns>
    [Remote(IsPure = true)]
    public static List<Solution.ICountry> GetCountries(List<DirRX.Solution.ICountry> selectedCountries)
    {
      return DirRX.Solution.Countries.GetAll().ToList().Where(c => c.Status == DirRX.Solution.Country.Status.Active &&
                                                              !c.GroupFlag.GetValueOrDefault() &&
                                                              !selectedCountries.Contains(c)).ToList();
    }
  }
}