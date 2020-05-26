using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.SendNoticeQueueItem;

namespace DirRX.ActionItems
{
  partial class SendNoticeQueueItemServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      _obj.NoticeIsSend = false;
    }
  }

}