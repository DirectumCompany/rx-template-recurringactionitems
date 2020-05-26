using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Brands.BrandsRegistration;

namespace DirRX.Brands.Client
{
  partial class BrandsRegistrationCollectionActions
  {

    public virtual bool CanDeleteRegistrations(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _objs.Any();
    }

    public virtual void DeleteRegistrations(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_objs.Any())
      {
        var registrations = _objs.ToList();
        int selectedCount = registrations.Count;
        int deletedCount = Functions.BrandsRegistration.Remote.DeleteRegistrations(registrations);
        string message = deletedCount == selectedCount ? 
          BrandsRegistrations.Resources.SelectedRegistrationsDeletedSuccessfully :
          BrandsRegistrations.Resources.SelectedRegistrationsDeletedPartiallyFormat(deletedCount.ToString(), selectedCount.ToString());
        Dialogs.ShowMessage(message);
      }
    }
  }

  internal static class BrandsRegistrationStaticActions
  {

    public static bool CanRegistrImportXlsx(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public static void RegistrImportXlsx(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Module.BrandRegistrImport();
    }
  }

}