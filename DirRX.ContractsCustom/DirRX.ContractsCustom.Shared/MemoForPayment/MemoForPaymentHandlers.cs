using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.MemoForPayment;

namespace DirRX.ContractsCustom
{
  partial class MemoForPaymentSharedHandlers
  {

    public virtual void CoExecutorChanged(DirRX.ContractsCustom.Shared.MemoForPaymentCoExecutorChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue)
      {
        _obj.Supervisor = Functions.MemoForPayment.GetSupervisor(_obj);
        _obj.Department = e.NewValue.Department;
      }
    }

    public override void ResponsibleEmployeeChanged(Sungero.Contracts.Shared.ContractualDocumentResponsibleEmployeeChangedEventArgs e)
    {
      base.ResponsibleEmployeeChanged(e);
      
      if (e.NewValue != null && e.NewValue != e.OldValue)
      {
        // Если не указан Соисполнитель, то установить подразделение Исполнителя.
        if (_obj.CoExecutor == null)
        {
          _obj.Department = e.NewValue.Department;
          _obj.Supervisor = Functions.MemoForPayment.GetSupervisor(_obj);
        }
        _obj.BusinessUnit = e.NewValue.Department.BusinessUnit;
      }
      
      
    }

    public virtual void PaymentConditionChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      var isDelay = e.NewValue == PaymentCondition.Delay;
      _obj.State.Properties.DaysOfDelay.IsEnabled = isDelay;
      _obj.State.Properties.DaysOfDelay.IsRequired = isDelay;
      
    }

    public virtual void IsHighUrgencyChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      var isHighUrgency = e.NewValue == true;
      _obj.State.Properties.UrgencyReason.IsEnabled = isHighUrgency;
      _obj.State.Properties.UrgencyReason.IsRequired = isHighUrgency;
    }

  }
}