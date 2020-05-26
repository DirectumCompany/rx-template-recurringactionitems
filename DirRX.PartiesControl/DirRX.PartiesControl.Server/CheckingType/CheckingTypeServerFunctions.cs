using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.CheckingType;

namespace DirRX.PartiesControl.Server
{
  partial class CheckingTypeFunctions
  {

    /// <summary>
    /// Создать тип проверки контрагента.
    /// </summary>
    [Public]
    public static void CreateCheckingType(string name, Enumeration provision)
    {
      if (CheckingTypes.GetAll(s => s.Name == name).Any())
        return;
      
      var checkingType = CheckingTypes.Create();
      checkingType.Name = name;
      checkingType.DocProvision = provision;
      checkingType.DefaultChecking = false;
      checkingType.Status = Sungero.CoreEntities.DatabookEntry.Status.Active;
      checkingType.Save();
    }
    
    /// <summary>
    /// Получить тип проверки контрагента по умолчанию для группы Лукойл.
    /// </summary>
    /// <returns>Тип проверки контрагента.</returns>
    [Public, Remote(IsPure = true)]
    public static ICheckingType GetLukoilCheckingType()
    {
      return CheckingTypes.GetAll(t => t.LukoilChecking == true).FirstOrDefault();
    }

  }
}