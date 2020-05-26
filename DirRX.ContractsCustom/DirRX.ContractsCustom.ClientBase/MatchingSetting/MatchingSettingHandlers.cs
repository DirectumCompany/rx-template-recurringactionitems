using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.MatchingSetting;

namespace DirRX.ContractsCustom
{
  partial class MatchingSettingClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
    	Functions.MatchingSetting.SetRequiredProperties(_obj);
    }
    
  }
}