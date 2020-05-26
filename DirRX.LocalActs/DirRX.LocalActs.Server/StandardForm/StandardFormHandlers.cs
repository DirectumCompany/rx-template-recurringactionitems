using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.StandardForm;

namespace DirRX.LocalActs
{
  partial class StandardFormServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.NeedCheckTrademarkRegistration = false;
      _obj.NeedPaperSigning = false;
      _obj.NeedPersonalSignatureAcquaintance = false;
      _obj.NeedRegulatoryDocument = false;
      _obj.NeedTaxMonitoring = false;
      _obj.IsBPOwner = false;
    }
  }

  partial class StandardFormTemplatePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> TemplateFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.DocumentType != null)
        query = query.Where(x => x.DocumentType.ToString() == _obj.DocumentType.DocumentTypeGuid);
      return query;
    }
  }

  partial class StandardFormSupervisorPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> SupervisorFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(x => x.IsSingleUser == true);
    }
  }

  partial class StandardFormDocumentKindPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> DocumentKindFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return _obj.DocumentType != null ? query.Where(x => Sungero.Docflow.DocumentTypes.Equals(x.DocumentType, _obj.DocumentType)) : query;
    }
  }

}