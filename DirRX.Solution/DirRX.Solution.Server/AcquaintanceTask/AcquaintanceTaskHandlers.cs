using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.AcquaintanceTask;

namespace DirRX.Solution
{
  partial class AcquaintanceTaskServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      #region Скопированно из стандартной.
      
      // Запомнить номер версии и хеш для отчета.
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
      {
        _obj.AcquaintanceVersions.Clear();
        Functions.AcquaintanceTask.StoreAcquaintanceTaskVersion(_obj, document, true);
        var addendas = _obj.AddendaGroup.OfficialDocuments;
        foreach (var addenda in addendas)
          Functions.AcquaintanceTask.StoreAcquaintanceTaskVersion(_obj, addenda, false);
      }
      
      #endregion
      
      // Запомнить участников ознакомления.
      _obj.Acquainters.Clear();
      
      var recipients = _obj.Performers.Select(x => x.Performer).ToList();
      var employees = Sungero.Docflow.PublicFunctions.Module.Remote.GetEmployeesFromRecipients(recipients)
        .Distinct();
      
      if (_obj.IsElectronicAcquaintance.GetValueOrDefault())
        employees = employees.Where(emp => emp.Login != null && emp.Login.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
      
      foreach (var employee in employees)
      {
        var newAcquainter = _obj.Acquainters.AddNew();
        newAcquainter.Acquainter = employee;
      }
    }
  }


}