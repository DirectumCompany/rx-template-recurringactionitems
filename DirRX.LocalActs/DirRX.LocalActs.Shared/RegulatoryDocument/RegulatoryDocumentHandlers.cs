using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.RegulatoryDocument;

namespace DirRX.LocalActs
{

  partial class RegulatoryDocumentSharedHandlers
  {

    public override void DocumentKindChanged(Sungero.Docflow.Shared.OfficialDocumentDocumentKindChangedEventArgs e)
    {
      base.DocumentKindChanged(e);
      
      Functions.RegulatoryDocument.CalculateEditionAndIndex(_obj);
    }

    public virtual void BPGroupChanged(DirRX.LocalActs.Shared.RegulatoryDocumentBPGroupChangedEventArgs e)
    {
      Functions.RegulatoryDocument.CalculateEditionAndIndex(_obj);
    }

    public virtual void PreviousEditionChanged(DirRX.LocalActs.Shared.RegulatoryDocumentPreviousEditionChangedEventArgs e)
    {
      Functions.RegulatoryDocument.CalculateEditionAndIndex(_obj);
      
      // TODO: Если вернут требование автосвязывания, то раскомментировать.
      //_obj.Relations.AddFromOrUpdate(Constants.Module.RegulatoryNewEditionRelationName, e.OldValue, e.NewValue);
    }

  }
}