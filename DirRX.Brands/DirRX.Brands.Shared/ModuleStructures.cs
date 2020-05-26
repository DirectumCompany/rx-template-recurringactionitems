using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Brands.Structures.Module
{

  /// <summary>
  /// Регистрация товарного знака.
  /// </summary>
  partial class BrandRegistrData
  {
    /// <summary>
    /// Строка xlsx файла.
    /// </summary>
    public int Row { get; set; }
    
    /// <summary>
    /// Идентификатор товарного знака.
    /// </summary>
    public int BrandID { get; set; }
    
    /// <summary>
    /// Наименование товарного знака.
    /// </summary>
    public string BrandName { get; set; }

    /// <summary>
    /// № регистрации.
    /// </summary>
    public string RegistrNum { get; set; }
    
    /// <summary>
    /// Заголовок вида регистрации.
    /// </summary>
    public string RegistrKind { get; set; }
    
    /// <summary>
    /// Наименование страны.
    /// </summary>
    public string CountryName { get; set; }
    
    /// <summary>
    /// Класс МКТУ.
    /// </summary>
    public string ICGSClass { get; set; }
    
    /// <summary>
    /// Наименование товарной группы.
    /// </summary>
    public string ProductGroupName { get; set; }
    
    /// <summary>
    /// Дата регистрации.
    /// </summary>
    public DateTime? RegistrDate { get; set; }
    
    /// <summary>
    /// Действует до.
    /// </summary>
    public DateTime? ValidTill { get; set; }
    
    /// <summary>
    /// Заголовок статуса.
    /// </summary>
    public string RegistrStatus { get; set; }
    
    /// <summary>
    /// Оспаривание.
    /// </summary>
    public bool IsAppeal { get; set; }
    
    /// <summary>
    /// Примечание.
    /// </summary>
    public string Note { get; set; }
  }
  
  /// <summary>
  /// Ошибки создания регистраций товарных знаков.
  /// </summary>
  partial class BrandRegistrDataErrors
  {
    /// <summary>
    /// Строка xlsx файла.
    /// </summary>
    public int Row { get; set; }
    
    /// <summary>
    /// Идентификатор товарного знака.
    /// </summary>
    public int BrandID { get; set; }
    
    /// <summary>
    /// Товарный знак.
    /// </summary>
    public string BrandName { get; set; }
    
    /// <summary>
    /// Сообщение об ошибке.
    /// </summary>
    public string Message { get; set; }
  }

}