using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Brands.BrandsRegistration;

namespace DirRX.Brands.Server
{
  partial class BrandsRegistrationFunctions
  {

    /// <summary>
    /// Удалить выбранные регистрации.
    /// </summary>
    [Remote]
    public static int DeleteRegistrations(List<IBrandsRegistration> registrations)
    {
      int deleted = 0;
      foreach (IBrandsRegistration registr in registrations)
      {
        var lockInfo = Locks.GetLockInfo(registr);
        if (!lockInfo.IsLocked)
        {
          BrandsRegistrations.Delete(registr);
          deleted++;
        }
        else
          Logger.Error(BrandsRegistrations.Resources.SelectedRegistrationsDeleteLockFormat(lockInfo.OwnerName, lockInfo.LockTime.ToString("g")));
      }
      
      return deleted;
    }

  }
}