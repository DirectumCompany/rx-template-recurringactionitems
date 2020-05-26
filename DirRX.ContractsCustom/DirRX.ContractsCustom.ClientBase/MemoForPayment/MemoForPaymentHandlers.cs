using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.MemoForPayment;

namespace DirRX.ContractsCustom
{
  partial class MemoForPaymentClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      _obj.State.Properties.RegistrationState.IsVisible = false;
      _obj.State.Properties.InternalApprovalState.IsVisible = false;
      _obj.State.Properties.ExternalApprovalState.IsVisible = false;
      _obj.State.Properties.ExecutionState.IsVisible = false;
      _obj.State.Properties.ControlExecutionState.IsVisible = false;
      _obj.State.Properties.ExchangeState.IsVisible = false;
      
      // Соисполнитель доступен для редактирования  только для сотрудников из роли Сотрудники клиентского сервиса.
      var userIncludedInRole = Users.Current.IncludedIn(Constants.Module.RoleGuid.CustomerServiceEmployeesRole);
      _obj.State.Properties.CoExecutor.IsEnabled = userIncludedInRole;
    }
  }

}