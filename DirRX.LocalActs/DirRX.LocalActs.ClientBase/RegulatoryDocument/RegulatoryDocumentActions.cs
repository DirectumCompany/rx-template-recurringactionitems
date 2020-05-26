using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.RegulatoryDocument;

namespace DirRX.LocalActs.Client
{

  partial class RegulatoryDocumentActions
  {
    public virtual void SpreadsOnClean(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      while (_obj.SpreadsOn.Any(x => !(Sungero.Company.Departments.Is(x.Recepient) ||
                                       Sungero.CoreEntities.Roles.Is(x.Recepient) ||
                                       Solution.BusinessUnits.Is(x.Recepient)) ||
                                x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.Administrators ||
                                x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.Auditors ||
                                x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.ConfigurationManagers ||
                                x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.ServiceUsers ||
                                x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.SoloUsers ||
                                x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.DeliveryUsersSid ||
                                x.Recepient.Sid == Sungero.Projects.PublicConstants.Module.RoleGuid.ParentProjectTeam ||
                                x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.AllUsers))
        _obj.SpreadsOn.Remove(_obj.SpreadsOn.First(x => !(Sungero.Company.Departments.Is(x.Recepient) ||
                                                          Sungero.CoreEntities.Roles.Is(x.Recepient) ||
                                                          Solution.BusinessUnits.Is(x.Recepient)) ||
                                                   x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.Administrators ||
                                                   x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.Auditors ||
                                                   x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.ConfigurationManagers ||
                                                   x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.ServiceUsers ||
                                                   x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.SoloUsers ||
                                                   x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.DeliveryUsersSid ||
                                                   x.Recepient.Sid == Sungero.Projects.PublicConstants.Module.RoleGuid.ParentProjectTeam ||
                                                   x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.AllUsers));
    }

    public virtual bool CanSpreadsOnClean(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }


    public override void ShowRelatedDocuments(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.ShowRelatedDocuments(e);
    }

    public override bool CanShowRelatedDocuments(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanShowRelatedDocuments(e);
    }

  }

}