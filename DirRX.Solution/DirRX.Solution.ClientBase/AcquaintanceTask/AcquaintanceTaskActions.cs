using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.AcquaintanceTask;

namespace DirRX.Solution.Client
{
  partial class AcquaintanceTaskActions
  {
    public override void ShowNotAutomatedEmployees(Sungero.Domain.Client.ExecuteActionArgs e)
    {      
      var recipients = _obj.Performers.Select(x => x.Performer).ToList();
      var activeRecipients = recipients.Where(x => x != null && x.Status == Sungero.CoreEntities.DatabookEntry.Status.Active).ToList();
      var performers = Sungero.Docflow.PublicFunctions.Module.Remote.GetEmployeesFromRecipients(activeRecipients)
        .Where(emp => emp.IsSystem != true && emp.Status == Sungero.CoreEntities.DatabookEntry.Status.Active && 
               (emp.Login == null || emp.Login.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed))
        .Distinct()
        .ToList();
      
      performers.Show();
    }

    public override bool CanShowNotAutomatedEmployees(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanShowNotAutomatedEmployees(e);
    }

    public override void Start(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      // Проверить корректность срока.
      if (!Sungero.Docflow.PublicFunctions.Module.CheckDeadline(_obj.Deadline, Calendar.Now))
      {
        e.AddError(Sungero.RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThenToday);
        return;
      }
      
      // Проверить существование тела документа.
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      if (_obj.IsElectronicAcquaintance.Value && !document.HasVersions)
      {
        e.AddError(AcquaintanceTasks.Resources.AcquaintanceTaskDocumentWithoutBodyMessage);
        return;
      }
      
      var employees = Functions.AcquaintanceTask.Remote.GetActivePerformers(_obj);
      
      // Проверить наличие участников ознакомления.
      if (employees.Count == 0)
      {
        e.AddError(Sungero.RecordManagement.AcquaintanceTasks.Resources.PerformersCantBeEmpty);
        return;
      }
      
      // Техничекое ограничение платформы на запуск задачи для большого числа участников.
      if (employees.Count > Sungero.RecordManagement.Constants.AcquaintanceTask.PerformersLimit)
      {
        e.AddError(Sungero.RecordManagement.AcquaintanceTasks.Resources.ToManyPerformersFormat(Sungero.RecordManagement.Constants.AcquaintanceTask.PerformersLimit));
        return;
      }
      
      // Выдать права на прочие документы.
      var otherDocuments = _obj.OtherGroup.All.ToList();
      var recipients = employees.Cast<IRecipient>().ToList();
      if (Sungero.Docflow.PublicFunctions.Module.ShowDialogGrantAccessRights(_obj, otherDocuments, recipients) == false)
        return;
      
      e.CloseFormAfterAction = true;
      _obj.Start();
    }

    public override bool CanStart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanStart(e);
    }

    public virtual void ClearList(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      _obj.Performers.Clear();
    }

    public virtual bool CanClearList(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.Status == DirRX.Solution.AcquaintanceTask.Status.Draft;
    }

  }

}