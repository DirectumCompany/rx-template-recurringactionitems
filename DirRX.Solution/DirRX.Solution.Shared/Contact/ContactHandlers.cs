using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Contact;

namespace DirRX.Solution
{
  partial class ContactSharedHandlers
  {

    public override void CompanyChanged(Sungero.Parties.Shared.ContactCompanyChangedEventArgs e)
    {      
      base.CompanyChanged(e);
      if (e.NewValue != e.OldValue)
        _obj.Subdivision = null;
    }

  }
}