using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.StandardForm;

namespace DirRX.LocalActs.Client
{
  partial class StandardFormActions
  {
    public override void SaveAndClose(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.StandardForm.Validation(_obj, e))
        return;
      base.SaveAndClose(e);
    }

    public override bool CanSaveAndClose(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSaveAndClose(e);
    }

    public virtual void SaveDuplicate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      _obj.Save();
      e.CloseFormAfterAction = true;
    }

    public virtual bool CanSaveDuplicate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public override void Save(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!Functions.StandardForm.Validation(_obj, e))
        return;
      base.Save(e);
    }

    public override bool CanSave(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSave(e);
    }

    public virtual void ShowExistingForms(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      DirRX.LocalActs.PublicFunctions.StandardForm.Remote.FindStandardForms(_obj.DocumentKind).Where(x => !StandardForms.Equals(_obj, x) && x.Status == StandardForm.Status.Active).Show();
    }

    public virtual bool CanShowExistingForms(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

}