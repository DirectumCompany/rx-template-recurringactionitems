using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Contract;
using DirRX.ContractsCustom;

namespace DirRX.Solution
{
  partial class ContractTrackingSharedCollectionHandlers
  {

    public override void TrackingDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e)
    {
      // Убрали проверку.
    }
  }

  partial class ContractCounterpartiesSharedCollectionHandlers
  {

    public virtual void CounterpartiesDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e)
    {
      Functions.Contract.SelectStandartFrom(_obj);
    }

    public virtual void CounterpartiesAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.Number = (_obj.Counterparties.Max(a => a.Number) ?? 0) + 1;
    }
  }

  partial class ContractCounterpartiesSharedHandlers
  {

    public virtual void CounterpartiesContactChanged(DirRX.Solution.Shared.ContractCounterpartiesContactChangedEventArgs e)
    {
      if (e.NewValue != null && _obj.Counterparty == null)
        _obj.Counterparty = DirRX.Solution.Companies.As(e.NewValue.Company);
    }

    public virtual void CounterpartiesDeliveryMethodChanged(DirRX.Solution.Shared.ContractCounterpartiesDeliveryMethodChangedEventArgs e)
    {
      /*if (e.NewValue != null && e.NewValue != e.OldValue)
        _obj.State.Properties.Contact.IsRequired = e.NewValue.IsRequireContactInContract.GetValueOrDefault();*/
    }

    public virtual void CounterpartiesSignatoryChanged(DirRX.Solution.Shared.ContractCounterpartiesSignatoryChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue && _obj.Counterparty == null)
        _obj.Counterparty = DirRX.Solution.Companies.As(e.NewValue.Company);
    }

    public virtual void CounterpartiesCounterpartyChanged(DirRX.Solution.Shared.ContractCounterpartiesCounterpartyChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue)
      {
        _obj.Address = e.NewValue.PostalShippingAddress;
        _obj.DeliveryMethod = e.NewValue.DeliveryMethod;
        _obj.Contact = null;
        
        Functions.Contract.SelectStandartFrom(_obj.Contract);
        Functions.Contract.UpdateAnalysisRequired(DirRX.Solution.Contracts.As(_obj.RootEntity));
      }
      
      if (e.NewValue == null)
      {
        _obj.Address = null;
        _obj.DeliveryMethod = null;
        _obj.Contact = null;
      }
    }
  }


  partial class ContractOtherDocumentsSharedHandlers
  {

    public virtual void OtherDocumentsDocumentChanged(DirRX.Solution.Shared.ContractOtherDocumentsDocumentChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        var document = Contracts.As(_obj.RootEntity);
        var oldRelatedDocs = document.Relations.GetRelated();
        if (e.OldValue != null && oldRelatedDocs.Any(d => Sungero.Content.IElectronicDocument.Equals(e.OldValue, d)))
          document.Relations.Remove(Constants.Module.SimpleRelationRelationName, e.OldValue);
        if (e.NewValue != null && !oldRelatedDocs.Any(d => Sungero.Content.IElectronicDocument.Equals(e.NewValue, d)))
          document.Relations.Add(Constants.Module.SimpleRelationRelationName, e.NewValue);
      }
    }
  }

  partial class ContractOtherDocumentsSharedCollectionHandlers
  {
    public virtual void OtherDocumentsDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e)
    {
      if (_deleted.Document != null)
      {
        var oldRelatedDocs = _obj.Relations.GetRelated();
        if (oldRelatedDocs.Any(d => Sungero.Content.IElectronicDocument.Equals(_deleted.Document, d)))
          _obj.Relations.Remove(Constants.Module.SimpleRelationRelationName, _deleted.Document);
      }
    }
  }

  partial class ContractRequiredDocumentsSharedHandlers
  {

    public virtual void RequiredDocumentsDocumentChanged(DirRX.Solution.Shared.ContractRequiredDocumentsDocumentChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        var document = Contracts.As(_obj.RootEntity);
        var oldRelatedDocs = document.Relations.GetRelated();
        if (e.OldValue != null && oldRelatedDocs.Any(d => Sungero.Content.IElectronicDocument.Equals(e.OldValue, d)))
          // Если вид документа - Заключение об экономической оправданности сделки, то используется тип связи "Приложение".
          if (_obj.DocumentKind == DirRX.Solution.Constants.Module.PurchaseDocKind)
            document.Relations.Remove(Constants.Module.AddendumRelationName, e.OldValue);
          else
            document.Relations.Remove(Constants.Module.SimpleRelationRelationName, e.OldValue);
        if (e.NewValue != null && !oldRelatedDocs.Any(d => Sungero.Content.IElectronicDocument.Equals(e.NewValue, d)))
          // Если вид документа - Заключение об экономической оправданности сделки, то используется тип связи "Приложение".
          if (_obj.DocumentKind == DirRX.Solution.Constants.Module.PurchaseDocKind)
            document.Relations.Add(Constants.Module.AddendumRelationName, e.NewValue);
          else
            document.Relations.Add(Constants.Module.SimpleRelationRelationName, e.NewValue);
      }
    }
  }

  partial class ContractRequiredDocumentsSharedCollectionHandlers
  {

    public virtual void RequiredDocumentsDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e)
    {
      if (_deleted.Document != null)
      {
        var oldRelatedDocs = _obj.Relations.GetRelated();
        if (oldRelatedDocs.Any(d => Sungero.Content.IElectronicDocument.Equals(_deleted.Document, d)))
          _obj.Relations.Remove(Constants.Module.SimpleRelationRelationName, _deleted.Document);
      }
    }
  }

  partial class ContractTrackingSharedHandlers
  {

    public override void TrackingReturnResultChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      base.TrackingReturnResultChanged(e);
      
      if (e.NewValue != e.OldValue && e.NewValue == ContractTracking.ReturnResult.NotSigned && _obj.IsOriginal == false)
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(DirRX.Solution.Contracts.As(_obj.OfficialDocument),
                                                                              DirRX.ContractsCustom.PublicConstants.Module.ContractStatusGuid.CounterpartyRejectedSigningGuid,
                                                                              DirRX.ContractsCustom.PublicConstants.Module.ContractStatusType.ScanMoveStatus,
                                                                              true);
    }

    public override void TrackingActionChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      base.TrackingActionChanged(e);
      
      if (_obj.Action == ContractTracking.Action.OriginalSend)
      {
        var constant = DirRX.ContractsCustom.PublicFunctions.Module.Remote.GetContractConstant(DirRX.ContractsCustom.PublicConstants.Module.OriginalDeadlineGuid.ToString());
        if (constant != null && constant.Period.HasValue)
        {
          DateTime? returnDeadline = null;
          if (constant.Unit == ContractsCustom.ContractConstant.Unit.Day)
            returnDeadline = Calendar.Today.AddDays(constant.Period.Value);
          if (constant.Unit == ContractsCustom.ContractConstant.Unit.Month)
            returnDeadline = Calendar.Today.AddMonths(constant.Period.Value);
          if (constant.Unit == ContractsCustom.ContractConstant.Unit.Year)
            returnDeadline = Calendar.Today.AddYears(constant.Period.Value);
          
          if (returnDeadline != null)
            _obj.ReturnDeadline = returnDeadline;
        }
      }
    }
  }

  partial class ContractSharedHandlers
  {

    public override void ExternalApprovalStateChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      base.ExternalApprovalStateChanged(e);
      
      if (e.NewValue != e.OldValue)
      {
        if (e.NewValue == ExternalApprovalState.Signed)
          _obj.CounterpartyApprovalState = CounterpartyApprovalState.Signed;
        else if (e.NewValue == ExternalApprovalState.Unsigned)
          _obj.CounterpartyApprovalState = CounterpartyApprovalState.Unsigned;
      }
    }

    public virtual void CounterpartyApprovalStateChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        var tracking = _obj.Tracking.Where(t => t.Action == Solution.ContractTracking.Action.OriginalSend && t.IsOriginal == true).FirstOrDefault();
        var curEmployee = Employees.Current;
        
        if (e.NewValue == Solution.Contract.CounterpartyApprovalState.Signed && curEmployee != null)
        {
          if (_obj.ExternalApprovalState != ExternalApprovalState.Signed)
            _obj.ExternalApprovalState = ExternalApprovalState.Signed;
          
          var deadline = Functions.Module.Remote.GetDeadlineConstantValue();
          if (tracking == null)
          {
            Solution.IContractTracking issue = _obj.Tracking.AddNew() as IContractTracking;
            issue.Action = Solution.ContractTracking.Action.OriginalSend;
            issue.DeliveredTo = curEmployee;
            issue.ReturnDeadline = Calendar.UserToday.AddWorkingDays(deadline);
            issue.IsOriginal = true;
            issue.Format = Solution.ContractTracking.Format.Original;
            // Иначе идет отправка задачи
            issue.ExternalLinkId = 0;
          }
          else
          {
            Solution.IContractTracking issue = tracking as IContractTracking;
            issue.ReturnDeadline = Calendar.UserToday.AddWorkingDays(deadline);
            issue.Format = Solution.ContractTracking.Format.Original;
          }
        }
        
        if (e.NewValue == Solution.Contract.CounterpartyApprovalState.Unsigned && tracking != null)
          tracking.ReturnResult = Solution.ContractTracking.ReturnResult.NotSigned;
      }
    }

    public virtual void IsAnalysisRequiredChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue == true)
      {
        if (!_obj.RequiredDocuments.Any(r => r.DocumentKind == Constants.Module.AnalysisDocKind))
          _obj.RequiredDocuments.AddNew().DocumentKind = Constants.Module.AnalysisDocKind;
      }
      else
      {
        while (_obj.RequiredDocuments.Any(r => r.DocumentKind == Constants.Module.AnalysisDocKind && r.Document == null))
          _obj.RequiredDocuments.Remove(_obj.RequiredDocuments.First(r => r.DocumentKind == Constants.Module.AnalysisDocKind && r.Document == null));
      }
    }

    public virtual void IDSapChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
        this.SyncCounterparties();
    }

    public virtual void ShippingAddressChanged(DirRX.Solution.Shared.ContractShippingAddressChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
        this.SyncCounterparties();
    }

    public override void ContactChanged(Sungero.Contracts.Shared.ContractualDocumentContactChangedEventArgs e)
    {
      base.ContactChanged(e);
      if (e.NewValue != e.OldValue)
        this.SyncCounterparties();
    }

    public override void CounterpartySignatoryChanged(Sungero.Docflow.Shared.ContractualDocumentBaseCounterpartySignatoryChangedEventArgs e)
    {
      base.CounterpartySignatoryChanged(e);
      
      if (e.NewValue != e.OldValue)
        this.SyncCounterparties();
    }

    public override void ResponsibleEmployeeChanged(Sungero.Contracts.Shared.ContractualDocumentResponsibleEmployeeChangedEventArgs e)
    {
      base.ResponsibleEmployeeChanged(e);
      if (e.NewValue != null && e.OldValue != e.NewValue)
      {
        _obj.Department = e.NewValue.Department;
        
        if (_obj.DocumentGroup != null && _obj.DocumentGroup.IsSupervisorFunctionManager.GetValueOrDefault())
          _obj.Supervisor = DirRX.ActionItems.PublicFunctions.ActionItemsRole.Remote.GetInitCEOManager(DirRX.Solution.Employees.As(e.NewValue));
      }
    }

    public virtual void IsManyCounterpartiesChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (_obj.IsManyCounterparties == true)
      {
        Functions.Contract.ClearAndFillFirstCounterparty(_obj);
        
        _obj.Counterparty = DirRX.Solution.Companies.As(Sungero.Parties.PublicFunctions.Counterparty.Remote.GetDistributionListCounterparty());
        _obj.CounterpartySignatory = null;
        _obj.Contact = null;
        _obj.DeliveryMethod = null;
        _obj.ShippingAddress = null;
        _obj.IDSap = null;
      }
      else if(_obj.IsManyCounterparties == false)
      {
        var counterparty = _obj.Counterparties.OrderBy(a => a.Number).FirstOrDefault(a => a.Counterparty != null);
        if (counterparty != null)
        {
          _obj.Counterparty = counterparty.Counterparty;
          _obj.CounterpartySignatory = counterparty.Signatory;
          _obj.Contact = counterparty.Contact;
          _obj.DeliveryMethod = counterparty.DeliveryMethod;
          _obj.ShippingAddress = counterparty.Address;
          _obj.IDSap = counterparty.IDSap;
        }
        else
        {
          _obj.Counterparty = null;
          _obj.CounterpartySignatory = null;
          _obj.Contact = null;
          _obj.DeliveryMethod = null;
          _obj.ShippingAddress = null;
          _obj.IDSap = null;
        }
        
        Functions.Contract.ClearAndFillFirstCounterparty(_obj);
      }
    }

    public override void RegistrationNumberChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      base.RegistrationNumberChanged(e);
      
      // Установить статус договора "Ожидает отправки контрагенту".
      Functions.Contract.SetStatusOriginalWaitingForSending(_obj);
    }
    
    public override void ValidFromChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.ValidFromChanged(e);
      if (e.NewValue != e.OldValue)
      {
        Functions.Contract.UpdateLukoilApproving(_obj);
        Functions.Contract.UpdateAnalysisRequired(_obj);
      }
    }

    public virtual void SubcategoryChanged(DirRX.Solution.Shared.ContractSubcategoryChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue)
        Functions.Contract.SelectStandartFrom(_obj);
    }

    public override void DocumentKindChanged(Sungero.Docflow.Shared.OfficialDocumentDocumentKindChangedEventArgs e)
    {
      base.DocumentKindChanged(e);
      if (e.NewValue != null && e.NewValue != e.OldValue)
        Functions.Contract.SelectStandartFrom(_obj);
    }

    public override void IsStandardChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      base.IsStandardChanged(e);
      if (e.NewValue != null && e.NewValue != e.OldValue)
      {
        // Закрыть от редактирования поле "Действует по", если договор типовой.
        _obj.State.Properties.ValidTill.IsEnabled = !_obj.IsStandard.Value;
        
        _obj.State.Properties.Subcategory.IsRequired = _obj.IsStandard.Value && _obj.DocumentGroup != null && _obj.DocumentGroup.CounterpartySubcategories.Any();
        
        Functions.Contract.SelectStandartFrom(_obj);
      }
    }

    public virtual void StandartFormChanged(DirRX.Solution.Shared.ContractStandartFormChangedEventArgs e)
    {
      if (e.NewValue == e.OldValue || e.NewValue == null)
        return;
      
      var contractSetting = e.NewValue;
      
      _obj.ContractActivate = contractSetting.ContractActivate;
      _obj.IsAnalysisRequired = contractSetting.IsAnalysisRequired;
      
      // Очистить только строки, в которых не заполнен документ.
      while (_obj.RequiredDocuments.Any(r => r.Document == null))
        _obj.RequiredDocuments.Remove(_obj.RequiredDocuments.First(r => r.Document == null));
      
      foreach (var row in contractSetting.BindingDocument.Where(d => _obj.IsTender == true || d.DocumentsForTender == _obj.IsTender ))
      {
        if (!_obj.RequiredDocuments.Any(r => r.DocumentKind == row.DocumentKind))
          _obj.RequiredDocuments.AddNew().DocumentKind = row.DocumentKind;
      }
      if (_obj.HolderTZ == DirRX.Solution.Contract.HolderTZ.Third)
      {
        if (!_obj.RequiredDocuments.Any(r => r.DocumentKind == Constants.Module.TrademarkDocKind))
          _obj.RequiredDocuments.AddNew().DocumentKind = Constants.Module.TrademarkDocKind;
      }
      
      if (_obj.ContractFunctionality != null && _obj.ContractFunctionality == DirRX.Solution.Contract.ContractFunctionality.Purchase
          && !_obj.RequiredDocuments.Any(r => r.DocumentKind == Constants.Module.PurchaseDocKind))
        _obj.RequiredDocuments.AddNew().DocumentKind = Constants.Module.PurchaseDocKind;
      
      Functions.Contract.UpdateLukoilApproving(_obj);
      
      if (contractSetting.IsAnalysisRequired == true)
        Functions.Contract.UpdateAnalysisRequired(_obj);
      else
        _obj.IsAnalysisRequired = false;
    }

    public override void InternalApprovalStateChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      base.InternalApprovalStateChanged(e);
      
      if (e.NewValue != e.OldValue && e.NewValue == Contract.InternalApprovalState.OnApproval)
      {
        ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(_obj,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusGuid.OnReworkGuid,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus);
        ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(_obj,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusGuid.RejectedGuid,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus);
        // Установка статуса "На согласовании".
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.OnApprovingGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus,
                                                                              false);
      }
      
      if (e.NewValue != e.OldValue && e.NewValue == Contract.InternalApprovalState.OnRework)
      {
        ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(_obj,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusGuid.OnApprovingGuid,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus);
        // Установка статуса "На доработке".
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.OnReworkGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus,
                                                                              false);
      }
      
      if (e.NewValue != e.OldValue && e.NewValue == Contract.InternalApprovalState.PendingSign)
      {
        ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(_obj,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusGuid.OnApprovingGuid,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus);
        // Установка статуса "Согласован".
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.ApprovedGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus,
                                                                              false);
        // Установка статуса "Передан Подписанту на подтверждение в электронном виде".
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.SendedToSignerGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus,
                                                                              false);
      }
      
      if (e.NewValue != e.OldValue && e.NewValue == Contract.InternalApprovalState.Signed)
      {
        ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(_obj,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusGuid.SendedToSignerGuid,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus);
        ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(_obj,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusGuid.ApprovedGuid,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus);
        // Установка статуса "Подтвержден Подписантом в электронном виде".
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.SignerAcceptedGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus,
                                                                              false);
      }
      
      if (e.NewValue != e.OldValue && e.NewValue == Contract.InternalApprovalState.Aborted)
      {
        // Установить статус "Отказ от заключения договора".
       ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.RejectedGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus,
                                                                              true);
      }
    }

    public override void CaseFileChanged(Sungero.Docflow.Shared.OfficialDocumentCaseFileChangedEventArgs e)
    {
      base.CaseFileChanged(e);
      
      // Установка статуса "Документ помещен в архив".
     if (e.NewValue != null && e.NewValue != e.OldValue && _obj.PlacedToCaseFileDate.HasValue)
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalArchivedGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus,
                                                                              true);
    }

    public override void PlacedToCaseFileDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.PlacedToCaseFileDateChanged(e);
      
      // Установка статуса "Документ помещен в архив".
       if (e.NewValue.HasValue && e.NewValue != e.OldValue && _obj.CaseFile != null)
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalArchivedGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus,
                                                                              true);
    }

    public virtual void ContractorOriginalSigningChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      // Установка статуса "Получен оригинал, подписанный Контрагентом".
    if (e.NewValue != e.OldValue && e.NewValue == Contract.ContractorOriginalSigning.Signed && _obj.ExternalApprovalState == Contract.ExternalApprovalState.Signed)
      {
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalReceivedFromCounterpartyGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus,
                                                                              false);
        _obj.CounterpartyApprovalState = CounterpartyApprovalState.Signed;
      }
      
      // Установка статуса "Контрагент вернул неподписанный документ".
      if (e.NewValue != e.OldValue && e.NewValue == Contract.ContractorOriginalSigning.NotSigned && _obj.ExternalApprovalState == Contract.ExternalApprovalState.Unsigned)
      {
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalReceivedNonSignedGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus,
                                                                              false);
        _obj.CounterpartyApprovalState = CounterpartyApprovalState.Unsigned;
      }
      
      if (e.NewValue != e.OldValue && e.NewValue == Contract.ContractorOriginalSigning.Signed && _obj.OriginalSigning == Contract.OriginalSigning.Signed)
      {
        ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(_obj,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalSignedByBusinessUnitGuid,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus);
        ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(_obj,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalReceivedFromCounterpartyGuid,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus);
        // Установка статуса "Оригиналы подписаны всеми сторонами".
       ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalSignedByAllSidesGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus,
                                                                              false);
        
      }
    }

    public virtual void OriginalSigningChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue && e.NewValue == Contract.OriginalSigning.Signed)
      {
        // Удалить статус "Оригинал передан на подписание в Обществе".
   ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(_obj,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalSendedBusinessUnitForSigningGuid,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus);
        // Установить статус договора "Ожидает отправки контрагенту".
        Functions.Contract.SetStatusOriginalWaitingForSending(_obj);
        
        if (_obj.ContractorOriginalSigning == Contract.ContractorOriginalSigning.Signed)
        {
          ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(_obj,
                                                                                   ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalReceivedFromCounterpartyGuid,
                                                                                   ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus);
          // Установка статуса "Оригиналы подписаны всеми сторонами".
           ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                                ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalSignedByAllSidesGuid,
                                                                                ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus,
                                                                                false);
        }
        else
        {
          // Установка статуса "Оригинал подписан в Обществе".
           ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                                ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalSignedByBusinessUnitGuid,
                                                                                ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus,
                                                                                false);
        }
      }
      
      if (e.NewValue != e.OldValue && e.NewValue == Contract.OriginalSigning.NotSigned)
      {
        ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(_obj,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalSendedBusinessUnitForSigningGuid,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus);
        // Установка статуса "Подписант отказался подписать документ".
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.SignerRejectedSigningGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus,
                                                                              false);
      }
      
      if (e.NewValue != e.OldValue && e.NewValue == Contract.OriginalSigning.OnSigning)
      {
        // Установка статуса "Оригинал передан на подписание в Обществе".
         ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalSendedBusinessUnitForSigningGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus,
                                                                              false);
      }
    }

    public virtual void LukoilApprovingChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue && e.NewValue == Contract.LukoilApproving.Required)
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.LukoilApprovedGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus,
                                                                              false);
      
      if (e.NewValue != e.OldValue && e.NewValue == Contract.LukoilApproving.Received)
        ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(_obj,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusGuid.LukoilApprovedGuid,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus);
    }

    public override void CurrencyChanged(Sungero.Docflow.Shared.ContractualDocumentBaseCurrencyChangedEventArgs e)
    {
      base.CurrencyChanged(e);
      
      // Заполнить признак "Требуется корпоративное одобрение" в зависимости от соответствующей константы.
     if (e.NewValue != e.OldValue)
      {
        if (_obj.TransactionAmount.HasValue)
          _obj.IsCorporateApprovalRequired = DirRX.ContractsCustom.PublicFunctions.Module.Remote.IsCorporateApprovalRequired(_obj.TransactionAmount.Value, e.NewValue);
        else
          _obj.IsCorporateApprovalRequired = false;
        
        Functions.Contract.UpdateLukoilApproving(_obj);
        Functions.Contract.UpdateAnalysisRequired(_obj);
      }
    }

    public virtual void DocumentValidityChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        Functions.Contract.CheckValidTillState(_obj);
        Functions.Contract.UpdateLukoilApproving(_obj);
        Functions.Contract.UpdateAnalysisRequired(_obj);
        
        if (e.NewValue != null && _obj.ValidFrom.HasValue && !_obj.ValidTill.HasValue)
          _obj.ValidTill = _obj.ValidFrom.Value.AddMonths(e.NewValue.Value).AddDays(-1);
      }
    }

    public override void ValidTillChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.ValidTillChanged(e);
      if (e.NewValue != e.OldValue)
      {
        Functions.Contract.CheckValidTillState(_obj);
        Functions.Contract.UpdateLukoilApproving(_obj);
        Functions.Contract.UpdateAnalysisRequired(_obj);
        Functions.Contract.ChangeDestructionDate(_obj);
      }
    }

    public virtual void HolderTZChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        if (_obj.HolderTZ == DirRX.Solution.Contract.HolderTZ.Third)
        {
          if (!_obj.RequiredDocuments.Any(r => r.DocumentKind == Constants.Module.TrademarkDocKind))
            _obj.RequiredDocuments.AddNew().DocumentKind = Constants.Module.TrademarkDocKind;
        }
        else
        {
          while (_obj.RequiredDocuments.Any(r => r.DocumentKind == Constants.Module.TrademarkDocKind && r.Document == null))
            _obj.RequiredDocuments.Remove(_obj.RequiredDocuments.First(r => r.DocumentKind == Constants.Module.TrademarkDocKind && r.Document == null));
        }
      }
    }

    public virtual void ContractFunctionalityChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      Functions.Contract.ProcessDefaultTenderCounterparty(_obj);
      
      if (e.NewValue == e.OldValue)
        return;
      
      Functions.Contract.ChangeDocumentProperties(_obj);
      
      var purchase = DirRX.Solution.Contract.ContractFunctionality.Purchase;
      if (e.NewValue == purchase && !_obj.RequiredDocuments.Any(r => r.DocumentKind == Constants.Module.PurchaseDocKind))
        _obj.RequiredDocuments.AddNew().DocumentKind = Constants.Module.PurchaseDocKind;
      if (e.OldValue == purchase && _obj.RequiredDocuments.Any(r => r.DocumentKind == Constants.Module.PurchaseDocKind))
      {
        while (_obj.RequiredDocuments.Any(r => r.DocumentKind == Constants.Module.PurchaseDocKind))
          _obj.RequiredDocuments.Remove(_obj.RequiredDocuments.First(r => r.DocumentKind == Constants.Module.PurchaseDocKind));
      }
      _obj.State.Properties.TenderType.IsVisible = _obj.IsTender.Value && _obj.ContractFunctionality != ContractFunctionality.Sale;
      
      // Установить доступность и заполнить по умолчанию поля Код вида договора ИУС ЛЛК, Закупка, Сбыт.
     if (e.NewValue != null)
      {
        bool isContractFunctionalityMixed = _obj.ContractFunctionality == ContractFunctionality.Mixed;
        _obj.State.Properties.IMSCodeCollection.IsVisible = !isContractFunctionalityMixed;
        _obj.State.Properties.IMSCodePurchaseCollection.IsVisible = isContractFunctionalityMixed;
        _obj.State.Properties.IMSCodeSaleCollection.IsVisible = isContractFunctionalityMixed;
        if (isContractFunctionalityMixed)
        {
          _obj.IMSCodeCollection.Clear();
          if (_obj.DocumentGroup != null && _obj.DocumentGroup.IMSCode != null)
          {
            if (_obj.DocumentGroup.ContractFunctionality == DirRX.Solution.ContractCategory.ContractFunctionality.Purchase)
              _obj.IMSCodePurchaseCollection.AddNew().IMSCode = _obj.DocumentGroup.IMSCode;
            else
              if (_obj.DocumentGroup.ContractFunctionality == DirRX.Solution.ContractCategory.ContractFunctionality.Sale)
                _obj.IMSCodeSaleCollection.AddNew().IMSCode = _obj.DocumentGroup.IMSCode;
          }
        }
        else
        {
          _obj.IMSCodePurchaseCollection.Clear();
          _obj.IMSCodeSaleCollection.Clear();
          if (e.OldValue == ContractFunctionality.Mixed && _obj.DocumentGroup != null && _obj.DocumentGroup.IMSCode != null)
          {
            if (_obj.ContractFunctionality == _obj.DocumentGroup.ContractFunctionality)
              _obj.IMSCodeCollection.AddNew().IMSCode = _obj.DocumentGroup.IMSCode;
          }
        }
      }
    }

    public virtual void TenderStepChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      Functions.Contract.ProcessDefaultTenderCounterparty(_obj);
    }

    public virtual void TransactionAmountChanged(Sungero.Domain.Shared.DoublePropertyChangedEventArgs e)
    {
      if (e.NewValue == null)
      {
        _obj.IsCorporateApprovalRequired = false;
        return;
      }
      
      // Подставить по умолчанию валюту рубль.
      // Если валюта не менятеся, то вызываем заполнение поля "Согласование с ПАО "Лукойл", иначе будет вызвано в обработчике изменения свойства валюты.
       if (_obj.Currency == null)
      {
        var defaultCurrency = Sungero.Commons.PublicFunctions.Currency.Remote.GetDefaultCurrency();
        if (defaultCurrency != null)
          _obj.Currency = defaultCurrency;
        else
        {
          Functions.Contract.UpdateLukoilApproving(_obj);
          Functions.Contract.UpdateAnalysisRequired(_obj);
        }
      }
      else
      {
        Functions.Contract.UpdateLukoilApproving(_obj);
        Functions.Contract.UpdateAnalysisRequired(_obj);
      }
      
      // Заполнить признак "Требуется корпоративное одобрение" в зависимости от соответствующей константы.
      if (e.NewValue != e.OldValue)
        _obj.IsCorporateApprovalRequired = DirRX.ContractsCustom.PublicFunctions.Module.Remote.IsCorporateApprovalRequired(e.NewValue.Value, _obj.Currency);
    }

    public virtual void IsTermlessChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        if (e.NewValue == true)
        {
          _obj.DaysToFinishWorks = null;
        }
        Functions.Contract.CheckValidTillState(_obj);
        Functions.Contract.UpdateLukoilApproving(_obj);
        Functions.Contract.UpdateAnalysisRequired(_obj);
      }
    }

    public override void DaysToFinishWorksChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      base.DaysToFinishWorksChanged(e);
      if (e.NewValue != e.OldValue)
        Functions.Contract.CheckValidTillState(_obj);
    }

    public virtual void ActualDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      var isBackdating = e.NewValue != null && e.NewValue < Calendar.Today;
      _obj.State.Properties.BackdatingReason.IsEnabled = isBackdating;
      _obj.State.Properties.BackdatingReason.IsRequired = isBackdating;
      
      // Если поле Действует с пустое, заполнять датой документа.
      if (e.NewValue != null && !_obj.ValidFrom.HasValue && e.NewValue != e.OldValue)
        _obj.ValidFrom = e.NewValue;
    }

    public virtual void IsTenderChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue)
      {
        _obj.State.Properties.TenderStep.IsVisible = _obj.IsTender.Value;
        _obj.State.Properties.TenderType.IsVisible = _obj.IsTender.Value && _obj.ContractFunctionality != ContractFunctionality.Sale;
        // Устанавливается автоматически значением «Согласование проекта договора».
         if (_obj.IsTender.Value)
        {
          _obj.TenderStep = TenderStep.ApprovalProject;
          _obj.ContractActivate = DirRX.Solution.Contract.ContractActivate.Original;
          
          _obj.Counterparties.Clear();
          _obj.IsManyCounterparties = false;
          var standCompany = DirRX.ContractsCustom.PublicFunctions.Module.Remote.GetTenderPurchaseCounterparty();
          _obj.Counterparty = DirRX.Solution.Companies.As(standCompany);
        }
        else
          _obj.TenderStep = null;
        
        if (_obj.IsTender.Value && _obj.ContractFunctionality == ContractFunctionality.Mixed)
          _obj.ContractFunctionality = null;
        
        // Почистить таблицу с обязательными документами при смене признака.
        if (e.NewValue == false && _obj.StandartForm != null && _obj.InternalApprovalState == null)
        {
          foreach (var row in _obj.StandartForm.BindingDocument.Where(d => d.DocumentsForTender == true))
          {
            while (_obj.RequiredDocuments.Any(r => r.DocumentKind == row.DocumentKind && r.Document == null))
              _obj.RequiredDocuments.Remove(_obj.RequiredDocuments.First(r => r.DocumentKind == row.DocumentKind && r.Document == null));
          }
          
          foreach (var row in _obj.StandartForm.BindingDocumentCondition.Where(d => d.DocumentsForTender == true))
          {
            while (_obj.RequiredDocuments.Any(r => r.DocumentKind == row.DocumentKind.Name && r.Document == null))
              _obj.RequiredDocuments.Remove(_obj.RequiredDocuments.First(r => r.DocumentKind == row.DocumentKind.Name && r.Document == null));
          }
        }
      }
    }

    public virtual void StartConditionsExistsChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      var isDPOEmployeesRole = Users.Current.IncludedIn(DirRX.ContractsCustom.PublicConstants.Module.RoleGuid.DPOEmployeesRole);
      var startConditionsExists = _obj.StartConditionsExists == true;
      _obj.State.Properties.StartConditions.IsEnabled = isDPOEmployeesRole && startConditionsExists;
      _obj.State.Properties.AreConditionsCompleted.IsEnabled = isDPOEmployeesRole && startConditionsExists;
      if (e.NewValue == true)
        _obj.DpoUser = DirRX.Solution.Employees.Current;
      else
        _obj.DpoUser = null;
    }

    public override void CounterpartyChanged(Sungero.Docflow.Shared.ContractualDocumentBaseCounterpartyChangedEventArgs e)
    {
      base.CounterpartyChanged(e);
      
      if (e.NewValue != e.OldValue)
      {
        if (e.NewValue != null)
        {
          _obj.ShippingAddress = _obj.Counterparty.PostalShippingAddress;
          _obj.DeliveryMethod = _obj.Counterparty.DeliveryMethod;
          _obj.Contact = null;
          
          if (!_obj.IsManyCounterparties.GetValueOrDefault())
            Functions.Contract.SelectStandartFrom(_obj);
        }
        else
        {
          _obj.ShippingAddress = null;
          _obj.DeliveryMethod = null;
          _obj.Contact = null;
        }
        this.SyncCounterparties();
      }
    }

    public override void DeliveryMethodChanged(Sungero.Docflow.Shared.OfficialDocumentDeliveryMethodChangedEventArgs e)
    {
      base.DeliveryMethodChanged(e);
      
      if (e.NewValue != e.OldValue)
      {
        if (e.NewValue != null)
        {
          /*var deliveryMethod = DirRX.Solution.MailDeliveryMethods.As(e.NewValue);
          _obj.State.Properties.Contact.IsRequired = deliveryMethod.IsRequireContactInContract.GetValueOrDefault();
          
          if (_obj.Counterparty != null)
            _obj.State.Properties.ChangingShippingReason.IsRequired = !Solution.MailDeliveryMethods.Equals(e.NewValue, _obj.Counterparty.DeliveryMethod);*/
        }
        else
          _obj.State.Properties.Contact.IsRequired = false;
        
        this.SyncCounterparties();
      }
    }

    public override void DocumentGroupChanged(Sungero.Docflow.Shared.OfficialDocumentDocumentGroupChangedEventArgs e)
    {
      base.DocumentGroupChanged(e);
      
      if (e.NewValue != null && e.NewValue != e.OldValue)
      {
        if (_obj.DocumentGroup.ContractFunctionality.HasValue)
          _obj.ContractFunctionality = _obj.DocumentGroup.ContractFunctionality.Value;
        
        bool isContractFunctionalityMixed = _obj.ContractFunctionality == ContractFunctionality.Mixed;
        if (_obj.DocumentGroup.IMSCode != null)
        {
          _obj.IMSCodeCollection.Clear();
          _obj.IMSCodePurchaseCollection.Clear();
          _obj.IMSCodeSaleCollection.Clear();
          if (!isContractFunctionalityMixed)
            _obj.IMSCodeCollection.AddNew().IMSCode = _obj.DocumentGroup.IMSCode;
        }
        else
        {
          if (isContractFunctionalityMixed)
            _obj.IMSCodeCollection.Clear();
          else
          {
            _obj.IMSCodePurchaseCollection.Clear();
            _obj.IMSCodeSaleCollection.Clear();
          }
        }
        
        var supervisor = _obj.DocumentGroup.Supervisor;
        if (supervisor != null)
        {
          var roleRecipients = Roles.As(supervisor).RecipientLinks.FirstOrDefault(x => x.Member != null && DirRX.Solution.Employees.Is(x.Member));
          if (roleRecipients != null)
            _obj.Supervisor = DirRX.Solution.Employees.As(roleRecipients.Member);
        }
        else
        {
          if (_obj.DocumentGroup.IsSupervisorFunctionManager.GetValueOrDefault() && _obj.ResponsibleEmployee != null)
            _obj.Supervisor = DirRX.ActionItems.PublicFunctions.ActionItemsRole.Remote.GetInitCEOManager(DirRX.Solution.Employees.As(_obj.ResponsibleEmployee));
        }
        
        if (_obj.DocumentGroup.DestinationCountry == ContractCategory.DestinationCountry.RF)
        {
          var countryCode = DirRX.ContractsCustom.PublicConstants.Module.RussianFederationCountryCode;
          var country = DirRX.ContractsCustom.PublicFunctions.Module.Remote.GetCountryByCode(countryCode);
          if (country != null)
          {
            _obj.DestinationCountries.Clear();
            _obj.DestinationCountries.AddNew().DestinationCountry = country;
          }
        }
        if (_obj.DocumentGroup.DestinationCountry == ContractCategory.DestinationCountry.NotRequired)
          _obj.DestinationCountries.Clear();
        
        Functions.Contract.SelectStandartFrom(_obj);
      }
      
      Functions.Contract.ChangeDocumentProperties(_obj);
      if (e.OldValue != e.NewValue)
        Functions.Contract.ChangeDestructionDate(_obj);
    }

    public virtual void IsHighUrgencyChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      _obj.State.Properties.UrgencyReason.IsRequired = e.NewValue.Value;
      _obj.State.Properties.UrgencyReason.IsEnabled = e.NewValue.Value;
    }
    
    private void SyncCounterparties()
    {
      if (_obj.IsManyCounterparties != true)
        Functions.Contract.ClearAndFillFirstCounterparty(_obj);
    }

  }
}