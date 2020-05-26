using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.CheckingDocumentList;

namespace DirRX.PartiesControl.Server
{
  partial class CheckingDocumentListFunctions
  {
    /// <summary>
    /// Поиск перечня документов.
    /// </summary>
    /// <param name="resident">Признак "Резидент".</param>
    /// <param name="counterpartyType">Тип контрагента.</param>
    /// <param name="checkingReason">Причина проверки.</param>
    /// <returns></returns>
    [Public, Remote]
    public static ICheckingDocumentList GetCheckingDocumentList(bool resident, Enumeration counterpartyType, ICheckingReason checkingReason)
    {
      return CheckingDocumentLists.GetAll().ToList().FirstOrDefault(l => CheckingReasons.Equals(l.Reason, checkingReason) &&
                                                                    (l.ResidentPick.GetValueOrDefault() == DirRX.PartiesControl.CheckingDocumentList.ResidentPick.Resident && resident ||
                                                                     l.ResidentPick.GetValueOrDefault() == DirRX.PartiesControl.CheckingDocumentList.ResidentPick.NonResident && !resident) &&
                                                                    l.CounterpartyType == counterpartyType);
    }
  }
}