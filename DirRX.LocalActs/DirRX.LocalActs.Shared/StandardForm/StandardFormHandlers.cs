using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.StandardForm;

namespace DirRX.LocalActs
{
  partial class StandardFormSharedHandlers
  {

    public virtual void DocumentKindChanged(DirRX.LocalActs.Shared.StandardFormDocumentKindChangedEventArgs e)
    {
      _obj.Addressee = null;
    }

    public virtual void DocumentTypeChanged(DirRX.LocalActs.Shared.StandardFormDocumentTypeChangedEventArgs e)
    {
      _obj.DocumentKind = null;
      _obj.Addressee = null;
      _obj.Template = null;
      Functions.StandardForm.SetStateProperties(_obj);
    }

    public virtual void IsBPOwnerChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        _obj.State.Properties.Supervisor.IsEnabled = !e.NewValue.Value;
        _obj.Supervisor = null;
      }
    }
  }

}