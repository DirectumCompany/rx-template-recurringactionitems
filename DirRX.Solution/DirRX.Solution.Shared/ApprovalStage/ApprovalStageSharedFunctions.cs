using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalStage;

namespace DirRX.Solution.Shared
{
  partial class ApprovalStageFunctions
  {
    public override List<Enumeration?> GetPossibleRoles()
    {
      var baseRoles = base.GetPossibleRoles();
      
      if (_obj.StageType == Sungero.Docflow.ApprovalStage.StageType.SimpleAgr ||
          _obj.StageType == Sungero.Docflow.ApprovalStage.StageType.Notice)
      {
        baseRoles.Add(DirRX.LocalActs.LocalActsRole.Type.Subscribers);
        baseRoles.Add(DirRX.LocalActs.LocalActsRole.Type.Supervisor);
        baseRoles.Add(DirRX.LocalActs.LocalActsRole.Type.RegDocManagers);
        baseRoles.Add(DirRX.LocalActs.LocalActsRole.Type.CRegDocManagers);
        baseRoles.Add(DirRX.LocalActs.LocalActsRole.Type.SprvisorManager);
        baseRoles.Add(DirRX.LocalActs.LocalActsRole.Type.RiskManagers);
        baseRoles.Add(DirRX.ContractsCustom.ContractsRole.Type.CoExecutor);        
        baseRoles.Add(DirRX.RecordCustom.RecordCustomRole.Type.ApproverPrvSt);
      }
      
      if (_obj.StageType == Sungero.Docflow.ApprovalStage.StageType.Approvers)
      {
        baseRoles.Add(DirRX.LocalActs.LocalActsRole.Type.Supervisor);
        baseRoles.Add(DirRX.LocalActs.LocalActsRole.Type.Initiator);
        baseRoles.Add(DirRX.LocalActs.LocalActsRole.Type.RegDocManagers);
        baseRoles.Add(DirRX.LocalActs.LocalActsRole.Type.CRegDocManagers);
        baseRoles.Add(DirRX.LocalActs.LocalActsRole.Type.SprvisorManager);
        baseRoles.Add(DirRX.ContractsCustom.ContractsRole.Type.ContractResp);
        baseRoles.Add(DirRX.ContractsCustom.ContractsRole.Type.CoExecutor);
				baseRoles.Add(DirRX.ContractsCustom.ContractsRole.Type.Stage2Approvers);
				baseRoles.Add(DirRX.ContractsCustom.ContractsRole.Type.NonLukoil);
				baseRoles.Add(DirRX.ContractsCustom.ContractsRole.Type.NonStandart);
				baseRoles.Add(DirRX.LocalActs.LocalActsRole.Type.SprvisorManager);
        baseRoles.Add(DirRX.LocalActs.LocalActsRole.Type.RiskManagers);
        baseRoles.Add(Sungero.Docflow.ApprovalRole.Type.Addressee);
        baseRoles.Add(DirRX.RecordCustom.RecordCustomRole.Type.InitCEOManager);
        baseRoles.Add(DirRX.RecordCustom.RecordCustomRole.Type.MemoAddressee);
        baseRoles.Add(DirRX.RecordCustom.RecordCustomRole.Type.MemoAssignee);
        baseRoles.Add(DirRX.RecordCustom.RecordCustomRole.Type.AssigneeManager);
      }
      
      return baseRoles;
    }
    
    /// <summary>
    /// Установить обязательность свойств.
    /// </summary>
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();
      
      var allowSendToReworkInSimpleAgreement = _obj.StageType == StageType.SimpleAgr && (_obj.AllowSendToRework.GetValueOrDefault() || _obj.AllowSendToRecycling.GetValueOrDefault());
      var isStageWithRework = _obj.StageType == StageType.Approvers || allowSendToReworkInSimpleAgreement;
      _obj.State.Properties.ReworkType.IsRequired = _obj.Info.Properties.ReworkType.IsRequired || isStageWithRework;
    }
    
