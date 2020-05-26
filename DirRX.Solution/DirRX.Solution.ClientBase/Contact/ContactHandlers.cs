using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Contact;

namespace DirRX.Solution
{
  partial class ContactClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);
      
      // Скрыть действие "Добавить сотрудника" от всех, кроме Администраторов.
      var currentUser = Users.Current;
      var groupAdministrators = Roles.Administrators;
      if (!currentUser.IncludedIn(groupAdministrators))
        e.HideAction(_obj.Info.Actions.CreateEmployee);
    }
  }
}