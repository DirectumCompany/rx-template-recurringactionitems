using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.RegulatoryDocument;

namespace DirRX.LocalActs
{

  partial class RegulatoryDocumentClientHandlers
  {

    public virtual void EndDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && _obj.StartDate.HasValue && e.NewValue.Value < _obj.StartDate.Value)
        e.AddWarning(LocalActs.RegulatoryDocuments.Resources.EndDateMustBeMoreThenStartDate);
    }

    public virtual void StartDateValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && _obj.EndDate.HasValue && e.NewValue.Value > _obj.EndDate.Value)
        e.AddWarning(LocalActs.RegulatoryDocuments.Resources.EndDateMustBeMoreThenStartDate);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      
      Functions.RegulatoryDocument.SetPropertiesAvailabilityAndVisibility(_obj);
    }

  }
}