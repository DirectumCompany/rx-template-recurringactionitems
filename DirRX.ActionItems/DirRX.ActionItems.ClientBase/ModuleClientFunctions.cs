using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ActionItems.Client
{
  public class ModuleFunctions
  {
    
    /// <summary>
    /// Создать новую настройку уведомлений.
    /// </summary>
    public virtual void CreateAndShowNoticeSettingWithDialog()
    {
      var dialog = Dialogs.CreateInputDialog(Resources.AddSubscriberDialogTitle, Resources.AddSubscriberPropertyTitle);
      var possibleRoles = DirRX.ActionItems.PublicFunctions.ActionItemsRole.Remote.GetPossibleRolesForNotices();
      var roleButton = dialog.AddSelect(string.Empty, false, ActionItemsRoles.Null).From(possibleRoles);
      dialog.Buttons.AddOkCancel();
      dialog.Buttons.Default = DialogButtons.Ok;
      if (dialog.Show() == DialogButtons.Ok && roleButton.Value != null)
        Functions.Module.Remote.CreateNoticeSetting(roleButton.Value).Show();
    }

    /// <summary>
    /// Создать новое поручение.
    /// </summary>
    public virtual void CreateAndShowAssignment()
    {
      Functions.Module.Remote.CreateAssignment().Show();
    }

  }
}