using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.RevisionRequest;

namespace DirRX.PartiesControl
{
  partial class RevisionRequestClientHandlers
  {

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      if (_obj.CheckingType != null && !string.IsNullOrEmpty(_obj.CheckingType.MessageText))
        e.AddInformation(_obj.CheckingType.MessageText, _obj.Info.Actions.OpenDocumentLink);
      if (_obj.Counterparty != null)
      {
        if (_obj.Counterparty.CounterpartyStatus != null && _obj.Counterparty.CounterpartyStatus.Sid == PartiesControl.PublicConstants.CounterpartyStatus.DefaultStatus.StopListSid)
        {
          var stopListItem = _obj.Counterparty.StoplistHistory.FirstOrDefault(s => !s.ExcludeDate.HasValue);
          e.AddError(DirRX.PartiesControl.RevisionRequests.Resources.StopListMessageFormat(stopListItem.Reason.Name, stopListItem.IncludeDate.Value.ToString("d")));
        }
        else
        {
          if (_obj.Counterparty.CounterpartyStatus != null && _obj.Counterparty.CounterpartyStatus.Sid == PartiesControl.PublicConstants.CounterpartyStatus.DefaultStatus.CheckingRequiredSid)
            e.AddInformation(DirRX.PartiesControl.RevisionRequests.Resources.CouterpartyStatusMessageWithoutDateFormat(_obj.Counterparty.CounterpartyStatus));
          else
            e.AddInformation(DirRX.PartiesControl.RevisionRequests.Resources
                             .CounterpartyStatusMessageFormat(_obj.Counterparty.CounterpartyStatus,
                                                              _obj.Counterparty.CheckingDate.HasValue ? _obj.Counterparty.CheckingDate.Value.Date.ToString("d") : string.Empty,
                                                              _obj.Counterparty.CheckingValidDate.HasValue ? _obj.Counterparty.CheckingValidDate.Value.Date.ToString("d") : string.Empty));
        }
      }
      
      Functions.RevisionRequest.SetEnabledProperties(_obj);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {      
      base.Showing(e);
      _obj.State.Properties.SecurityComment.IsVisible = Solution.Employees.Current != null &&
        (DirRX.ActionItems.PublicFunctions.ActionItemsRole.Remote.IsCEO(Solution.Employees.Current) ||
         Solution.Employees.Equals(_obj.PreparedBy, Solution.Employees.Current) ||
         Solution.Employees.Equals(_obj.Supervisor, Solution.Employees.Current) ||
         Solution.Employees.Current.IncludedIn(Constants.Module.SecurityServiceRole));
      
      _obj.State.Properties.CheckingType.IsRequired = true;
      _obj.State.Properties.CheckingReason.IsRequired = true;
    }

  }
}