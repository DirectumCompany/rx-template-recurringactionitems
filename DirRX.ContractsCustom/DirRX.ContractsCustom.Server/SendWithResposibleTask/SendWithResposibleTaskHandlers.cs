﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.SendWithResposibleTask;

namespace DirRX.ContractsCustom
{
  partial class SendWithResposibleTaskServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.Subject = DirRX.ContractsCustom.SendWithResposibleTasks.Resources.TaskSubject;
    }
  }

}