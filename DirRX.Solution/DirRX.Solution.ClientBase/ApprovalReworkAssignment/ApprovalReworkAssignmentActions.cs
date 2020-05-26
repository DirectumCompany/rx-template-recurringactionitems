using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalReworkAssignment;

namespace DirRX.Solution.Client
{
  partial class ApprovalReworkAssignmentActions
  {
    public virtual void AddSubscribers(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var subscribers = DirRX.Solution.PublicFunctions.Module.GetSelectedEmployees(_obj.Subscribers.Select(s => s.Subscriber).ToList());
      if (subscribers.Any())
      {
        var isSend = DirRX.ActionItems.PublicFunctions.SendNoticeQueueItem.Remote.CreateSendNoticeQueueItem(subscribers.ToList(), null, _obj);
        if (isSend)
        {
          foreach (var subscriber in subscribers)
          {
            var newSubscriber = _obj.Subscribers.AddNew();
            newSubscriber.Subscriber = subscriber;
          }
          
          _obj.Save();
        }
        else
          e.AddError(DirRX.ActionItems.Resources.AddSubscriberError);
      }
    }

    public virtual bool CanAddSubscribers(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.AccessRights.CanUpdate() && _obj.Status == Status.InProcess;
    }

  }

}