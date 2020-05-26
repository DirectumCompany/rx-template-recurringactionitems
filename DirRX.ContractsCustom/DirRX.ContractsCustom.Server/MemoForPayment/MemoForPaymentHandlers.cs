using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.MemoForPayment;

namespace DirRX.ContractsCustom
{
  partial class MemoForPaymentCounterpartyPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> CounterpartyFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = base.CounterpartyFiltering(query, e);
      return query;
    }
  }

  partial class MemoForPaymentServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      // Формируем префикс и постфикс рег. номера.
      var leadingDocumentNumber = Functions.MemoForPayment.GetLeadDocumentNumber(_obj);
      var errorMessage = DirRX.Solution.PublicFunctions.DocumentRegister.UpdateDocumentPrefixAndPostfix(_obj, e, leadingDocumentNumber);
      if (errorMessage != string.Empty)
        e.AddError(errorMessage);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      if (!_obj.State.IsCopied)
        _obj.IsHighUrgency = false; 
    }
  }

  partial class MemoForPaymentUrgencyReasonPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> UrgencyReasonFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(r => r.Usage == DirRX.ContractsCustom.DefaultReasons.Usage.Promptly);
    }
  }

}