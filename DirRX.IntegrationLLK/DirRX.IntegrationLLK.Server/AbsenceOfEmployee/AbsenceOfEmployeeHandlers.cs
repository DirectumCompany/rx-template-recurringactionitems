using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.IntegrationLLK.AbsenceOfEmployee;

namespace DirRX.IntegrationLLK
{
  partial class AbsenceOfEmployeeServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      string shortStartDate = _obj.StartDate.HasValue ? _obj.StartDate.Value.ToShortDateString() : string.Empty;
      string shortEndDate = _obj.EndDate.HasValue ? _obj.EndDate.Value.ToShortDateString() : string.Empty;
      _obj.Name = AbsenceOfEmployees.Resources.AbsenceOfEmployeeRecordNameFormat(shortStartDate, shortEndDate, _obj.Employee.Name);
    }
  }

}