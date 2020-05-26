using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalTask;

namespace DirRX.Solution
{
  partial class ApprovalTaskServerHandlers
  {

    public override void BeforeRestart(Sungero.Workflow.Server.BeforeRestartEventArgs e)
    {
      base.BeforeRestart(e);
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      
      // Очистить статусы договоров и ДС.
      var contract = DirRX.Solution.Contracts.As(document);
      if (contract != null)
      {
        contract.OriginalSigning = null;
        contract.ContractorOriginalSigning = null;
        contract.InternalApprovalState = null;
        
        if (contract.CounterpartyApprovalState != Solution.Contract.CounterpartyApprovalState.Signed)
        {
          contract.ExternalApprovalState = null;
          contract.Tracking.Clear();
          contract.ScanMoveStatuses.Clear();
        }
        
        contract.ApproveStatuses.Clear();
        contract.OriginalMoveStatuses.Clear();
      }
      var supAgreement = DirRX.Solution.SupAgreements.As(document);
      if (supAgreement != null)
      {
        supAgreement.OriginalSigning = null;
        supAgreement.ContractorOriginalSigning = null;
        supAgreement.InternalApprovalState = null;
        
        if (supAgreement.CounterpartyApprovalState != Solution.SupAgreement.CounterpartyApprovalState.Signed)
        {
          supAgreement.ExternalApprovalState = null;
          supAgreement.Tracking.Clear();
          supAgreement.ScanMoveStatuses.Clear();
        }
        
        supAgreement.ApproveStatuses.Clear();
        supAgreement.OriginalMoveStatuses.Clear();
      }
    }

    public override void BeforeAbort(Sungero.Workflow.Server.BeforeAbortEventArgs e)
    {
      base.BeforeAbort(e);
      
      // Перевести все риски в статус Устаревшие.
      var obsoleteRisksStatus = LocalActs.RiskStatuses.GetAll(x => x.Name == DirRX.Solution.ApprovalReworkAssignments.Resources.ObsoleteRisksStatusName).FirstOrDefault();
      if (obsoleteRisksStatus != null)
        foreach (var risk in _obj.RiskAttachmentGroup.Risks)
          DirRX.Solution.PublicFunctions.Module.Remote.SetRiskStatusClosed(risk);
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      // Если тема не сформирована, то подставить пустую.
      if (_obj.Subject == Sungero.Docflow.Resources.AutoformatTaskSubject)
        using (TenantInfo.Culture.SwitchTo())
          _obj.Subject = ApprovalTasks.Resources.TaskSubjectFormat(string.Empty);
      
      // Выдать права на документы, для всех, кому выданы права на задачу.
      if (_obj.State.IsChanged)
      {
        var attachments = _obj.AllAttachments.Where(x => !DirRX.LocalActs.Risks.Is(x)).ToList();
        Sungero.Docflow.PublicFunctions.Module.GrantManualReadRightForAttachments(_obj, attachments);
      }
    }
    
    public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
    {
      base.BeforeStart(e);
      
      if (_obj.Subscribers.Count > 0)
      {
        Functions.ApprovalTask.GrantReadSubscribersRights(_obj);
        Functions.ApprovalTask.SendNoticeToSubscribers(_obj);
      }
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document != null && document.DocumentKind.DocumentType.DocumentTypeGuid == DirRX.Solution.Constants.Module.DocumentTypeGuid.RevisionRequest)
      {
        var revisionRequests = PartiesControl.RevisionRequests.As(document);
        
        var bindingDocCopies = revisionRequests.BindingDocuments.Where(d => d.Format == DirRX.PartiesControl.RevisionRequestBindingDocuments.Format.Copy);
        foreach (var bindingDocRow in bindingDocCopies)
        {
          bindingDocRow.Sent = true;
          bindingDocRow.SendDate = Calendar.UserToday;
        }
        
        var bindingSequrityDocCopies = revisionRequests.SecurityServiceDocuments.Where(d => d.Format == DirRX.PartiesControl.RevisionRequestBindingDocuments.Format.Copy);
        foreach (var bindingSequrityDocRow in bindingSequrityDocCopies)
        {
          bindingSequrityDocRow.Sent = true;
          bindingSequrityDocRow.SendDate = Calendar.UserToday;
        }
        
        // Проверка на блокировку контрагента находится в обработчике выполнения действия.
        revisionRequests.Counterparty.IsDocumentsProvided = false;
        revisionRequests.Counterparty.Save();
        
        document.Save();
      }
      
      var contract = DirRX.Solution.Contracts.As(document);
      var supAgreement = DirRX.Solution.SupAgreements.As(document);
      
      // Установить высокую важность для задач по согласованию договорных документов, если документ по контрагенту с признаком «Стратегический партнер».
      var contractBase = Sungero.Docflow.ContractualDocumentBases.As(document);
      if (contractBase != null)
      {
        // Для договора и ДС cмотрим контрагентов по коллекции, для всех остальных - по свойству.
        if (contract != null || supAgreement != null)
        {
          if ((contract != null && contract.Counterparties.Any(cp => DirRX.Solution.Companies.As(cp.Counterparty).IsStrategicPartner == true)) ||
              (supAgreement != null && supAgreement.Counterparties.Any(cp => DirRX.Solution.Companies.As(cp.Counterparty).IsStrategicPartner == true)))
            _obj.Importance = Solution.ApprovalTask.Importance.High;
        }
        else
        {
          var company = Solution.Companies.As(contractBase.Counterparty);
          if (company != null)
          {
            if (company.IsStrategicPartner == true)
              _obj.Importance = Solution.ApprovalTask.Importance.High;
          }
        }
      }
      
      // Очистить статусы договоров и ДС.
      if (contract != null)
      {
        contract.OriginalSigning = null;
        contract.ContractorOriginalSigning = null;
        contract.InternalApprovalState = null;
        
        if (contract.CounterpartyApprovalState != Solution.Contract.CounterpartyApprovalState.Signed)
        {
          contract.ExternalApprovalState = null;
          contract.Tracking.Clear();
          contract.ScanMoveStatuses.Clear();
        }
        
        contract.ApproveStatuses.Clear();        
        contract.OriginalMoveStatuses.Clear();
      }
      if (supAgreement != null)
      {
        supAgreement.OriginalSigning = null;
        supAgreement.ContractorOriginalSigning = null;
        supAgreement.InternalApprovalState = null;
        
        if (supAgreement.CounterpartyApprovalState != Solution.SupAgreement.CounterpartyApprovalState.Signed)
        {
          supAgreement.ExternalApprovalState = null;
          supAgreement.Tracking.Clear();
          supAgreement.ScanMoveStatuses.Clear();
        }
        
        supAgreement.ApproveStatuses.Clear();
        supAgreement.OriginalMoveStatuses.Clear();
      }
      
      _obj.InitDeadline = _obj.MaxDeadline;
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      _obj.NeedLUKOILApproval = false;
      _obj.IsRecycle = false;
      _obj.LastStageWithRisk = false;
      _obj.NeedPaperSigning = false;
    }
  }
}