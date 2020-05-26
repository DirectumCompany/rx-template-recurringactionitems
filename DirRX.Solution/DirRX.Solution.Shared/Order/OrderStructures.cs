using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Solution.Structures.RecordManagement.Order
{
  /// <summary>
  /// Информация об отсутствии регистрации товарного знака.
  /// </summary>
  partial class MissBrands
  {
    /// <summary>
    /// Товарный знак.
    /// </summary>
    public DirRX.Brands.IWordMark WordMark { get; set; }
    
    /// <summary>
    /// Страны для которых нет информации о регистрации или есть процесс оспаривания.
    /// </summary>
    public System.Collections.Generic.IList<DirRX.Solution.Structures.RecordManagement.Order.Countries> Countries { get; set; }
    
    /// <summary>
    /// Признак оспаривания.
    /// </summary>
    public bool IsAppeal { get; set; }
  }
  
  /// <summary>
  /// Информация о странах для товарных знаков.
  /// </summary>
  partial class Countries
  {
    /// <summary>
    /// Регистрационный номер.
    /// </summary>
    public string RegistrationNumber { get; set; }   
    
    /// <summary>
    /// Страна.
    /// </summary>
    public DirRX.Solution.ICountry Country { get; set; }    
    
    /// <summary>
    /// Признак страны поставки.
    /// </summary>
    public bool IsDelivery { get; set; }
    
    /// <summary>
    /// Признак страны производителя.
    /// </summary>
    public bool IsProduction { get; set; }
  }
}