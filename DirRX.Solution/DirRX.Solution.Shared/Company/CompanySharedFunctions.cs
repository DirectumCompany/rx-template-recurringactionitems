using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Company;

namespace DirRX.Solution.Shared
{
  partial class CompanyFunctions
  {

    /// <summary>
    /// Вычислить срок действия проверки.
    /// </summary>
    public DateTime? CalcCheckingValidDate()
    {
      if (_obj.CheckingResult == null || !_obj.CheckingDate.HasValue)
        return null;
      
      int validPeriod = _obj.CheckingResult.ValidPeriod.GetValueOrDefault();
      if (validPeriod == 0)
        return null;
      
      return _obj.CheckingDate.Value.AddMonths(validPeriod);
    }

    /// <summary>
    /// Установить доступность свойств.
    /// </summary>
    public void SetPropertiesAvailability()
    {
      bool isComplianceSpecialist = Users.Current.IncludedIn(PartiesControl.PublicConstants.Module.ComplianceSpecialistRole);
      bool isAdministratorOrSpecialRole = Users.Current.IncludedIn(Roles.Administrators) || Users.Current.IncludedIn(PartiesControl.PublicConstants.Module.SpecialFieldsRole);
      _obj.State.Properties.IsStrategicPartner.IsEnabled = isAdministratorOrSpecialRole;
      _obj.State.Properties.IsSanctions.IsEnabled = isComplianceSpecialist;
      _obj.State.Properties.CheckingType.IsEnabled = isAdministratorOrSpecialRole;
    }
    
    /// <summary>
    /// Статус контрагента «Стоп-лист».
    /// </summary>
    [Public]
    public bool IsStopList()
    {
      return _obj.CounterpartyStatus != null && _obj.CounterpartyStatus.Sid == PartiesControl.PublicConstants.CounterpartyStatus.DefaultStatus.StopListSid;
    }
    
    /// <summary>
    /// Статус контрагента «Требуется проверка».
    /// </summary>
    [Public]
    public bool IsCheckingRequired()
    {
      return _obj.CounterpartyStatus != null && _obj.CounterpartyStatus.Sid == PartiesControl.PublicConstants.CounterpartyStatus.DefaultStatus.CheckingRequiredSid;
    }
    
    /// <summary>
    /// Контрагент не одобрен.
    /// </summary>
    [Public]
    public bool IsNotApproved()
    {
      return _obj.CheckingResult != null && _obj.CheckingResult.Decision == PartiesControl.CheckingResult.Decision.NotApproved;
    }
    
    /// <summary>
    /// Статус контрагента не соответствует категории договора.
    /// </summary>
    /// <param name="category">Категория.</param>
    /// <returns>True, если статус контрагента не соответствует категории договора.</returns>
    [Public]
    public bool IsStatusNotCorrectToCategory(Solution.IContractCategory category)
    {
      var counterpartyCheckingResult = DirRX.PartiesControl.PublicFunctions.CheckingResult.Remote.GetResultForStatus(_obj.CounterpartyStatus);
      
      return !category.CounterpartyCheckingResult.Any(r => PartiesControl.CheckingResults.Equals(r.Result, counterpartyCheckingResult));
    }
    
    /// <summary>
    /// Требуется создание заявки на проверку контрагента.
    /// </summary>
    /// <description>Статус контрагента «Требуется проверка» ИЛИ статус контрагента не соответствует виду заключаемого договора и статус не «Стоп-лист» и при этом контрагент одобрен или отсутствует результат проверки.</description>
    /// <param name="needContractCounterpartyChecking">True, если статус контрагента не соответствует категории договора.</param>
    /// <returns>True, если требуется создание заявки на проверку контрагента.</returns>
    [Public]
    public bool NeedCreateRevisionRequest(bool statusNotCorrectToCategory)
    {
      return IsCheckingRequired() || (statusNotCorrectToCategory && !IsStopList() && !IsNotApproved());
    }

  }
}