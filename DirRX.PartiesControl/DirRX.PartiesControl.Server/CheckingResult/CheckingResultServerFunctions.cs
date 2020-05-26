using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.CheckingResult;

namespace DirRX.PartiesControl.Server
{
  partial class CheckingResultFunctions
  {

    /// <summary>
    /// Получить дубли результатов проверки контрагентов.
    /// </summary>
    [Remote(IsPure = true)]
    public IQueryable<ICheckingResult> GetDoubleResults()
    {
      var reasonListId = _obj.Reasons.Select(r => r.Reason.Id).ToList();
      var typeListId = _obj.Types.Select(r => r.Type.Id).ToList();
      
      var duplicates = CheckingResults.GetAll().Where(r => !Equals(r, _obj) && r.Name == _obj.Name).ToList();
      duplicates.AddRange(CheckingResults.GetAll()
        .Where(r => !Equals(r, _obj) &&
               r.Decision.Equals(Decision.Approved) &&
               r.Status == DirRX.PartiesControl.CheckingResult.Status.Active)
        .ToList()
        .Where(r => r.Reasons.Select(rr => rr.Reason.Id).OrderBy(rr => rr).SequenceEqual(reasonListId.OrderBy(rr => rr)) &&
               r.Types.Select(rr => rr.Type.Id).OrderBy(rr => rr).SequenceEqual(typeListId.OrderBy(rr => rr))));
      
      return duplicates.AsQueryable();
    }

    [Public, Remote(IsPure = true)]
    public static ICheckingResult GetResultForStatus(ICounterpartyStatus status)
    {
      return CheckingResults.GetAll().ToList().FirstOrDefault(r => CounterpartyStatuses.Equals(r.CounterpartyStatus, status));
    }
  }
}