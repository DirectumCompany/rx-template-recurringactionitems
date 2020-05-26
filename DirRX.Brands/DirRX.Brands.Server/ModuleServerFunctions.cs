using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Brands.Server
{
  public class ModuleFunctions
  {

    /// <summary>
    /// Проверить записи регистраций товарных знаков.
    /// </summary>
    /// <param name="registrList">Список структур регистраций товарных знаков.</param>
    /// <returns>Список структур регистраций товарных знаков с ошибками.</returns>
    [Remote]
    public List<Brands.Structures.Module.BrandRegistrDataErrors> CheckAndCreateBrandRegistrDataFromExcel(List<Brands.Structures.Module.BrandRegistrData> registrList)
    {
      // Проверить полученное содержимое и сформировать список ошибок.
      var checkingErrors = CheckBrandRegistrDataFromExcel(registrList);
      
      // Получить корректные данные, которые можно загружать.
      var errorRows = checkingErrors.Select(r => r.Row);
      var correctRegistrList = registrList.FindAll(r => !errorRows.Contains(r.Row));
      
      // Загрузить данные, при наличии ошибок, добавить их в список.
      var creatingErrors = CreateBrandRegistrDataFromExcel(correctRegistrList);
      checkingErrors.AddRange(creatingErrors);
                
      if (checkingErrors.Any())
      {
        var textErrors = new System.Text.StringBuilder();
        foreach (var error in checkingErrors)
          textErrors.Append(string.Format("В строке {0} обнаружены следующие ошибки:{1}{2}", error.Row, Environment.NewLine, error.Message));
        
        var role = Roles.GetAll(x => x.Sid == Constants.Module.IntellectualPropertySpecialistRoleGuid).FirstOrDefault();
        var notice = Sungero.Workflow.SimpleTasks.CreateWithNotices("Список ошибок при загрузке товарных знаков", new[] { role });
        notice.ActiveText = textErrors.ToString();
        notice.Save();
        notice.Start();
      }
      
      return checkingErrors;
    }
    
    /// <summary>
    /// Проверить записи регистраций товарных знаков.
    /// </summary>
    /// <param name="registrList">Список структур регистраций товарных знаков.</param>
    /// <returns>Список структур регистраций товарных знаков с ошибками.</returns>
    private List<Brands.Structures.Module.BrandRegistrDataErrors> CheckBrandRegistrDataFromExcel(List<Brands.Structures.Module.BrandRegistrData> registrList)
    {
      List<Brands.Structures.Module.BrandRegistrDataErrors> errors = new List<DirRX.Brands.Structures.Module.BrandRegistrDataErrors>();
      
      foreach (Brands.Structures.Module.BrandRegistrData registr in registrList)
      {
        string rowMessage = string.Empty;
        
        #region Товарного знак.
        if (string.IsNullOrEmpty(registr.BrandName))
          rowMessage = string.Format("{0}Не заполнено наименование товарного знака;{1}", rowMessage, Environment.NewLine);
        else
        {
          var brand = WordMarks.GetAll(b => b.Name == registr.BrandName).FirstOrDefault();
          if (brand == null)
            rowMessage = string.Format("{0}Товарный знак {2} отсутствует в справочнике;{1}", rowMessage, Environment.NewLine, registr.BrandName);
        }
        #endregion
        
        #region Номер регистрации.
        if (string.IsNullOrEmpty(registr.RegistrNum))
          rowMessage = string.Format("{0}Не заполнен номер регистрации товарного знака;{1}", rowMessage, Environment.NewLine);
        #endregion
        
        #region Вид регистрации.
        if (string.IsNullOrEmpty(registr.RegistrKind))
          rowMessage = string.Format("{0}Не заполнен вид регистрации товарного знака;{1}", rowMessage, Environment.NewLine);
        else
        {
          bool isRegistrKind = (registr.RegistrKind == Brands.BrandsRegistrations.Info.Properties.RegistrationKind.GetLocalizedValue(Brands.BrandsRegistration.RegistrationKind.International).ToUpper() ||
                                registr.RegistrKind == Brands.BrandsRegistrations.Info.Properties.RegistrationKind.GetLocalizedValue(Brands.BrandsRegistration.RegistrationKind.National).ToUpper() ||
                                registr.RegistrKind == Brands.BrandsRegistrations.Info.Properties.RegistrationKind.GetLocalizedValue(Brands.BrandsRegistration.RegistrationKind.Regional).ToUpper());

          if (!isRegistrKind)
            rowMessage = string.Format("{0}Вид регистрации товарного знака {2} не найден;{1}", rowMessage, Environment.NewLine, registr.RegistrKind);
        }
        #endregion
        
        #region Страна.
        if (string.IsNullOrEmpty(registr.CountryName))
          rowMessage = string.Format("{0}Не заполнена страна товарного знака;{1}", rowMessage, Environment.NewLine);
        else
        {
          var country = DirRX.Solution.Countries.GetAll(c => c.Name == registr.CountryName.Trim()).FirstOrDefault();
          if (country == null)
            rowMessage = string.Format("{0}Страна {2} отсутствует в справочнике;{1}", rowMessage, Environment.NewLine, registr.CountryName);
        }
        #endregion
        
        #region Класс МКТУ.
        if (string.IsNullOrEmpty(registr.ICGSClass))
          rowMessage = string.Format("{0}Не заполнен класс МКТУ товарного знака;{1}", rowMessage, Environment.NewLine);
        #endregion
        
        #region Товарная группа.
        if (string.IsNullOrEmpty(registr.ProductGroupName))
          rowMessage = string.Format("{0}Не заполнена товарная группа товарного знака;{1}", rowMessage, Environment.NewLine);
        else
        {
          var productGroup = ProductGroups.GetAll(g => g.Name == registr.ProductGroupName.Trim()).FirstOrDefault();
          if (productGroup == null)
            rowMessage = string.Format("{0}Товарная группа {2} отсутствует в справочнике;{1}", rowMessage, Environment.NewLine, registr.ProductGroupName);
        }
        #endregion
        
        #region Статус.
        bool needDates = false;
        if (string.IsNullOrEmpty(registr.RegistrStatus))
          rowMessage = string.Format("{0}Не заполнен статус товарного знака;{1}", rowMessage, Environment.NewLine);
        else
        {
          bool isStatus = (registr.RegistrStatus == Brands.BrandsRegistrations.Info.Properties.Status.GetLocalizedValue(Brands.BrandsRegistration.Status.Determine).ToUpper() ||
                           registr.RegistrStatus == Brands.BrandsRegistrations.Info.Properties.Status.GetLocalizedValue(Brands.BrandsRegistration.Status.Overdue).ToUpper() ||
                           registr.RegistrStatus == Brands.BrandsRegistrations.Info.Properties.Status.GetLocalizedValue(Brands.BrandsRegistration.Status.Refused).ToUpper() ||
                           registr.RegistrStatus == Brands.BrandsRegistrations.Info.Properties.Status.GetLocalizedValue(Brands.BrandsRegistration.Status.Registered).ToUpper() ||
                           registr.RegistrStatus == Brands.BrandsRegistrations.Info.Properties.Status.GetLocalizedValue(Brands.BrandsRegistration.Status.Request).ToUpper());

          if (!isStatus)
            rowMessage = string.Format("{0}Статус товарного знака {2} не найден;{1}", rowMessage, Environment.NewLine, registr.RegistrStatus);
          
          needDates = (registr.RegistrStatus == Brands.BrandsRegistrations.Info.Properties.Status.GetLocalizedValue(Brands.BrandsRegistration.Status.Registered).ToUpper() ||
                       registr.RegistrStatus == Brands.BrandsRegistrations.Info.Properties.Status.GetLocalizedValue(Brands.BrandsRegistration.Status.Overdue).ToUpper());
        }
        #endregion
        
        if (needDates)
        {
          #region Дата регистрации.
          if (registr.RegistrDate == null)
            rowMessage = string.Format("{0}Не заполнена дата регистрации товарного знака;{1}", rowMessage, Environment.NewLine);
          #endregion
          
          #region Дата окончания.
          if (registr.ValidTill == null)
            rowMessage = string.Format("{0}Не заполнена дата окончания действия товарного знака;{1}", rowMessage, Environment.NewLine);
          #endregion
        }
        
        if (!string.IsNullOrEmpty(rowMessage))
        {
          Brands.Structures.Module.BrandRegistrDataErrors error = new DirRX.Brands.Structures.Module.BrandRegistrDataErrors();
          error.Row = registr.Row;
          error.Message = rowMessage;
          error.BrandName = registr.BrandName;
          
          if (registr.BrandID > 0)
            error.BrandID = registr.BrandID;
          
          errors.Add(error);
        }
      }
      
      return errors;
    }

    /// <summary>
    /// Создать записи регистраций товарных знаков.
    /// </summary>
    /// <param name="registrList">Список структур регистраций товарных знаков.</param>
    /// <returns>Список структур регистраций товарных знаков с ошибками.</returns>
    private List<Brands.Structures.Module.BrandRegistrDataErrors> CreateBrandRegistrDataFromExcel(List<Brands.Structures.Module.BrandRegistrData> registrList)
    {
      List<Brands.Structures.Module.BrandRegistrDataErrors> errors = new List<DirRX.Brands.Structures.Module.BrandRegistrDataErrors>();
      
      // Чтение локализованных значений перечислений.
      string registrationKindInternational = Brands.BrandsRegistrations.Info.Properties.RegistrationKind.GetLocalizedValue(Brands.BrandsRegistration.RegistrationKind.International).ToUpper();
      string registrationKindNational = Brands.BrandsRegistrations.Info.Properties.RegistrationKind.GetLocalizedValue(Brands.BrandsRegistration.RegistrationKind.National).ToUpper();
      string registrationKindRegional = Brands.BrandsRegistrations.Info.Properties.RegistrationKind.GetLocalizedValue(Brands.BrandsRegistration.RegistrationKind.Regional).ToUpper();
      string statusDetermine = Brands.BrandsRegistrations.Info.Properties.Status.GetLocalizedValue(Brands.BrandsRegistration.Status.Determine).ToUpper();
      string statusOverdue = Brands.BrandsRegistrations.Info.Properties.Status.GetLocalizedValue(Brands.BrandsRegistration.Status.Overdue).ToUpper();
      string statusRefused = Brands.BrandsRegistrations.Info.Properties.Status.GetLocalizedValue(Brands.BrandsRegistration.Status.Refused).ToUpper();
      string statusRegistered = Brands.BrandsRegistrations.Info.Properties.Status.GetLocalizedValue(Brands.BrandsRegistration.Status.Registered).ToUpper();
      string statusRequest = Brands.BrandsRegistrations.Info.Properties.Status.GetLocalizedValue(Brands.BrandsRegistration.Status.Request).ToUpper();
      
      foreach (Brands.Structures.Module.BrandRegistrData registr in registrList)
      {
        try
        {
          var brandRegistr = BrandsRegistrations.GetAll(b => b.Id == registr.BrandID).FirstOrDefault();
          if (brandRegistr == null)
            brandRegistr = BrandsRegistrations.Create();
          
          brandRegistr.RegistrationNumber = registr.RegistrNum;
          brandRegistr.ICGSClass = registr.ICGSClass;
          brandRegistr.RegistrationDate = registr.RegistrDate;
          brandRegistr.ValidUntil = registr.ValidTill;
          brandRegistr.IsAppeal = registr.IsAppeal;
          
          if (registr.Note.Length > brandRegistr.Info.Properties.Note.Length)
            registr.Note = registr.Note.Substring(0, brandRegistr.Info.Properties.Note.Length);
          
          brandRegistr.Note = registr.Note;
          
          var brand = WordMarks.GetAll(b => b.Name == registr.BrandName).FirstOrDefault();
          brandRegistr.Brand = brand;
          
          var country = DirRX.Solution.Countries.GetAll(c => c.Name == registr.CountryName.Trim()).FirstOrDefault();
          brandRegistr.Country = country;
          
          var productGroup = ProductGroups.GetAll(g => g.Name == registr.ProductGroupName.Trim()).FirstOrDefault();
          brandRegistr.ProductGroup = productGroup;
          
          if (registr.RegistrKind == registrationKindInternational)
            brandRegistr.RegistrationKind = Brands.BrandsRegistration.RegistrationKind.International;
          if (registr.RegistrKind == registrationKindNational)
            brandRegistr.RegistrationKind = Brands.BrandsRegistration.RegistrationKind.National;
          if (registr.RegistrKind == registrationKindRegional)
            brandRegistr.RegistrationKind = Brands.BrandsRegistration.RegistrationKind.Regional;
          
          if (registr.RegistrStatus == statusDetermine)
            brandRegistr.Status = Brands.BrandsRegistration.Status.Determine;
          if (registr.RegistrStatus == statusOverdue)
            brandRegistr.Status = Brands.BrandsRegistration.Status.Overdue;
          if (registr.RegistrStatus == statusRefused)
            brandRegistr.Status = Brands.BrandsRegistration.Status.Refused;
          if (registr.RegistrStatus == statusRegistered)
            brandRegistr.Status = Brands.BrandsRegistration.Status.Registered;
          if (registr.RegistrStatus == statusRequest)
            brandRegistr.Status = Brands.BrandsRegistration.Status.Request;

          brandRegistr.Save();
        }
        catch (Exception ex)
        {
          Brands.Structures.Module.BrandRegistrDataErrors error = new DirRX.Brands.Structures.Module.BrandRegistrDataErrors();
          error.Row = registr.Row;
          error.Message = ex.Message;
          error.BrandName = registr.BrandName;
          
          if (registr.BrandID > 0)
            error.BrandID = registr.BrandID;
          
          errors.Add(error);
        }
      }
      
      return errors;
    }
  }
}