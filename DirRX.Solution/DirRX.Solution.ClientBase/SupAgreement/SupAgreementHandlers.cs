using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.SupAgreement;

namespace DirRX.Solution
{
  partial class SupAgreementTrackingClientHandlers
  {

    public override IEnumerable<Enumeration> TrackingActionFiltering(IEnumerable<Enumeration> query)
    {
      query = base.TrackingActionFiltering(query);
      return query.Where(a => a != ContractTracking.Action.OriginalSend);
    }
  }

  partial class SupAgreementClientHandlers
  {

    public virtual void DocumentValidityValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue <= 0)
        e.AddError(DirRX.ActionItems.Resources.ValueMustBePositive);
    }

    public override IEnumerable<Enumeration> LifeCycleStateFiltering(IEnumerable<Enumeration> query)
    {
      query = base.LifeCycleStateFiltering(query);
      return query.Where(d => !Equals(d, LifeCycleState.Terminated));
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);
      Functions.SupAgreement.ChangeDocumentProperties(_obj);
      //_obj.State.Properties.TransactionAmount.IsRequired = true;
      
      // Скрыть кнопки "Создать из файла", "Создать со сканера" для типового ДС.
      if (_obj.IsStandard == true && _obj.InternalApprovalState != InternalApprovalState.Signed)
      {
      	e.HideAction(_obj.Info.Actions.CreateFromFile);
      	e.HideAction(_obj.Info.Actions.CreateFromScanner);
      }
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      _obj.State.Properties.DeliveryMethod.IsVisible = true;
      Functions.SupAgreement.ChangeDocumentProperties(_obj);
      //Functions.SupAgreement.CheckValidTillState(_obj);
      
      var isManyCounterparties = _obj.IsManyCounterparties.GetValueOrDefault();
      _obj.State.Properties.Counterparties.IsEnabled = isManyCounterparties;
      
      _obj.State.Properties.Counterparty.IsEnabled = !isManyCounterparties;
      _obj.State.Properties.CounterpartySignatory.IsEnabled = !isManyCounterparties;
      _obj.State.Properties.Contact.IsEnabled = !isManyCounterparties;
      _obj.State.Properties.ShippingAddress.IsEnabled = !isManyCounterparties;
      _obj.State.Properties.DeliveryMethod.IsEnabled = !isManyCounterparties;
      //_obj.State.Properties.ShippingAddress.IsRequired = !isManyCounterparties;
      //_obj.State.Properties.DeliveryMethod.IsRequired = !isManyCounterparties;
      _obj.State.Properties.ValidTill.IsEnabled = !_obj.IsStandard.Value;
    }
  }

}