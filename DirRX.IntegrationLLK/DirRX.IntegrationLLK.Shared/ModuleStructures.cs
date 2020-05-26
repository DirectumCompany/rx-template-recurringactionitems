using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.IntegrationLLK.Structures.Module
{
  
  /// <summary>
  /// Балансовая единица, импортированная из ССПД.
  /// </summary>
  partial class SSPDBalanceUnit
  {
    /// <summary>
    /// Идентификатор балансовой единицы.
    /// </summary>
    public Guid BalanceUnitGUID { get;set; }
    
    /// <summary>
    /// Наименование балансовой единицы.
    /// </summary>
    public string BalanceUnitName { get;set; }

    /// <summary>
    /// Код БЕ в КССС.
    /// </summary>
    public int? KSSSBalanceUnitID { get;set; }
    
    /// <summary>
    /// КССС код контрагента.
    /// </summary>
    public int? KSSSContragentID { get;set; }
    
    /// <summary>
    /// Признак блокировки.
    /// </summary>
    public bool isBlocked { get;set; }
  }
  
  /// <summary>
  /// Организационная структура, импортированная из ССПД.
  /// </summary>
  partial class SSPDOrgstructure
  {
    /// <summary>
    /// Идентификатор организационной структуры.
    /// </summary>
    public Guid OrgstructureGUID { get;set; }
    
    /// <summary>
    /// Составной код организационной структуры с префиксом кадровой системы.
    /// </summary>
    public string OUID { get;set; }

    /// <summary>
    /// Наименование организационной структуры.
    /// </summary>
    public string DisplayName { get;set; }
    
    /// <summary>
    /// Код оргструктуры в HR.
    /// </summary>
    public string OUHR { get;set; }
    
    /// <summary>
    /// Идентификатор балансовой единицы.
    /// </summary>
    public Guid BalanceUnitGUID { get;set; }
    
    /// <summary>
    /// Код родительской организационной структуры.
    /// </summary>
    public string POUID { get;set; }
    
    /// <summary>
    /// Признак блокировки.
    /// </summary>
    public bool isBlocked { get;set; }
    
    /// <summary>
    /// Подразделение в системе.
    /// </summary>
    public DirRX.Solution.IDepartment Department { get;set; }
    
    /// <summary>
    /// Подразделение организации в системе.
    /// </summary>
    public IDepartCompanies CompanyDepartment { get;set; }
  }
  
  /// <summary>
  /// Сотрудник, импортированный из ССПД.
  /// </summary>
  partial class SSPDPerson
  {
    /// <summary>
    /// Идентификатор сотрудника.
    /// </summary>
    public Guid PersonGUID { get;set; }
    
    /// <summary>
    /// Фамилия сотрудника.
    /// </summary>
    public string SurName { get;set; }

    /// <summary>
    /// Имя сотрудника.
    /// </summary>
    public string GivenName { get;set; }
    
    /// <summary>
    /// Отчество сотрудника.
    /// </summary>
    public string MiddleName { get;set; }
    
    /// <summary>
    /// ФИО сотрудника.
    /// </summary>
    public string FullName { get;set; }
    
    /// <summary>
    /// Табельный номер.
    /// </summary>
    public string PersonalNumber { get;set; }
    
    /// <summary>
    /// Логин.
    /// </summary>
    public string PersonLogin { get;set; }
    
    /// <summary>
    /// Должность.
    /// </summary>
    public string Position { get;set; }
    
    /// <summary>
    /// Эл. почта.
    /// </summary>
    public string Mail { get;set; }
    
    /// <summary>
    /// Рабочий адрес.
    /// </summary>
    public string OfficeAddress { get;set; }
    
    /// <summary>
    /// Рабочие телефоны.
    /// </summary>
    public string OfficePhones { get;set; }
    
    /// <summary>
    /// Идентификатор организационной структуры.
    /// </summary>
    public Guid OrgstructureGUID { get;set; }
    
    /// <summary>
    /// Идентификатор балансовой единицы.
    /// </summary>
    public Guid BalanceUnitGUID { get;set; }

    /// <summary>
    /// Признак блокировки.
    /// </summary>
    public bool isBlocked { get;set; }
    
    /// <summary>
    /// Признак руководителя.
    /// </summary>
    public bool isManager { get;set; }
    
    /// <summary>
    /// Сотрудник в системе.
    /// </summary>
    public DirRX.Solution.IEmployee Employee { get;set; }
    
    /// <summary>
    /// Контакт в системе.
    /// </summary>
    public Sungero.Parties.IContact Contact { get;set; }
  }
  
  /// <summary>
  /// Руководитель, импортированный из ССПД.
  /// </summary>
  partial class SSPDManager
  {
    /// <summary>
    /// Идентификатор сотрудника.
    /// </summary>
    public Guid PersonGUID { get;set; }
    
    /// <summary>
    /// ФИО сотрудника.
    /// </summary>
    public string FullName { get;set; }
    
    /// <summary>
    /// Табельный номер.
    /// </summary>
    public string PersonalNumber { get;set; }
    
    /// <summary>
    /// Идентификатор организационной структуры.
    /// </summary>
    public Guid OrgstructureGUID { get;set; }
    
    /// <summary>
    /// Идентификатор балансовой единицы.
    /// </summary>
    public Guid BalanceUnitGUID { get;set; }
  }
  
  /// <summary>
  /// Информация об отсутствии сотрудника.
  /// </summary>
  [Public]
  partial class AbsenceOfEmployee
  {
    /// <summary>
    /// Табельный номер.
    /// </summary>
    public string PersonalNumber { get;set; }
    
    /// <summary>
    /// ФИО сотрудника.
    /// </summary>
    public string EmployeeName { get;set; }

    /// <summary>
    /// Штатная должность.
    /// </summary>
    public string Position { get;set; }
    
    /// <summary>
    /// Вид отсутствия или присутствия.
    /// </summary>
    public string AbsenceType { get;set; }
    
    /// <summary>
    /// Начало.
    /// </summary>
    public DateTime StartDate { get;set; }
    
    /// <summary>
    /// Истечение.
    /// </summary>
    public DateTime EndDate { get;set; }
    
    /// <summary>
    /// Примечание.
    /// </summary>
    public string Comment { get;set; }
    
    /// <summary>
    /// Сотрудник.
    /// </summary>
    public DirRX.Solution.IEmployee Employee { get;set; }
  }
}