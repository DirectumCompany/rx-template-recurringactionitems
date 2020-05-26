using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Employee;

namespace DirRX.Solution
{
  partial class EmployeeServerHandlers
  {

    public override void Saved(Sungero.Domain.SavedEventArgs e)
    {
      Logger.Debug("Событие после сохранения сотрудника. Актуализация RecipientList в новом и старом подразделениях.");
      base.Saved(e);
      Logger.Debug("Событие после сохранения сотрудника.Актуализация RecipientList успешно выполнена.");
    }

    public override void Deleting(Sungero.Domain.DeletingEventArgs e)
    {
      base.Deleting(e);
      
      if (_obj.Manager != null)
        Solution.Functions.Employee.DeleteSystemSubstitution(_obj, _obj.Manager);
    }

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      base.Saving(e);
      
      // Создание замещений для нового руководителя.
      if (_obj.State.Properties.Manager.IsChanged)
      {
        if (_obj.Manager != null)
          _obj.NeedUpdateSubtitution = true;

        if (_obj.State.Properties.Manager.OriginalValue != null)
          Solution.Functions.Employee.DeleteSystemSubstitution(_obj, _obj.State.Properties.Manager.OriginalValue);
      }
    }
  }

}