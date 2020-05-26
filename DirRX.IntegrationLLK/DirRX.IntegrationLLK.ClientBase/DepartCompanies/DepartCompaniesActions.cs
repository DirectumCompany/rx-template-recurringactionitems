using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.IntegrationLLK.DepartCompanies;

namespace DirRX.IntegrationLLK.Client
{
  partial class DepartCompaniesActions
  {
    public virtual void ShowContacts(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.DepartCompanies.Remote.GetContactsFromSubdivision(_obj).Show();
    }

    public virtual bool CanShowContacts(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

}