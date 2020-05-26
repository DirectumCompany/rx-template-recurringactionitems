using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Solution.Module.Contracts.Server
{
  partial class ModuleFunctions
  {

       /// <summary>
    /// Сотрудники, которых необходимо уведомить о сроке договорного документа.
    /// </summary>
    /// <param name="contractualDocument">Договорной документ.</param>
    /// <returns>Список сотрудников.</returns>
    public virtual List<IUser> GetPerformersOfContractualDocument(Sungero.Contracts.IContractualDocument contractualDocument)
    {
      var performer = contractualDocument.ResponsibleEmployee ?? Employees.As(contractualDocument.Author);
      var performers = new List<IUser>() { };
      
      if (performer == null)
        return performers;
      
      var manager = Sungero.Docflow.PublicFunctions.Module.Remote.GetManager(performer);
      
      var performerPersonalSetting = Sungero.Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(performer).MyContractsNotification;
      
      if (performerPersonalSetting == true)
        performers.Add(performer);
      if (manager != null)
      {
        var managerPersonalSetting = Sungero.Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(manager).MySubordinatesContractsNotification;
        if (managerPersonalSetting == true)
          performers.Add(manager);
      }
      
      return performers;
    }

  }
}