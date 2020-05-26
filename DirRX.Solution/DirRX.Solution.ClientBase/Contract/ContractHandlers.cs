using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Contract;

namespace DirRX.Solution
{
  partial class ContractTrackingClientHandlers
  {

    public override IEnumerable<Enumeration> TrackingActionFiltering(IEnumerable<Enumeration> query)
    {
      query = base.TrackingActionFiltering(query);
      return query.Where(a => a != ContractTracking.Action.OriginalSend);
    }
  }


  partial class ContractClientHandlers
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

    public virtual void IsTenderValueInput(Sungero.Presentation.BooleanValueInputEventArgs e)
    {
      if (e.NewValue != e.OldValue && _obj.HasVersions)
        e.AddWarning(DirRX.Solution.Contracts.Resources.IsTenderChanged);
    }

    public virtual IEnumerable<Enumeration> ContractFunctionalityFiltering(IEnumerable<Enumeration> query)
    {
      if (_obj.IsTender == true)
        return query.Where(f => f != ContractFunctionality.Mixed);
      
      return query;
    }

    public virtual IEnumerable<Enumeration> TenderStepFiltering(IEnumerable<Enumeration> query)
    {
      if (_obj.IsTender == true && _obj.ContractFunctionality == DirRX.Solution.Contract.ContractFunctionality.Purchase)
        query = query.Where(x => x != DirRX.Solution.Contract.TenderStep.AppWithChanges);
      return query;
    }

    public virtual void TransactionAmountValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
    {
      if (e.NewValue < 0)
        e.AddError(DirRX.Solution.Contracts.Resources.PositiveAmount);
    }

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      base.Showing(e);
      Functions.Contract.ChangeDocumentProperties(_obj);
      
      // Скрыть кнопки "Создать из файла", "Создать со сканера" для типового договора.
      if (_obj.IsStandard == true && _obj.InternalApprovalState != InternalApprovalState.Signed)
      {
      	e.HideAction(_obj.Info.Actions.CreateFromFile);
      	e.HideAction(_obj.Info.Actions.CreateFromScanner);
      }
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      Functions.Contract.ChangeDocumentProperties(_obj);
      Functions.Contract.CheckValidTillState(_obj);

      if (_obj.HolderTZ == DirRX.Solution.Contract.HolderTZ.Third)
        e.AddInformation(DirRX.Solution.Contracts.Resources.OpenProductNameHint, _obj.Info.Actions.OpenProductName);
      
      var isManyCounterparties = _obj.IsManyCounterparties.GetValueOrDefault();
      _obj.State.Properties.Counterparties.IsEnabled = isManyCounterparties;
      
      _obj.State.Properties.Counterparty.IsEnabled = !isManyCounterparties;
      _obj.State.Properties.CounterpartySignatory.IsEnabled = !isManyCounterparties;
      _obj.State.Properties.Contact.IsEnabled = !isManyCounterparties;
      _obj.State.Properties.ShippingAddress.IsEnabled = !isManyCounterparties;
      _obj.State.Properties.DeliveryMethod.IsEnabled = !isManyCounterparties;
      //_obj.State.Properties.ShippingAddress.IsRequired = !isManyCounterparties;
      //_obj.State.Properties.DeliveryMethod.IsRequired = !isManyCounterparties;
      _obj.State.Properties.TenderType.IsVisible = _obj.IsTender.Value && _obj.ContractFunctionality != ContractFunctionality.Sale;
      _obj.State.Properties.ValidTill.IsEnabled = !_obj.IsStandard.Value;
      
      _obj.State.Properties.Subcategory.IsRequired = _obj.IsStandard.Value && _obj.DocumentGroup != null && _obj.DocumentGroup.CounterpartySubcategories.Any();
    }
  }

}