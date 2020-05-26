using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProcessSubstitutionModule.SubstituteConnection;

namespace DirRX.ProcessSubstitutionModule
{
  partial class SubstituteConnectionServerHandlers
  {

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      if (_obj.NeedUpdateSubtitution.HasValue && _obj.NeedUpdateSubtitution.Value)
        DirRX.ProcessSubstitutionModule.Jobs.CreateSystemSubstitutions.Enqueue();
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      _obj.Name = string.Format(DirRX.ProcessSubstitutionModule.SubstituteConnections.Resources.DefaultConnectionName, _obj.ProcessSubstitutionID);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.Name = DirRX.ProcessSubstitutionModule.SubstituteConnections.Resources.DefaultConnectionName;
    }
  }

}