    /// <summary>
    /// Установить видимость свойств.
    /// </summary>
    public void SetVisibilityProperties()
    {
      // Для задания
      var isSimpleAssignment = _obj.StageType == StageType.SimpleAgr;
      var isApprover =  _obj.StageType == StageType.Approvers;
      var isCheckReturn = _obj.StageType == StageType.CheckReturn;
      var isSending = _obj.StageType == StageType.Sending;
      
      _obj.State.Properties.AllowSendToRecycling.IsVisible = isSimpleAssignment || isApprover;
      _obj.State.Properties.IsRiskConfirmation.IsVisible = isSimpleAssignment;
      _obj.State.Properties.NeedCounterpartyApproval.IsVisible = isSimpleAssignment;
      _obj.State.Properties.CanFailOn.IsVisible = isSimpleAssignment || isApprover;
      _obj.State.Properties.RequestInitiatorOn.IsVisible = isSimpleAssignment || isApprover;
      _obj.State.Properties.AllowSelectSigner.IsVisible = isSimpleAssignment;
      _obj.State.Properties.IsAssignmentOnWaitingEndValidContractor.IsVisible = isSimpleAssignment;
      _obj.State.Properties.IsAssignmentOnSigningOriginals.IsVisible = isSimpleAssignment;
      _obj.State.Properties.ApprovalWithRiskOn.IsVisible = isApprover;
      _obj.State.Properties.CanDenyApprovalOn.IsVisible = isApprover;
      _obj.State.Properties.KindOfDocumentNeedSend.IsVisible = isSending;
      _obj.State.Properties.KindOfDocumentNeedReturn.IsVisible = isCheckReturn;
      _obj.State.Properties.IsAssignmentAllDocsReceived.IsVisible = isSimpleAssignment;
      _obj.State.Properties.IsAssignmentCorporateApproval.IsVisible = isSimpleAssignment;
      _obj.State.Properties.IsSubjectTransactionConfirmation.IsVisible = isSimpleAssignment;
      _obj.State.Properties.IsAssignmentOnSigningScans.IsVisible = isSimpleAssignment;
      _obj.State.Properties.NeedBarcode.IsVisible = isApprover;
      
      // Для согласования
    }
    
    /// <summary>
    /// Установить доступность свойств.
    /// </summary>
    public void SetAvailabilityProperties()
    {
      var isSimpleAssignment = _obj.StageType == StageType.SimpleAgr;
      var allowSendToRework = _obj.AllowSendToRework.GetValueOrDefault() || _obj.AllowSendToRecycling.GetValueOrDefault();
      _obj.State.Properties.ReworkType.IsEnabled = isSimpleAssignment ? allowSendToRework : true;
      var isApprover =  _obj.StageType == StageType.Approvers;
      var isCheckReturn = _obj.StageType == StageType.CheckReturn;
      var isSending = _obj.StageType == StageType.Sending;
      
      _obj.State.Properties.AllowSendToRecycling.IsEnabled = isSimpleAssignment || isApprover;
      _obj.State.Properties.IsRiskConfirmation.IsEnabled = isSimpleAssignment;
      _obj.State.Properties.NeedCounterpartyApproval.IsEnabled = isSimpleAssignment;
      _obj.State.Properties.CanFailOn.IsEnabled = isSimpleAssignment || isApprover;
      _obj.State.Properties.RequestInitiatorOn.IsEnabled = isSimpleAssignment || isApprover;
      _obj.State.Properties.AllowSelectSigner.IsEnabled = isSimpleAssignment;
      _obj.State.Properties.IsAssignmentOnWaitingEndValidContractor.IsEnabled = isSimpleAssignment;
      _obj.State.Properties.IsAssignmentOnSigningOriginals.IsEnabled = isSimpleAssignment;
      _obj.State.Properties.ApprovalWithRiskOn.IsEnabled = isApprover;
      _obj.State.Properties.CanDenyApprovalOn.IsEnabled = isApprover;
      _obj.State.Properties.KindOfDocumentNeedSend.IsEnabled = isSending;
      _obj.State.Properties.KindOfDocumentNeedReturn.IsEnabled = isCheckReturn;
      _obj.State.Properties.IsAssignmentAllDocsReceived.IsEnabled = isSimpleAssignment;
      _obj.State.Properties.IsAssignmentCorporateApproval.IsEnabled = isSimpleAssignment;
      _obj.State.Properties.IsSubjectTransactionConfirmation.IsEnabled = isSimpleAssignment;
      _obj.State.Properties.NeedBarcode.IsEnabled = isApprover;
    }
  }
}