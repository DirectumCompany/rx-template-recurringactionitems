using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ContractConstant;

namespace DirRX.ContractsCustom.Server
{
  partial class ContractConstantFunctions
  {
    [Public]
    public static System.TimeSpan GetConstantPeriodSpan(Guid sid)
    {
      var constant = ContractConstants.GetAll(x => x.Sid == sid.ToString()).FirstOrDefault();
      var period = TimeSpan.Zero;
      if (constant.Unit == ContractsCustom.ContractConstant.Unit.Day)
        period = Calendar.Today.AddDays(constant.Period.Value) - Calendar.Today;
      if (constant.Unit == ContractsCustom.ContractConstant.Unit.Month)
        period = Calendar.Today.AddMonths(constant.Period.Value) - Calendar.Today;
      if (constant.Unit == ContractsCustom.ContractConstant.Unit.Year)
        period = Calendar.Today.AddYears(constant.Period.Value) - Calendar.Today;
      return period;
    }
  }
}