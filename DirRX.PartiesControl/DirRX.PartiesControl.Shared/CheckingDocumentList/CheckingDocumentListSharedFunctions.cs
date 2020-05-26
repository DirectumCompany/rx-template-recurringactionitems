using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.CheckingDocumentList;

namespace DirRX.PartiesControl.Shared
{
  partial class CheckingDocumentListFunctions
  {

    /// <summary>
    /// Заполнить наименование записи.
    /// </summary>
    public virtual void FillName()
    {
      var name = string.Empty;
      
      /* Имя в формате:
        Перечень документов для проверки контрагента - <Резидент/Нерезидент>. Тип контрагента - <Тип. контрагента>. Причина проверки - <Причина проверки>.
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        name = CheckingDocumentLists.Resources.CheckingDocumentListNameFormat(PartiesControl.CheckingDocumentLists.Info.Properties.ResidentPick.GetLocalizedValue(_obj.ResidentPick),
                                                                              PartiesControl.CheckingDocumentLists.Info.Properties.CounterpartyType.GetLocalizedValue(_obj.CounterpartyType),
                                                                              _obj.Reason.Name);
      }
      
      name = Sungero.Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = name;
    }

  }
}