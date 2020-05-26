using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ConfirmContractExecutedTask;

namespace DirRX.ContractsCustom
{
  partial class ConfirmContractExecutedTaskServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.Subject = DirRX.ContractsCustom.ConfirmContractExecutedTasks.Resources.ConfirmContractExecutedTaskSubject;
      _obj.ActiveText = DirRX.ContractsCustom.ConfirmContractExecutedTasks.Resources.ConfirmContractExecutedTaskText;
    }
  }

}