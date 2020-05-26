using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ConfirmContractExecutedAssignment;

namespace DirRX.ContractsCustom.Server
{
  partial class ConfirmContractExecutedAssignmentFunctions
  {
    /// <summary>
    /// Проверка открытия карточки документа пользователем.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="version">Дата и время начала.</param>
    /// <returns>True, если прочитано, иначе - false.</returns>
    [Remote(IsPure = true), Public]
    public static bool IsDocumentOpened(Sungero.Docflow.IOfficialDocument document, DateTime dateFrom)
    {
      return document.History.GetAll()
        .Where(x => Equals(Users.Current, x.User) &&
               x.Action == Sungero.CoreEntities.History.Action.Read &&
               x.Operation == null &&
               x.HistoryDate != null && x.HistoryDate.Between(dateFrom, Calendar.Now)).Any();
    }
  }
}