using System;
using Sungero.Core;

namespace DirRX.Brands.Constants
{
  public static class Module
  {

    /// <summary>
    /// Имя параметра в таблице Sungero_Docflow_Params для фиксации последнего выполнения фонового процесса рассылки заданий-оповещений.
    /// </summary>
    public const string LastNewDesignationTaskDateTimeDocflowParamName = "LastNewDesignationTaskDate";

    /// <summary>
    /// GUID для роли "Специалист по интеллектуальной собственности".
    /// </summary>
    [Public]
    public static readonly Guid IntellectualPropertySpecialistRoleGuid = Guid.Parse("A34F09FE-A2B4-4B03-B462-E00A65ECF706");
    
    /// <summary>
    /// GUID для роли "Пользователи с доступом к реестру товарных знаков".
    /// </summary>
    public static readonly Guid BrandsManagersRoleGuid = Guid.Parse("1EB68B63-2C59-42E5-BD70-4249FB01E21A");
    
    /// <summary>
    /// GUID для роли "Ответственный за добавление наименований продуктов".
    /// </summary>
    public static readonly Guid ProductNameManagersRoleGuid = Guid.Parse("05CA9D84-E53F-438A-8400-F500D6E6B0B3");
    
    /// <summary>
    /// GUID для роли "Ответственные за настройку видов товаров и товарных групп".
    /// </summary>
    public static readonly Guid ProductKindAndGroupManagersRoleGuid = Guid.Parse("F9F1ACCD-B63E-4A70-8F2F-273AE1F8FE4C");
    
    /// <summary>
    /// GUID для роли "Ответственные за настройку стран".
    /// </summary>
    [Public]
    public static readonly Guid CountriesManagersRoleGuid = Guid.Parse("B27FFB34-0B56-41FD-827B-D643DD0AEF5F");
    
  }
}