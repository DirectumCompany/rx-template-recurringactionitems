using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.SupAgreement;

namespace DirRX.Solution
{
  partial class SupAgreementTrackingSharedCollectionHandlers
  {

    public override void TrackingDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e)
    {
      // Убрали проверку.
    }
  }

  partial class SupAgreementCounterpartiesSharedHandlers
  {

    public virtual void CounterpartiesContactChanged(DirRX.Solution.Shared.SupAgreementCounterpartiesContactChangedEventArgs e)
    {
      if (e.NewValue != null && _obj.Counterparty == null)
        _obj.Counterparty = DirRX.Solution.Companies.As(e.NewValue.Company);
    }

    public virtual void CounterpartiesDeliveryMethodChanged(DirRX.Solution.Shared.SupAgreementCounterpartiesDeliveryMethodChangedEventArgs e)
    {
      /*if (e.NewValue != null && e.NewValue != e.OldValue)
        _obj.State.Properties.Contact.IsRequired = e.NewValue.IsRequireContactInContract.GetValueOrDefault();*/
    }

    public virtual void CounterpartiesSignatoryChanged(DirRX.Solution.Shared.SupAgreementCounterpartiesSignatoryChangedEventArgs e)
    {
      if (e.NewValue != null && _obj.Counterparty == null)
        _obj.Counterparty = DirRX.Solution.Companies.As(e.NewValue.Company);
    }

    public virtual void CounterpartiesCounterpartyChanged(DirRX.Solution.Shared.SupAgreementCounterpartiesCounterpartyChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue)
        Functions.SupAgreement.SelectStandartFrom(_obj.SupAgreement);
    }
  }

  partial class SupAgreementCounterpartiesSharedCollectionHandlers
  {

    public virtual void CounterpartiesDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e)
    {
      Functions.SupAgreement.SelectStandartFrom(_obj);
    }

    public virtual void CounterpartiesAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.Number = (_obj.Counterparties.Max(a => a.Number) ?? 0) + 1;
    }
  }

  partial class SupAgreementRequiredDocumentsSharedHandlers
  {

    public virtual void RequiredDocumentsDocumentChanged(DirRX.Solution.Shared.SupAgreementRequiredDocumentsDocumentChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        var document = SupAgreements.As(_obj.RootEntity);
        var oldRelatedDocs = document.Relations.GetRelated();
        if (e.OldValue != null && oldRelatedDocs.Any(d => Sungero.Content.IElectronicDocument.Equals(e.OldValue, d)))
          document.Relations.Remove(Constants.Module.SimpleRelationRelationName, e.OldValue);
        if (e.NewValue != null && !oldRelatedDocs.Any(d => Sungero.Content.IElectronicDocument.Equals(e.NewValue, d)))
          document.Relations.Add(Constants.Module.SimpleRelationRelationName, e.NewValue);
      }
    }
  }

  partial class SupAgreementRequiredDocumentsSharedCollectionHandlers
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

  partial class SupAgreementOtherDocumentsSharedCollectionHandlers
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

  partial class SupAgreementOtherDocumentsSharedHandlers
  {

    public virtual void OtherDocumentsDocumentChanged(DirRX.Solution.Shared.SupAgreementOtherDocumentsDocumentChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        var document = SupAgreements.As(_obj.RootEntity);
        var oldRelatedDocs = document.Relations.GetRelated();
        if (e.OldValue != null && oldRelatedDocs.Any(d => Sungero.Content.IElectronicDocument.Equals(e.OldValue, d)))
          document.Relations.Remove(Constants.Module.SimpleRelationRelationName, e.OldValue);
        if (e.NewValue != null && !oldRelatedDocs.Any(d => Sungero.Content.IElectronicDocument.Equals(e.NewValue, d)))
          document.Relations.Add(Constants.Module.SimpleRelationRelationName, e.NewValue);
      }
    }
  }


  partial class SupAgreementTrackingSharedHandlers
  {

    public override void TrackingReturnResultChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      base.TrackingReturnResultChanged(e);
      
      if (e.NewValue != e.OldValue && e.NewValue == SupAgreementTracking.ReturnResult.NotSigned && _obj.IsOriginal == false)
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(DirRX.Solution.SupAgreements.As(_obj.OfficialDocument),
                                                                              DirRX.ContractsCustom.PublicConstants.Module.ContractStatusGuid.CounterpartyRejectedSigningGuid,
                                                                              DirRX.ContractsCustom.PublicConstants.Module.ContractStatusType.ScanMoveStatus,
                                                                              true);
    }

    public override void TrackingActionChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      base.TrackingActionChanged(e);
      
      if (_obj.Action == SupAgreementTracking.Action.OriginalSend)
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

  partial class SupAgreementSharedHandlers
  {

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
        
        if (e.NewValue == Solution.SupAgreement.CounterpartyApprovalState.Signed && curEmployee != null)
        {
          if (_obj.ExternalApprovalState != ExternalApprovalState.Signed)
            _obj.ExternalApprovalState = ExternalApprovalState.Signed;
          
          var deadline = Functions.Module.Remote.GetDeadlineConstantValue();
          if (tracking == null)
          {
            Solution.ISupAgreementTracking issue = _obj.Tracking.AddNew() as ISupAgreementTracking;
            issue.Action = Solution.SupAgreementTracking.Action.OriginalSend;
            issue.DeliveredTo = curEmployee;
            issue.ReturnDeadline = Calendar.UserToday.AddWorkingDays(deadline);
            issue.IsOriginal = true;
            issue.Format = Solution.SupAgreementTracking.Format.Original;
            // Иначе идет отправка задачи
            issue.ExternalLinkId = 0;
          }
          else
          {
            Solution.ISupAgreementTracking issue = tracking as ISupAgreementTracking;
            issue.ReturnDeadline = Calendar.UserToday.AddWorkingDays(deadline);
            issue.Format = Solution.SupAgreementTracking.Format.Original;
          }
        }
        
        if (e.NewValue == Solution.SupAgreement.CounterpartyApprovalState.Unsigned && tracking != null)
          tracking.ReturnResult = Solution.SupAgreementTracking.ReturnResult.NotSigned;
      }
    }

    public override void CounterpartySignatoryChanged(Sungero.Docflow.Shared.ContractualDocumentBaseCounterpartySignatoryChangedEventArgs e)
    {
      base.CounterpartySignatoryChanged(e);
      if (e.NewValue != e.OldValue)
        this.SyncCounterparties();
    }

    public override void ContactChanged(Sungero.Contracts.Shared.ContractualDocumentContactChangedEventArgs e)
    {
      base.ContactChanged(e);
      if (e.NewValue != e.OldValue)
        this.SyncCounterparties();
    }

    public virtual void ShippingAddressChanged(DirRX.Solution.Shared.SupAgreementShippingAddressChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
        this.SyncCounterparties();
    }

    public virtual void IDSapChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
        this.SyncCounterparties();
    }

    public override void ResponsibleEmployeeChanged(Sungero.Contracts.Shared.ContractualDocumentResponsibleEmployeeChangedEventArgs e)
    {
      base.ResponsibleEmployeeChanged(e);
      if (e.NewValue != null && e.OldValue != e.NewValue)
        _obj.Department = e.NewValue.Department;
    }

    public virtual void IsManyCounterpartiesChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (_obj.IsManyCounterparties == true)
      {
        Functions.SupAgreement.ClearAndFillFirstCounterparty(_obj);
        
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
        
        Functions.SupAgreement.ClearAndFillFirstCounterparty(_obj);
      }
    }

    public virtual void LukoilApprovingChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue && e.NewValue == SupAgreement.LukoilApproving.Required)
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.LukoilApprovedGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus,
                                                                              false);
      
      if (e.NewValue != e.OldValue && e.NewValue == SupAgreement.LukoilApproving.Received)
        ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(_obj,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusGuid.LukoilApprovedGuid,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus);
    }

    public virtual void OriginalSigningChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue && e.NewValue == SupAgreement.OriginalSigning.Signed)
      {
        // Удалить статус "Оригинал передан на подписание в Обществе".
        ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(_obj,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalSendedBusinessUnitForSigningGuid,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus);
        // Установить статус договора "Ожидает отправки контрагенту".
        Functions.SupAgreement.SetStatusOriginalWaitingForSending(_obj);
        
        if (_obj.ContractorOriginalSigning == SupAgreement.ContractorOriginalSigning.Signed)
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
      
      if (e.NewValue != e.OldValue && e.NewValue == SupAgreement.OriginalSigning.NotSigned)
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
      
      if (e.NewValue != e.OldValue && e.NewValue == SupAgreement.OriginalSigning.OnSigning)
      {
        // Установка статуса "Оригинал передан на подписание в Обществе".
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalSendedBusinessUnitForSigningGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus,
                                                                              false);
      }
    }

    public virtual void ContractorOriginalSigningChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      // Установка статуса "Получен оригинал, подписанный Контрагентом".
      if (e.NewValue != e.OldValue && e.NewValue == SupAgreement.ContractorOriginalSigning.Signed && _obj.ExternalApprovalState == SupAgreement.ExternalApprovalState.Signed)
      {
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalReceivedFromCounterpartyGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus,
                                                                              false);
        _obj.CounterpartyApprovalState = CounterpartyApprovalState.Signed;
      }
      
      // Установка статуса "Контрагент вернул неподписанный документ".
      if (e.NewValue != e.OldValue && e.NewValue == SupAgreement.ContractorOriginalSigning.NotSigned && _obj.ExternalApprovalState == SupAgreement.ExternalApprovalState.Unsigned)
      {
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalReceivedNonSignedGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus,
                                                                              false);
        _obj.CounterpartyApprovalState = CounterpartyApprovalState.Unsigned;
      }
      
      if (e.NewValue != e.OldValue && e.NewValue == SupAgreement.ContractorOriginalSigning.Signed && _obj.OriginalSigning == SupAgreement.OriginalSigning.Signed)
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

    public override void PlacedToCaseFileDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.PlacedToCaseFileDateChanged(e);
      
      // Установка статуса "Документ помещен в архив".
      if (e.NewValue != null && e.NewValue != e.OldValue && _obj.PlacedToCaseFileDate.HasValue)
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalArchivedGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus,
                                                                              true);
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

    public override void InternalApprovalStateChanged(Sungero.Domain.Shared.EnumerationPropertyChangedEventArgs e)
    {
      base.InternalApprovalStateChanged(e);
      
      
      if (e.NewValue != e.OldValue && e.NewValue == SupAgreement.InternalApprovalState.OnApproval)
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
      
      if (e.NewValue != e.OldValue && e.NewValue == SupAgreement.InternalApprovalState.OnRework)
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
      
      if (e.NewValue != e.OldValue && e.NewValue == SupAgreement.InternalApprovalState.PendingSign)
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
      
      if (e.NewValue != e.OldValue && e.NewValue == SupAgreement.InternalApprovalState.Signed)
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
      
      if (e.NewValue != e.OldValue && e.NewValue == SupAgreement.InternalApprovalState.Aborted)
      {
        // Установить статус "Отказ от заключения договора".
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(_obj,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.RejectedGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus,
                                                                              true);
      }
    }

    public override void ValidFromChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.ValidFromChanged(e);
      
      if (e.NewValue != e.OldValue)
      {
        Functions.SupAgreement.UpdateLukoilApproving(_obj);
        Functions.SupAgreement.UpdateAnalysisRequired(_obj);
      }
    }

    public override void CounterpartyChanged(Sungero.Docflow.Shared.ContractualDocumentBaseCounterpartyChangedEventArgs e)
    {
      base.CounterpartyChanged(e);
      
      if (e.NewValue != null && e.NewValue != e.OldValue && !_obj.IsManyCounterparties.GetValueOrDefault())
        Functions.SupAgreement.SelectStandartFrom(_obj);
      
      if (e.NewValue != e.OldValue)
        this.SyncCounterparties();
    }

    public override void DocumentGroupChanged(Sungero.Docflow.Shared.OfficialDocumentDocumentGroupChangedEventArgs e)
    {
      base.DocumentGroupChanged(e);
      
      if (e.NewValue != null && e.NewValue != e.OldValue)
        Functions.SupAgreement.SelectStandartFrom(_obj);
    }

    public override void IsStandardChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      base.IsStandardChanged(e);
      
      if (e.NewValue != null && e.NewValue != e.OldValue)
      {
        // Закрыть от редактирования поле "Действует по", если договор типовой.
        _obj.State.Properties.ValidTill.IsEnabled = !_obj.IsStandard.Value;
        
        Functions.SupAgreement.SelectStandartFrom(_obj);
      }
    }

    public override void DocumentKindChanged(Sungero.Docflow.Shared.OfficialDocumentDocumentKindChangedEventArgs e)
    {
      base.DocumentKindChanged(e);
      
      if (e.NewValue != null && e.NewValue != e.OldValue)
        Functions.SupAgreement.SelectStandartFrom(_obj);
    }

    public virtual void StandartFormChanged(DirRX.Solution.Shared.SupAgreementStandartFormChangedEventArgs e)
    {
      if (e.NewValue == e.OldValue || e.NewValue == null)
        return;
      
      var contractSetting = e.NewValue;
      
      _obj.ContractActivate = contractSetting.ContractActivate;
      
      Functions.SupAgreement.UpdateAnalysisRequired(_obj);
      Functions.SupAgreement.UpdateLukoilApproving(_obj);
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
        
        Functions.SupAgreement.UpdateLukoilApproving(_obj);
        Functions.SupAgreement.UpdateAnalysisRequired(_obj);
      }
    }

    public virtual void DocumentValidityChanged(Sungero.Domain.Shared.IntegerPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue)
      {
        Functions.SupAgreement.CheckValidTillState(_obj);
        Functions.SupAgreement.UpdateAnalysisRequired(_obj);
        
        if (e.NewValue != null && _obj.ValidFrom.HasValue && !_obj.ValidTill.HasValue)
          _obj.ValidTill = _obj.ValidFrom.Value.AddMonths(e.NewValue.Value).AddDays(-1);
      }
    }

    public override void ValidTillChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.ValidTillChanged(e);
      if (e.NewValue != e.OldValue)
      {
        Functions.SupAgreement.CheckValidTillState(_obj);
        Functions.SupAgreement.UpdateLukoilApproving(_obj);
        Functions.SupAgreement.ChangeDestructionDate(_obj);
        Functions.SupAgreement.UpdateAnalysisRequired(_obj);
      }
    }

    public virtual void UsedTrademarkChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (e.NewValue != e.OldValue && e.NewValue == true && !_obj.RequiredDocuments.Any(r => r.DocumentKind == Constants.Module.TrademarkDocKind))
        _obj.RequiredDocuments.AddNew().DocumentKind = Constants.Module.TrademarkDocKind;
      if (e.NewValue != e.OldValue && e.NewValue == false && _obj.RequiredDocuments.Any(r => r.DocumentKind == Constants.Module.TrademarkDocKind))
      {
        while (_obj.RequiredDocuments.Any(r => r.DocumentKind == Constants.Module.TrademarkDocKind))
          _obj.RequiredDocuments.Remove(_obj.RequiredDocuments.First(r => r.DocumentKind == Constants.Module.TrademarkDocKind));
      }
    }

    public override void LeadingDocumentChanged(Sungero.Docflow.Shared.OfficialDocumentLeadingDocumentChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      // В ДС поле валюта заполнять из договора.
      if (e.NewValue != null)
      {
        var contract = Contracts.As(e.NewValue);
        _obj.BusinessUnit = contract.BusinessUnit;
        Sungero.Docflow.PublicFunctions.OfficialDocument.CopyProjects(e.NewValue, _obj);
        _obj.Currency = contract.Currency;
        _obj.ContractValidTill = contract.ValidTill;
        _obj.Territory = contract.Territory;

        if (contract.Supervisor != null)
          _obj.Supervisor = contract.Supervisor;
        
        _obj.IsManyCounterparties = contract.IsManyCounterparties;
        if (contract.IsManyCounterparties == true && contract.Counterparties.Any())
        {
          
          foreach (var counterparty in contract.Counterparties)
          {
            var newCounterparty = _obj.Counterparties.AddNew();
            newCounterparty.Counterparty = counterparty.Counterparty;
            newCounterparty.Signatory = counterparty.Signatory;
            newCounterparty.DeliveryMethod = counterparty.DeliveryMethod;
            newCounterparty.Address = counterparty.Address;
            newCounterparty.Contact = counterparty.Contact;
          }
        }
        else
        {
          _obj.Counterparty = contract.Counterparty;
          _obj.Contact = contract.Contact;
          _obj.DeliveryMethod = contract.Counterparty.DeliveryMethod;
          var supAgreementQuery = Functions.SupAgreement.Remote.GetSupAgreement(_obj, contract);
          if (supAgreementQuery.Count() > 0)
          {
            var supAgreement = supAgreementQuery.FirstOrDefault();
            _obj.ShippingAddress = supAgreement.ShippingAddress;
          }
          else
            _obj.ShippingAddress = contract.ShippingAddress;
        }
        
        // Для группы полей "Контрагент"
        _obj.IsScannedImageSign = contract.IsScannedImageSign;
        _obj.CounterpartySignatory = contract.CounterpartySignatory;
        
        Functions.SupAgreement.SelectStandartFrom(_obj);
        
        if (contract.StandartForm != null && contract.StandartForm.BindingDocument.Any())
        {
          _obj.RequiredDocuments.Clear();
          var isTender = contract.IsTender ?? false;
          foreach (var row in contract.StandartForm.BindingDocument.Where(d => isTender || d.DocumentsForTender == DirRX.Solution.Contracts.As(_obj.LeadingDocument).IsTender))
          {
            var newRow = _obj.RequiredDocuments.AddNew();
            newRow.DocumentKind = row.DocumentKind;
          }
        }
        
        if (_obj.StandartForm == null && contract.StandartForm != null)
          _obj.ContractActivate = contract.StandartForm.ContractActivate;
        
        if (contract.DocumentGroup != null)
        {
          if (contract.DocumentGroup.DestinationCountry == ContractCategory.DestinationCountry.RF)
          {
            var countryCode = DirRX.ContractsCustom.PublicConstants.Module.RussianFederationCountryCode;
            var country = DirRX.ContractsCustom.PublicFunctions.Module.Remote.GetCountryByCode(countryCode);
            if (country != null)
            {
              _obj.DestinationCountries.Clear();
              _obj.DestinationCountries.AddNew().DestinationCountry = country;
            }
          }
          else if (contract.DocumentGroup.DestinationCountry == ContractCategory.DestinationCountry.NotRequired)
            _obj.DestinationCountries.Clear();
        }
        
        Functions.SupAgreement.UpdateAnalysisRequired(_obj);
      }
      
      Functions.SupAgreement.ChangeDocumentProperties(_obj);
      
      FillName();
      _obj.Relations.AddFromOrUpdate(Sungero.Contracts.Constants.Module.SupAgreementRelationName, e.OldValue, e.NewValue);
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
          Functions.SupAgreement.UpdateLukoilApproving(_obj);
          Functions.SupAgreement.UpdateAnalysisRequired(_obj);
        }
      }
      else
      {
        Functions.SupAgreement.UpdateLukoilApproving(_obj);
        Functions.SupAgreement.UpdateAnalysisRequired(_obj);
      }
      
      // Заполнить признак "Требуется корпоративное одобрение" в зависимости от соответствующей константы.
      if (e.NewValue != e.OldValue)
        _obj.IsCorporateApprovalRequired = DirRX.ContractsCustom.PublicFunctions.Module.Remote.IsCorporateApprovalRequired(e.NewValue.Value, _obj.Currency);
    }

    public virtual void ActualDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      var isBackdating = e.NewValue != null && e.NewValue < Calendar.Today;
      _obj.State.Properties.BackdatingReason.IsEnabled = isBackdating;
      _obj.State.Properties.BackdatingReason.IsRequired = isBackdating;
      
      // Если поле Действует с пустое, заполнять датой документа.
      if (e.NewValue != null && !_obj.ValidFrom.HasValue && e.NewValue != e.OldValue)
        _obj.ValidFrom = e.NewValue;
      Functions.SupAgreement.FillName(_obj);
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
    
    public virtual void IsHighUrgencyChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      _obj.State.Properties.UrgencyReason.IsRequired = e.NewValue.Value;
      _obj.State.Properties.UrgencyReason.IsEnabled = e.NewValue.Value;
    }
    
    private void SyncCounterparties()
    {
      if (_obj.IsManyCounterparties == false)
        Functions.SupAgreement.ClearAndFillFirstCounterparty(_obj);
    }

  }
}