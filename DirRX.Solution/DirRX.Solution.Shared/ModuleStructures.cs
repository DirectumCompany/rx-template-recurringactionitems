using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Solution.Structures.Module
{
  
  #region Подписант.
  
  /// <summary>
  /// Подписант.
  /// </summary>
  [Public]
  partial class Signatory
  {
    public Sungero.Company.IEmployee Employee { get; set; }
    
    public int Priority { get; set; }
  }
  
  #endregion
  
  #region Результат формирования пакетов на отправку.
  
  /// <summary>
  /// Результат формирования пакетов на отправку.
  /// </summary>
  partial class CreatePackagesResult
  {
    public List<DirRX.ContractsCustom.IShippingPackage> Packages { get; set; }
    
    public string Message { get; set; }
  }
  
  #endregion
  
  #region Договор или ДС для экспорта в SAP.
  
  /// <summary>
  /// Код вида договора и функциональность для экспорта в SAP.
  /// </summary>
  [Public]
  partial class SAPContractKindFunctionality
  {
    /// <summary>
    /// Код вида договора SAP.
    /// </summary>
    public string ContractKind {get; set;}
    
    /// <summary>
    /// Функциональность договора.
    /// </summary>
    public string ContractFunctionality {get; set;}
  }
  
  /// <summary>
  /// Договор или ДС для экспорта в SAP.
  /// </summary>
  [Public]
  partial class SAPContract
  {
    /// <summary>
    /// ИД документа.
    /// </summary>
    public string Id {get; set;}
    
    /// <summary>
    /// Код КССС контрагента.
    /// </summary>
    public string KSSSContragentId {get; set;}
    
    /// <summary>
    /// Балансовая единица.
    /// </summary>
    public string BalanceUnit {get; set;}
    
    /// <summary>
    /// Функциональность договора.
    /// </summary>
    public string ContractFunctionality {get; set;}

    /// <summary>
    /// Код вида договора SAP.
    /// </summary>
    public string ContractKind {get; set;}
    
    /// <summary>
    /// Действует с.
    /// </summary>
    public string ValidFrom {get; set;}
    
    /// <summary>
    /// Действует по.
    /// </summary>
    public string ValidTill {get; set;}
    
    /// <summary>
    /// Дата договора.
    /// </summary>
    public string DocumentDate {get; set;}
    
    /// <summary>
    /// Рег. № ДС.
    /// </summary>
    public string RegNumber {get; set;}
    
    /// <summary>
    /// Рег. № Договора, Для ДС Рег. № Догвора основания.
    /// </summary>
    public string RegNumberContract {get; set;}
    
    /// <summary>
    /// Тип договора.
    /// </summary>
    public string DocumentType {get; set;}
    
    /// <summary>
    /// Сумма.
    /// </summary>
    public string TotalAmount {get; set;}
    
    /// <summary>
    /// Валюта (буквенный код).
    /// </summary>
    public string Currency {get; set;}
    
    /// <summary>
    /// Состояние.
    /// </summary>
    public string LifeCycleState {get; set;}
    
    /// <summary>
    /// Исполнитель.
    /// </summary>
    public string ResponsibleEmployee {get; set;}
    
    /// <summary>
    /// Подписал.
    /// </summary>
    public string OurSignatory {get; set;}
    
    /// <summary>
    /// Куратор.
    /// </summary>
    public string Supervisor {get; set;}
    
    /// <summary>
    /// Территория.
    /// </summary>
    public string Territory {get; set;}
    
    /// <summary>
    /// Электронная почта Ответственного за SAP.
    /// </summary>
    public string EmployeeEmail {get; set;}
    
    /// <summary>
    /// Имя (Договора, ДС).
    /// </summary>
    public string ContractName {get; set;}
    
    /// <summary>
    /// Подписант от контрагента.
    /// </summary>
    public string CountSignName {get; set;}
    
    /// <summary>
    /// Подписал (Наша сторона).
    /// </summary>
    public string OurSignName {get; set;}
    
    /// <summary>
    /// Способ доставки договора.
    /// </summary>
    public string DeliveryMethod {get; set;}
    
    /// <summary>
    /// Контакт.
    /// </summary>
    public string ContactName {get; set;}
  }
  
  #endregion
  
  #region Информация о включении/исключения контрагента в стоп-лист для экспорта в SAP.
  
  /// <summary>
  /// Информация о включении/исключения контрагента в стоп-лист для экспорта в SAP.
  /// </summary>
  [Public]
  partial class SAPStoplistEvent
  {
    /// <summary>
    /// КССС Код контрагента.
    /// </summary>
    public string KSSSContragentId {get; set;}
    
    /// <summary>
    /// Статус СТОП-ЛИСТА.
    /// </summary>
    public string StopListState {get; set;}
    
    /// <summary>
    /// № события.
    /// </summary>
    public string GUIDEvent {get; set;}
    
    /// <summary>
    /// Статус события.
    /// </summary>
    public string EventState {get; set;}
    
    /// <summary>
    /// Дата события.
    /// </summary>
    public string EventDate {get; set;}
    
    /// <summary>
    /// Код основания.
    /// </summary>
    public int IDReason {get; set;}

    /// <summary>
    /// Дата события.
    /// </summary>
    public string Reason {get; set;}
    
    /// <summary>
    /// Комментарий.
    /// </summary>
    public string Comment {get; set;}
    
    /// <summary>
    /// Пользователь.
    /// </summary>
    public string Employee {get; set;}
  }
  
  #endregion
}