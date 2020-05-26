using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.NoticeSetting;

namespace DirRX.ActionItems.Client
{
  partial class NoticeSettingActions
  {
    public virtual void GetSameSetting(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var setting = Functions.NoticeSetting.Remote.GetSameSetting(_obj);
      setting.Show();
    }

    public virtual bool CanGetSameSetting(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

}