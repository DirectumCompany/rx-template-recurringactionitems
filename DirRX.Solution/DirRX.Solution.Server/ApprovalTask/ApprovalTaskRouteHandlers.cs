using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;
using DirRX.Solution.ApprovalTask;
using DirRX.Solution.ApprovalCheckingAssignment;

namespace DirRX.Solution.Server
{
  partial class ApprovalTaskRouteHandlers
  {

    public override void StartBlock36(Sungero.Docflow.Server.ApprovalReviewAssignmentArguments e)
    {
      base.StartBlock36(e);
      
      // Проставить на служебную записку штамп о подписании.
      var document = Memos.As(_obj.DocumentGroup.OfficialDocuments.FirstOrDefault());
      
      if (document != null && e.Block.Performers.Any())
        DirRX.Solution.Functions.Memo.MemoConvertToPdfWithSignatureMark(document);
    }

    public override void Script26Execute()
    {
      base.Script26Execute();
      
      // Установить статус "Проверка контрагента", при старте согласования заявки на проверку.
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (document != null)
      {
        var request = PartiesControl.RevisionRequests.As(document);
        if (request != null && request.MainDocument != null && !request.ApprovalResult.HasValue)
        {
          ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(request.MainDocument,
                                                                                ContractsCustom.PublicConstants.Module.ContractStatusGuid.CounterpartyCheckingGuid,
                                                                                ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus,
                                                                                false);
          request.MainDocument.Save();
        }
      }
    }

    #region Задание с отправкой на доработку.

    public override void StartBlock31(Sungero.Docflow.Server.ApprovalCheckingAssignmentArguments e)
    {
      var stage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.SimpleAgr);
      if (stage == null)
        return;
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      // В задании на организацию подписания скан-копий при старте проверяем поле "Подписание оригиналов к/а", если оно заполнено, тогда не формируем задание.
      if (stage.IsAssignmentOnSigningScans == true)
      {
        var contract = Solution.Contracts.As(document);
        var supAgreement = Solution.SupAgreements.As(document);
        if (contract != null && contract.ContractorOriginalSigning != null)
          return;
        if (supAgreement != null && supAgreement.ContractorOriginalSigning != null)
          return;
      }
      
      #region Скопировано из стандартной.
      
      // Задать тему.
      e.Block.Subject = Sungero.Docflow.PublicFunctions.ApprovalRuleBase.FormatStageSubject(stage, document);
      
      // Задать исполнителей.
      e.Block.IsParallel = stage.Sequence == Sungero.Docflow.ApprovalStage.Sequence.Parallel;
      var performers = Sungero.Docflow.PublicFunctions.ApprovalStage.Remote.GetStagePerformers(_obj, stage);
      if (!performers.Any())
      {
        Functions.ApprovalTask.FillReworkReasonWhenAssigneeNotFound(_obj, stage);
        return;
      }
      foreach (var performer in performers)
        e.Block.Performers.Add(performer);
      
      // Задать результат выполнения при котором остановятся все задания по блоку.
      if (stage.ReworkType == Sungero.Docflow.ApprovalStage.ReworkType.AfterEach)
        e.Block.StopResult = Sungero.Docflow.ApprovalCheckingAssignment.Result.ForRework;

      // Срок.
      e.Block.RelativeDeadlineDays = stage.DeadlineInDays;
      e.Block.RelativeDeadlineHours = stage.DeadlineInHours;
      
      // Тема из этапа.
      e.Block.StageSubject = stage.Subject;
      
      // Выдать права на документы.
      var recipients = Sungero.Docflow.PublicFunctions.ApprovalStage.Remote.GetStageRecipients(stage, _obj);
      Functions.ApprovalTask.GrantRightForAttachmentsToPerformers(_obj, recipients);
      
      #endregion
    }

    public override void StartAssignment31(Sungero.Docflow.IApprovalCheckingAssignment assignment, Sungero.Docflow.Server.ApprovalCheckingAssignmentArguments e)
    {
      base.StartAssignment31(assignment, e);
      
      var stage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.SimpleAgr);
      DirRX.Solution.ApprovalCheckingAssignments.As(assignment).ApprovalStage = stage;
      if (stage != null && stage.IsRiskConfirmation.HasValue)
        DirRX.Solution.ApprovalCheckingAssignments.As(assignment).IsRiskConfirmation = stage.IsRiskConfirmation.Value;
      
      assignment.Deadline = Functions.ApprovalTask.UpdateRelativeDeadline(assignment.Performer, assignment.Deadline);
      
      // Заполнить поле подписал
      var customAssigment = Solution.ApprovalCheckingAssignments.As(assignment);
      customAssigment.Signatory = _obj.Signatory;

      if (stage != null && stage.IsAssignmentCorporateApproval == true)
      {
        var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
        if (document != null)
        {
          // Установка статуса "Получение корпоративного одобрения".
          var contractDoc = Sungero.Contracts.ContractualDocuments.As(document);
          if (contractDoc != null)
            ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(contractDoc,
                                                                                  DirRX.ContractsCustom.PublicConstants.Module.ContractStatusGuid.CorpAcceptanceGuid,
                                                                                  ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus,
                                                                                  false);
        }
      }
    }
    
    public override void CompleteAssignment31(Sungero.Docflow.IApprovalCheckingAssignment assignment, Sungero.Docflow.Server.ApprovalCheckingAssignmentArguments e)
    {
      base.CompleteAssignment31(assignment, e);
      
      // Обновить подписанта
      var assig = Solution.ApprovalCheckingAssignments.As(assignment);
      var signatory = assig.Signatory;
      if (_obj.Signatory != signatory && signatory != null)
        _obj.Signatory = signatory;
      
      var currentStage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.SimpleAgr);
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      var contract = Solution.Contracts.As(document);
      var supAgreement = Solution.SupAgreements.As(document);
      
      #region Заполнение поля "Подписание оригиналов"
      
      if (currentStage.IsAssignmentOnSigningOriginals == true)
      {
        var isSigned = assignment.Result == Sungero.Docflow.ApprovalCheckingAssignment.Result.Accept;
        Functions.Module.ChangeContractualDocSigningOriginalState(document, isSigned);
      }
      #endregion
      
      #region Заполнение поля "Согласование с ПАО «ЛУКОЙЛ»" договора/доп. соглашения.
      if (currentStage.IsSubjectTransactionConfirmation == true &&
          (assig.SubjectTransaction == SubjectTransaction.OCorHMA || assig.SubjectTransaction == SubjectTransaction.NonCurrentAsset))
      {
        if (contract != null)
          contract.LukoilApproving = DirRX.Solution.Contract.LukoilApproving.Required;
        if (supAgreement != null)
          supAgreement.LukoilApproving = DirRX.Solution.Contract.LukoilApproving.Required;
      }
      #endregion

      #region Установка статуса скан-копии
      if (currentStage.IsAssignmentOnSigningScans == true)
      {
        var isSigned = assignment.Result == Sungero.Docflow.ApprovalCheckingAssignment.Result.Accept;
        Solution.Functions.Module.ChangeContractualDocSigningCopyState(document, isSigned);
      }
      #endregion
      
    }
    
    public override void EndBlock31(Sungero.Docflow.Server.ApprovalCheckingAssignmentEndBlockEventArguments e)
    {
      base.EndBlock31(e);
      
      if (e.CreatedAssignments.Cast<DirRX.Solution.IApprovalCheckingAssignment>().Any(a => a.ForRecycle.GetValueOrDefault()))
        _obj.IsRecycle = true;
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();

      #region Проверка одобрения по заявке на проверку контрагента.
      var stage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.SimpleAgr);
      if (document != null && stage != null && stage.NeedCounterpartyApproval == true)
      {
        var revisionRequest = PartiesControl.RevisionRequests.As(document);
        if (revisionRequest != null)
        {
          bool isRework = e.CreatedAssignments.Any(a => a.Result == Sungero.Docflow.ApprovalCheckingAssignment.Result.ForRework);
          
          if (!isRework)
          {
            var archiveResponsibleRole = DirRX.PartiesControl.PublicFunctions.Module.Remote.GetArchiveResponsibleRole();
            revisionRequest.AccessRights.Grant(archiveResponsibleRole, DefaultAccessRightsTypes.Change);
            revisionRequest.AccessRights.Save();
            
            foreach (var bindingDocument in revisionRequest.BindingDocuments.Where(bd => bd.Document != null).Select(bd => bd.Document))
            {
              bindingDocument.AccessRights.Grant(archiveResponsibleRole, DefaultAccessRightsTypes.Read);
              bindingDocument.AccessRights.Save();
            }
            
            foreach (var bindingDocument in revisionRequest.SecurityServiceDocuments.Where(bd => bd.Document != null).Select(bd => bd.Document))
            {
              bindingDocument.AccessRights.Grant(archiveResponsibleRole, DefaultAccessRightsTypes.Read);
              bindingDocument.AccessRights.Save();
            }
            
            revisionRequest.CheckingDate = Calendar.Today;
            revisionRequest.Save();
            
            var counterparty = revisionRequest.Counterparty;
            
            if (revisionRequest.ApprovalResult.HasValue)
            {
              var counterpartyApproveEnum = revisionRequest.ApprovalResult.Value == DirRX.PartiesControl.RevisionRequest.ApprovalResult.Approved ?
                DirRX.PartiesControl.CheckingResult.Decision.Approved : DirRX.PartiesControl.CheckingResult.Decision.NotApproved;
              
              var result = DirRX.PartiesControl.CheckingResults.GetAll().ToList()
                .FirstOrDefault(r => r.Decision == counterpartyApproveEnum &&
                                r.Reasons.Any(re => DirRX.PartiesControl.CheckingReasons.Equals(re.Reason, revisionRequest.CheckingReason)) &&
                                r.Types.Any(t => DirRX.PartiesControl.CheckingTypes.Equals(t.Type, revisionRequest.CheckingType)));
              
              revisionRequest.CheckingResult = result;
              counterparty.CheckingResult = result;
              counterparty.CounterpartyStatus = result.CounterpartyStatus;
              
              if (revisionRequest.ApprovalResult.Value == DirRX.PartiesControl.RevisionRequest.ApprovalResult.Approved &&
                  !DirRX.PartiesControl.CheckingTypes.Equals(revisionRequest.CheckingType, counterparty.CheckingType))
              {
                counterparty.CheckingType = revisionRequest.CheckingType;
                counterparty.Save();
              }
            }
            
            #region Выполнения задания при согласовании договоров.
            
            var waitingAssignments = DirRX.Solution.ApprovalCheckingAssignments.GetAll(a => a.Status == Sungero.Workflow.Assignment.Status.InProcess &&
                                                                                       a.ApprovalStage.IsAssignmentOnWaitingEndValidContractor == true);
            foreach (var assignment in waitingAssignments)
            {
              var contractualDocument = Sungero.Contracts.ContractualDocuments.As(assignment.DocumentGroup.OfficialDocuments.FirstOrDefault());
              var supAgreement = Solution.SupAgreements.As(contractualDocument);
              // Если указано доп. соглашение - может понадобиться договор из карточки доп. соглашения.
              var contract = supAgreement == null ? DirRX.Solution.Contracts.As(contractualDocument) : DirRX.Solution.Contracts.As(supAgreement.LeadingDocument);
              Logger.DebugFormat("End31: counterparty {0} contractualDocument {1}", counterparty.Name, contractualDocument.Name);
              
              // Для договора и ДС проверяем всех контрагентов по коллекции.
              if (contract != null && contract.Counterparties.Any(c => Solution.Companies.Equals(c.Counterparty, counterparty)))
              {
                var canAssignmentComplete = true;
                var totalApprovalResult = revisionRequest.ApprovalResult.Value;
                // Получим все заявки на одобрение контрагента по документу кроме заявки текущего контрагента.
                var revisionRequests = PartiesControl.RevisionRequests.GetAll(r => !Solution.Companies.Equals(r.Counterparty, counterparty) &&
                                                                              Sungero.Contracts.ContractualDocuments.Equals(r.MainDocument, contractualDocument));
                Logger.DebugFormat("End31: contractualDocument {0} revisionRequests {1}", contractualDocument.Name, revisionRequests.Count());
                // Проверим результат проверки по каждому контрагенту.
                foreach (var request in revisionRequests)
                {
                  var docCounterparty = request.Counterparty;
                  Logger.DebugFormat("End31: docCounterparty {0} docCounterparty.CheckingDate {1} request.Created {2}",
                                     docCounterparty.Name, docCounterparty.CheckingDate.ToString(), request.Created.ToString());
                  // Проверим, была ли выполнена проверка контрагента с момента создания заявки на проверку.
                  if (docCounterparty.CheckingDate != null && request.Created != null && docCounterparty.CheckingDate.Value > request.Created.Value)
                  {
                    var isStatusNotCorrectToCategory = Solution.PublicFunctions.Company.IsStatusNotCorrectToCategory(docCounterparty, Solution.ContractCategories.As(contract.DocumentGroup));
                    var isNotApproved = Solution.PublicFunctions.Company.IsNotApproved(docCounterparty);
                    // Если статус контрагента не соответствует категории договора или он не одобрен, тогда укажем общий результат "На доработку".
                    if (isStatusNotCorrectToCategory || isNotApproved)
                      totalApprovalResult = DirRX.PartiesControl.RevisionRequest.ApprovalResult.Aborted;
                    
                    Logger.DebugFormat("End31: docCounterparty {0} isStatusNotCorrectToCategory {1} isNotApproved {2}",
                                       docCounterparty.Name, isStatusNotCorrectToCategory.ToString(), isNotApproved.ToString());
                  }
                  else
                  {
                    // Если проверка еще не выполнена, то выполнять задание при согласовании договора еще рано.
                    canAssignmentComplete = false;
                  }
                  Logger.DebugFormat("End31: docCounterparty {0} totalApprovalResult {1} canAssignmentComplete {2}",
                                     docCounterparty.Name, totalApprovalResult.ToString(), canAssignmentComplete.ToString());
                }
                
                //Если все контрагенты проверены, тогда выполним задание при согласовании договора.
                if (canAssignmentComplete)
                {
                  assignment.Complete(totalApprovalResult == PartiesControl.RevisionRequest.ApprovalResult.Approved ?
                                      Solution.ApprovalCheckingAssignment.Result.Accept : Solution.ApprovalCheckingAssignment.Result.ForRework);
                }
                
              }
              else if (contractualDocument != null && Solution.Companies.Equals(contractualDocument.Counterparty, counterparty))
              {
                if (revisionRequest.ApprovalResult.Value == DirRX.PartiesControl.RevisionRequest.ApprovalResult.Approved)
                  assignment.Complete(DirRX.Solution.ApprovalCheckingAssignment.Result.Accept);
                else
                  assignment.Complete(DirRX.Solution.ApprovalCheckingAssignment.Result.ForRework);
              }
            }
            
            #endregion
            
            if (counterparty.CheckingType.DocProvision == PartiesControl.CheckingType.DocProvision.NotRequired)
              counterparty.IsDocumentsProvided = true;
            counterparty.CheckingResult = revisionRequest.CheckingResult;
            counterparty.CheckingDate = Calendar.Now;
            counterparty.Save();
          }
        }
      }
      #endregion
      
      #region Работа со статусами договоров.

      // Выполнение задания с признаком Задание на ожидание окончания проверки контрагента.
      if (document != null && stage != null && stage.IsAssignmentOnWaitingEndValidContractor == true)
      {
        var contract = Sungero.Contracts.ContractualDocuments.As(document);
        if (contract != null)
        {
          // Установка статуса "Проверка контрагента".
          ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(contract,
                                                                                   ContractsCustom.PublicConstants.Module.ContractStatusGuid.CounterpartyCheckingGuid,
                                                                                   ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus);
        }
      }

      if (document != null && stage != null && stage.IsAssignmentCorporateApproval == true)
      {
        var contract = Sungero.Contracts.ContractualDocuments.As(document);
        if (contract != null)
          ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(contract,
                                                                                   DirRX.ContractsCustom.PublicConstants.Module.ContractStatusGuid.CorpAcceptanceGuid,
                                                                                   ContractsCustom.PublicConstants.Module.ContractStatusType.ApprovalStatus);
      }
      
      #endregion
    }
    
    #endregion
    
    #region Задание.

    public override void StartBlock30(Sungero.Docflow.Server.ApprovalSimpleAssignmentArguments e)
    {
      return;
    }
    
    public override void StartAssignment30(Sungero.Docflow.IApprovalSimpleAssignment assignment, Sungero.Docflow.Server.ApprovalSimpleAssignmentArguments e)
    {
      base.StartAssignment30(assignment, e);
      assignment.Deadline = Functions.ApprovalTask.UpdateRelativeDeadline(assignment.Performer, assignment.Deadline);
    }

    #endregion
    
    #region Контроль возврата.
    
    public override void StartAssignment27(Sungero.Docflow.IApprovalCheckReturnAssignment assignment, Sungero.Docflow.Server.ApprovalCheckReturnAssignmentArguments e)
    {
      base.StartAssignment27(assignment, e);
      assignment.Deadline = Functions.ApprovalTask.UpdateRelativeDeadline(assignment.Performer, assignment.Deadline);
      
      // Дописывать в свойство DeliveryMethodDescription информацию о формате, в котором необходимо получить документ, на основании свойства в карточке этапа.
      var stage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.CheckReturn);
      if (stage != null && stage.KindOfDocumentNeedReturn != null)
      {
        var kindOfDocumentNeedReturn = DirRX.Solution.ApprovalStages.Info.Properties.KindOfDocumentNeedSend.GetLocalizedValue(stage.KindOfDocumentNeedReturn.Value);
        DirRX.Solution.ApprovalCheckReturnAssignments.As(assignment).DeliveryMethodDescription += DirRX.Solution.ApprovalTasks.Resources.KindOfDocumentNeedReturnFormat(kindOfDocumentNeedReturn);
      }
    }
    
    public override void CompleteAssignment27(Sungero.Docflow.IApprovalCheckReturnAssignment assignment, Sungero.Docflow.Server.ApprovalCheckReturnAssignmentArguments e)
    {
      base.CompleteAssignment27(assignment, e);
      
      var currentStage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.CheckReturn);
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      var contract = Solution.Contracts.As(document);
      var supAgreement = Solution.SupAgreements.As(document);
      var KindOfDocumentNeedReturnOriginal = currentStage.KindOfDocumentNeedReturn == Solution.ApprovalStage.KindOfDocumentNeedReturn.Original;
      // Изменение статуса «Подписание оригиналов к/а».
      if (KindOfDocumentNeedReturnOriginal)
      {
        // Поле "Подписание оригиналов к/а" меняется на «Подписан» или «Не подписан».
        if (contract != null)
          contract.ContractorOriginalSigning = assignment.Result == Sungero.Docflow.ApprovalCheckReturnAssignment.Result.Signed ?
            Solution.Contract.ContractorOriginalSigning.Signed : Solution.Contract.ContractorOriginalSigning.NotSigned;
        if (supAgreement != null)
          supAgreement.ContractorOriginalSigning = assignment.Result == Sungero.Docflow.ApprovalCheckReturnAssignment.Result.Signed ?
            Solution.SupAgreement.ContractorOriginalSigning.Signed : Solution.SupAgreement.ContractorOriginalSigning.NotSigned;
      }
      else
      {
        // Добавить строку с действием «Досыл оригинала» если поле "Подписание оригиналов к/а" пустое.
        var performer = Solution.Employees.As(assignment.Performer);
        if (contract != null && contract.ContractorOriginalSigning == null)
        {
          // Установка статуса "Подписана скан-копия со стороны Контрагента" или "Контрагент отказался подписать документ".
          var statusGuid = assignment.Result == Sungero.Docflow.ApprovalCheckReturnAssignment.Result.Signed ?
            DirRX.ContractsCustom.PublicConstants.Module.ContractStatusGuid.ScanSignedByCounterpartyGuid :
            DirRX.ContractsCustom.PublicConstants.Module.ContractStatusGuid.CounterpartyRejectedSigningGuid;
          
          ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(contract,
                                                                                statusGuid,
                                                                                DirRX.ContractsCustom.PublicConstants.Module.ContractStatusType.ScanMoveStatus,
                                                                                true);

          if (!(contract.ContractActivate == Solution.Contract.ContractActivate.Copy &&
                contract.IsContractorSignsFirst != true && contract.IsScannedImageSign == true))
          {
            // TODO Дефект платформы, нет возможности по-другому привести типы у перекрытой коллекции.
            Solution.IContractTracking issue = contract.Tracking.AddNew() as IContractTracking;
            issue.DeliveredTo = performer;
            issue.Action = Solution.ContractTracking.Action.OriginalSend;
            issue.DeliveryDate = Calendar.GetUserToday(performer);
            issue.IsOriginal = true;
            issue.ReturnTask = _obj;
            issue.Format = Solution.ContractTracking.Format.Original;
          }
        }
        if (supAgreement != null && supAgreement.ContractorOriginalSigning == null)
        {
          // Установка статуса "Подписана скан-копия со стороны Контрагента" или "Контрагент отказался подписать документ".
          var statusGuid = assignment.Result == Sungero.Docflow.ApprovalCheckReturnAssignment.Result.Signed ?
            DirRX.ContractsCustom.PublicConstants.Module.ContractStatusGuid.ScanSignedByCounterpartyGuid :
            DirRX.ContractsCustom.PublicConstants.Module.ContractStatusGuid.CounterpartyRejectedSigningGuid;
          
          if (assignment.Result == Sungero.Docflow.ApprovalCheckReturnAssignment.Result.Signed)
            ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(supAgreement,
                                                                                  statusGuid,
                                                                                  DirRX.ContractsCustom.PublicConstants.Module.ContractStatusType.ScanMoveStatus,
                                                                                  true);
          
          
          if (!(supAgreement.ContractActivate == Solution.Contract.ContractActivate.Copy &&
                supAgreement.IsContractorSignsFirst != true && supAgreement.IsScannedImageSign == true))
          {
            // TODO Дефект платформы, нет возможности по-другому привести типы у перекрытой коллекции.
            Solution.ISupAgreementTracking issue = supAgreement.Tracking.AddNew() as ISupAgreementTracking;
            issue.DeliveredTo = performer;
            issue.Action = Solution.SupAgreementTracking.Action.OriginalSend;
            issue.DeliveryDate = Calendar.GetUserToday(performer);
            issue.IsOriginal = true;
            issue.ReturnTask = _obj;
            issue.Format = Solution.SupAgreementTracking.Format.Original;
          }
        }
      }
      
      // Прекратить задачу, если контрагент не подписал документ.
      if (assignment.Result == Sungero.Docflow.ApprovalCheckReturnAssignment.Result.NotSigned)
      {
        var subject = DirRX.Solution.Resources.CounterpartyNotSignedFormat(assignment.ActiveText);
        PublicFunctions.Module.Remote.CreateAuthorNotice(assignment, subject);
        PublicFunctions.Module.Remote.AbortTask(_obj.Id);
      }
    }
    
    public override void EndBlock27(Sungero.Docflow.Server.ApprovalCheckReturnAssignmentEndBlockEventArguments e)
    {
      base.EndBlock27(e);
    }
    
    #endregion
    
    #region Отправка контрагенту.
    
    public override void StartAssignment28(Sungero.Docflow.IApprovalSendingAssignment assignment, Sungero.Docflow.Server.ApprovalSendingAssignmentArguments e)
    {
      base.StartAssignment28(assignment, e);
      assignment.Deadline = Functions.ApprovalTask.UpdateRelativeDeadline(assignment.Performer, assignment.Deadline);
      // Дописывать в свойство DeliveryMethodDescription информацию о формате, в котором необходимо отправить документ, на основании свойства в карточке этапа.
      var stage = DirRX.Solution.ApprovalStages.As(e.Block.Stage);
      if (stage != null && stage.KindOfDocumentNeedSend != null)
      {
        var kindOfDocumentNeedSend = DirRX.Solution.ApprovalStages.Info.Properties.KindOfDocumentNeedSend.GetLocalizedValue(stage.KindOfDocumentNeedSend.Value);
        assignment.DeliveryMethodDescription = DirRX.Solution.ApprovalTasks.Resources.KindOfDocumentNeedSendFormat(kindOfDocumentNeedSend);
      }
    }
    
    public override void CompleteAssignment28(Sungero.Docflow.IApprovalSendingAssignment assignment, Sungero.Docflow.Server.ApprovalSendingAssignmentArguments e)
    {
      base.CompleteAssignment28(assignment, e);
      
      var currentStage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.Sending);
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      var contract = Solution.Contracts.As(document);
      var supAgreement = Solution.SupAgreements.As(document);
      var kindOfDocumentNeedSend = currentStage.KindOfDocumentNeedSend;
      
      if (contract != null)
      {
        var tracking = contract.Tracking.Where(t => t.Action == Solution.ContractTracking.Action.Sending || t.Action == Solution.ContractTracking.Action.Endorsement);
        foreach (Solution.IContractTracking record in tracking)
        {
          record.Format = kindOfDocumentNeedSend;
          record.IsOriginal = kindOfDocumentNeedSend == Solution.ApprovalStage.KindOfDocumentNeedSend.Original;

        }
      }
      
      if (supAgreement != null)
      {
        var tracking = supAgreement.Tracking.Where(t => t.Action == Solution.SupAgreementTracking.Action.Sending || t.Action == Solution.SupAgreementTracking.Action.Endorsement);
        foreach (Solution.ISupAgreementTracking record in tracking)
        {
          record.Format = kindOfDocumentNeedSend;
          record.IsOriginal = kindOfDocumentNeedSend == Solution.ApprovalStage.KindOfDocumentNeedSend.Original;
        }
      }
    }

    #endregion
    
    #region Создание поручений.

    public override void StartAssignment37(Sungero.Docflow.IApprovalExecutionAssignment assignment, Sungero.Docflow.Server.ApprovalExecutionAssignmentArguments e)
    {
      base.StartAssignment37(assignment, e);
      assignment.Deadline = Functions.ApprovalTask.UpdateRelativeDeadline(assignment.Performer, assignment.Deadline);
    }

    #endregion
    
    #region Рассмотрение.
    
    public override void StartAssignment36(Sungero.Docflow.IApprovalReviewAssignment assignment, Sungero.Docflow.Server.ApprovalReviewAssignmentArguments e)
    {
      base.StartAssignment36(assignment, e);
      assignment.Deadline = Functions.ApprovalTask.UpdateRelativeDeadline(assignment.Performer, assignment.Deadline);
    }
    
    #endregion
    
    #region Подписание (утверждение).
    
    public override void StartAssignment9(Sungero.Docflow.IApprovalSigningAssignment assignment, Sungero.Docflow.Server.ApprovalSigningAssignmentArguments e)
    {
      base.StartAssignment9(assignment, e);
      assignment.Deadline = Functions.ApprovalTask.UpdateRelativeDeadline(assignment.Performer, assignment.Deadline);
      
      //Заполнение поля "Подписал" в задании полем "На подпись" из карточки задачи.
      if (_obj.Signatory != null)
        DirRX.Solution.ApprovalSigningAssignments.As(assignment).SignedDirRX = DirRX.Solution.Employees.As(_obj.Signatory);
      
      if (assignment.IsCollapsed == true)
      {
        var collapsedSendingStage = Functions.ApprovalTask.GetCollapsedStage(_obj, _obj.StageNumber, Sungero.Docflow.ApprovalStage.StageType.Sending);
        if (collapsedSendingStage != null && collapsedSendingStage.KindOfDocumentNeedSend != null)
        {
          // Дописывать в свойство DeliveryMethodDescription информацию о формате, в котором необходимо отправить документ, на основании свойства в карточке этапа.
          var kindOfDocumentNeedSend = DirRX.Solution.ApprovalStages.Info.Properties.KindOfDocumentNeedSend.GetLocalizedValue(collapsedSendingStage.KindOfDocumentNeedSend.Value);
          assignment.DeliveryMethodDescription = DirRX.Solution.ApprovalTasks.Resources.KindOfDocumentNeedSendFormat(kindOfDocumentNeedSend);
        }
      }
    }
    
    public override void CompleteAssignment9(Sungero.Docflow.IApprovalSigningAssignment assignment, Sungero.Docflow.Server.ApprovalSigningAssignmentArguments e)
    {
      base.CompleteAssignment9(assignment, e);
      
      if (assignment.Result == Sungero.Docflow.ApprovalSigningAssignment.Result.ConfirmSign || assignment.Result == Sungero.Docflow.ApprovalSigningAssignment.Result.Sign)
      {
        var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
        if (document != null)
        {
          var memoForPayments = DirRX.ContractsCustom.MemoForPayments.As(document);
          Functions.ApprovalTask.ResetCounterpartyStatus(memoForPayments);
          
          var contractualDoc = Sungero.Contracts.ContractualDocuments.As(document);
          if (contractualDoc != null && contractualDoc.HasVersions && contractualDoc.LastVersion.PublicBody != null
              && contractualDoc.LastVersion.AssociatedApplication != Sungero.Content.AssociatedApplications.GetByExtension("pdf"))
          {
            try
            {
              DirRX.ContractsCustom.PublicFunctions.Module.Remote.ConvertDocumentToPdfWithSBarcode(document);
            }
            catch (Exception ex)
            {
              Logger.Error(ex.Message);
            }
          }
        }
        
        if (assignment.IsCollapsed == true)
        {
          // Если этап подписания схлопнут с отправкой контрагенту.
          var collapsedSendingStage = Functions.ApprovalTask.GetCollapsedStage(_obj, _obj.StageNumber, Sungero.Docflow.ApprovalStage.StageType.Sending);
          if (collapsedSendingStage != null)
          {
            // При добавлении информации о выдаче документа изменим признак Оригинала.
            var contract = Solution.Contracts.As(document);
            var supAgreement = Solution.SupAgreements.As(document);
            var kindOfDocumentNeedSend = collapsedSendingStage.KindOfDocumentNeedSend;
            
            if (contract != null)
            {
              var tracking = contract.Tracking.Where(t => t.Action == Solution.ContractTracking.Action.Sending || t.Action == Solution.ContractTracking.Action.Endorsement);
              foreach (Solution.IContractTracking record in tracking)
              {
                record.Format = kindOfDocumentNeedSend;
                record.IsOriginal = kindOfDocumentNeedSend == Solution.ApprovalStage.KindOfDocumentNeedSend.Original;
              }
            }
            
            if (supAgreement != null)
            {
              var tracking = supAgreement.Tracking.Where(t => t.Action == Solution.SupAgreementTracking.Action.Sending || t.Action == Solution.SupAgreementTracking.Action.Endorsement);
              foreach (Solution.ISupAgreementTracking record in tracking)
              {
                record.Format = kindOfDocumentNeedSend;
                record.IsOriginal = kindOfDocumentNeedSend == Solution.ApprovalStage.KindOfDocumentNeedSend.Original;
              }
            }
          }
        }
      }
      
      if (assignment.Result == Sungero.Docflow.ApprovalSigningAssignment.Result.Abort)
      {
        var memoForPayments = DirRX.ContractsCustom.MemoForPayments.As(_obj.DocumentGroup.OfficialDocuments.FirstOrDefault());
        if (memoForPayments != null)
        {
          memoForPayments.LifeCycleState = DirRX.ContractsCustom.MemoForPayment.LifeCycleState.Obsolete;
          Functions.ApprovalTask.ResetCounterpartyStatus(memoForPayments);
          memoForPayments.Save();
        }
      }
    }
    
    public override void EndBlock9(Sungero.Docflow.Server.ApprovalSigningAssignmentEndBlockEventArguments e)
    {
      base.EndBlock9(e);
      
      if (e.CreatedAssignments.Cast<DirRX.Solution.IApprovalSigningAssignment>().Any(a => a.ForRecycle.GetValueOrDefault()))
        _obj.IsRecycle = true;
    }

    #endregion
    
    #region Печать.
    
    public override void StartAssignment20(Sungero.Docflow.IApprovalPrintingAssignment assignment, Sungero.Docflow.Server.ApprovalPrintingAssignmentArguments e)
    {
      base.StartAssignment20(assignment, e);
      assignment.Deadline = Functions.ApprovalTask.UpdateRelativeDeadline(assignment.Performer, assignment.Deadline);
    }
    
    #endregion
    
    #region Регистрация.
    
    public override void StartBlock23(Sungero.Docflow.Server.ApprovalRegistrationAssignmentArguments e)
    {
      base.StartBlock23(e);
      // Установить свойство "На регистрации" для договора или доп. соглашения.
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var contract = Solution.Contracts.As(document);
      var supAgreement = Solution.SupAgreements.As(document);
      if (contract != null)
        contract.OnRegistration = true;
      if (supAgreement != null)
        supAgreement.OnRegistration = true;
    }

    public override void StartAssignment23(Sungero.Docflow.IApprovalRegistrationAssignment assignment, Sungero.Docflow.Server.ApprovalRegistrationAssignmentArguments e)
    {
      base.StartAssignment23(assignment, e);
      assignment.Deadline = Functions.ApprovalTask.UpdateRelativeDeadline(assignment.Performer, assignment.Deadline);
    }
    
    public override void EndBlock23(Sungero.Docflow.Server.ApprovalRegistrationAssignmentEndBlockEventArguments e)
    {
      base.EndBlock23(e);
      // Установить свойство "На регистрации" для договора или доп. соглашения.
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var contract = Solution.Contracts.As(document);
      var supAgreement = Solution.SupAgreements.As(document);
      if (contract != null)
        contract.OnRegistration = false;
      if (supAgreement != null)
        supAgreement.OnRegistration = false;
    }
    
    #endregion
    
    #region Согласование с руководителем.
    
    public override void StartAssignment3(Sungero.Docflow.IApprovalManagerAssignment assignment, Sungero.Docflow.Server.ApprovalManagerAssignmentArguments e)
    {
      base.StartAssignment3(assignment, e);
      assignment.Deadline = Functions.ApprovalTask.UpdateRelativeDeadline(assignment.Performer, assignment.Deadline);
    }
    
    #endregion
    
    #region Согласование с согласующими.
    
    public override void StartBlock6(Sungero.Docflow.Server.ApprovalAssignmentArguments e)
    {
      base.StartBlock6(e);
      _obj.LastStageWithRisk = false;
    }
    
    public override void StartAssignment6(Sungero.Docflow.IApprovalAssignment assignment, Sungero.Docflow.Server.ApprovalAssignmentArguments e)
    {
      base.StartAssignment6(assignment, e);
      assignment.Deadline = Functions.ApprovalTask.UpdateRelativeDeadline(assignment.Performer, assignment.Deadline);
    }
    
    public override void CompleteAssignment6(Sungero.Docflow.IApprovalAssignment assignment, Sungero.Docflow.Server.ApprovalAssignmentArguments e)
    {
      var dictionary = Functions.ApprovalTask.GetCurrentAttachmentsRights(_obj);
      base.CompleteAssignment6(assignment, e);
      Functions.ApprovalTask.RestoreAttachmentsRights(dictionary);
      
      var task = Solution.ApprovalTasks.As(_obj);
      if (!task.LastStageWithRisk.GetValueOrDefault() && Solution.ApprovalAssignments.As(assignment).ApprovedWithRisk.GetValueOrDefault())
        task.LastStageWithRisk = true;
      // При выполнении задания с результатом  "Отклонить", инициатору согласования, должно приходить уведомление (подзадачей).
      if (DirRX.Solution.ApprovalAssignments.As(assignment).Rejected.GetValueOrDefault())
      {
        //  Тема - Согласование с сотрудником <Фамилия И.О., должность> отклонено по причине:<Комментарий>.
        var performerName = assignment.Performer.Name;
        var performerJobName = string.Empty;
        if (DirRX.Solution.Employees.Is(assignment.Performer) && DirRX.Solution.Employees.As(assignment.Performer).JobTitle != null)
          performerJobName = DirRX.Solution.Employees.As(assignment.Performer).JobTitle.Name;
        var subject = DirRX.Solution.ApprovalTasks.Resources.RejectSubjectTextFormat(performerName, performerJobName, assignment.ActiveText).ToString();
        if (subject.Length > 250)
          subject = subject.Substring(0, 250);
        PublicFunctions.Module.Remote.CreateAuthorNotice(assignment,  subject);
      }
    }
    
    
    public override void EndBlock6(Sungero.Docflow.Server.ApprovalAssignmentEndBlockEventArguments e)
    {
      base.EndBlock6(e);
      
      if (e.CreatedAssignments.Cast<DirRX.Solution.IApprovalAssignment>().Any(a => a.ForRecycle.GetValueOrDefault()))
        _obj.IsRecycle = true;
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var stage = Functions.ApprovalTask.GetStage(_obj, Sungero.Docflow.ApprovalStage.StageType.Approvers);
      if (document != null && stage != null && stage.NeedBarcode == true)
      {
        bool isRework = e.CreatedAssignments.Any(a => a.Result == Sungero.Docflow.ApprovalAssignment.Result.ForRevision);
        if (!isRework)
        {
          var contractualDoc = Sungero.Contracts.ContractualDocuments.As(document);
          if (contractualDoc != null && contractualDoc.HasVersions && contractualDoc.LastVersion.PublicBody != null
              && contractualDoc.LastVersion.AssociatedApplication != Sungero.Content.AssociatedApplications.GetByExtension("pdf"))
          {
            // Сконвертировать документ в PDF и вставить штрихкод.
            var asyncConvertHandler = DirRX.ContractsCustom.AsyncHandlers.ConvertDocumentToPDFInsertBarcode.Create();
            asyncConvertHandler.DocumentId = contractualDoc.Id;
            asyncConvertHandler.ExecuteAsync();
          }
        }
      }
    }
    
    #endregion

    #region Доработка.
    
    public override void StartBlock5(Sungero.Docflow.Server.ApprovalReworkAssignmentArguments e)
    {
      base.StartBlock5(e);
      
      var approvalAssignments = ApprovalAssignments.GetAll(a => Equals(a.Task, _obj) && a.Created >= _obj.Started).ToList();
      // При результате "На переработку" обрабатываем исполнителей добавленных в базовой функции,
      // т.к. признак согласования с уменьшающимся кругом действует для правила и соответственно меняется для всех задач.
      if (_obj.IsRecycle.GetValueOrDefault())
      {
        e.Block.Subject = e.Block.Subject.Replace(DirRX.Solution.ApprovalTasks.Resources.ReworkText, DirRX.Solution.ApprovalTasks.Resources.RecycleText);
        
        foreach (var approver in e.Block.Approvers)
        {
          // Исключаем исполнителей, которые переадресовали задание.
          var approvalAssignment = approvalAssignments
            .Where(a => Equals(a.Performer, approver.Approver))
            .OrderByDescending(i => i.Modified)
            .FirstOrDefault();
          
          var forwarded = approvalAssignment != null && approvalAssignment.Result == Sungero.Docflow.ApprovalAssignment.Result.Forward;
          
          if (forwarded)
            approver.Action = Sungero.Docflow.ApprovalReworkAssignmentApprovers.Action.DoNotSend;
          else
            approver.Action = Sungero.Docflow.ApprovalReworkAssignmentApprovers.Action.SendForApproval;
        }
      }
      else
      {
        foreach (var risk in _obj.RiskAttachmentGroup.Risks.Where(x => x.Status == LocalActs.Risk.Status.Active))
        {
          var approver = e.Block.Approvers.Where(x => Employees.Equals(x.Approver, risk.Author)).FirstOrDefault();
          approver.Action = Sungero.Docflow.ApprovalReworkAssignmentApprovers.Action.SendForApproval;
        }
      }
      
      // Исключать сотрудников отклонивших согласование
      foreach (var approver in e.Block.Approvers)
      {
        var rejectedAssignment = approvalAssignments
          .Where(a => Equals(a.Performer, approver.Approver) && a.Rejected == true)
          .OrderByDescending(i => i.Modified)
          .FirstOrDefault();
        
        if (rejectedAssignment != null)
          approver.Action = Sungero.Docflow.ApprovalReworkAssignmentApprovers.Action.DoNotSend;
      }
    }
    
    public override void StartAssignment5(Sungero.Docflow.IApprovalReworkAssignment assignment, Sungero.Docflow.Server.ApprovalReworkAssignmentArguments e)
    {
      base.StartAssignment5(assignment, e);
      assignment.Deadline = Functions.ApprovalTask.UpdateRelativeDeadline(assignment.Performer, assignment.Deadline);
      Solution.ApprovalReworkAssignments.As(assignment).NeedPaperSigning = _obj.NeedPaperSigning.GetValueOrDefault();
    }
    
    public override void CompleteAssignment5(Sungero.Docflow.IApprovalReworkAssignment assignment, Sungero.Docflow.Server.ApprovalReworkAssignmentArguments e)
    {
      base.CompleteAssignment5(assignment, e);
      _obj.NeedPaperSigning = Solution.ApprovalReworkAssignments.As(assignment).NeedPaperSigning;
    }
    
    public override void EndBlock5(Sungero.Docflow.Server.ApprovalReworkAssignmentEndBlockEventArguments e)
    {
      base.EndBlock5(e);

      _obj.IsRecycle = false;
      
      // Меняем статус риска у тех, кому ушло задание на повторное согласование и риск не был согласован.
      var approvers = e.Block.Approvers.Where(x => x.Action == Sungero.Docflow.ApprovalReworkAssignmentApprovers.Action.SendForApproval).Select(x => x.Approver).ToList();
      var activeRisks = _obj.RiskAttachmentGroup.Risks.Where(x => x.Status == LocalActs.Risk.Status.Active && approvers.Contains(x.Author));
      foreach (var risk in activeRisks)
      {
        risk.Status = LocalActs.Risk.Status.Closed;
        risk.Save();
      }
      
      // Вернуть статус согласования контрагента, если при доработке не стали формировать протокол разногласий и CounterpartyApprovalState - Подписан.
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var contract = Solution.Contracts.As(document);
      var supAgreement = Solution.SupAgreements.As(document);
      if (contract != null && contract.CounterpartyApprovalState == Solution.Contract.CounterpartyApprovalState.Signed)
      {
        contract.ExternalApprovalState = Solution.Contract.ExternalApprovalState.Signed;
        _obj.DocumentExternalApprovalState = DocumentExternalApprovalState.Signed;
      }
      if (supAgreement != null && supAgreement.CounterpartyApprovalState == Solution.SupAgreement.CounterpartyApprovalState.Signed)
      {
        supAgreement.ExternalApprovalState = Solution.SupAgreement.ExternalApprovalState.Signed;
        _obj.DocumentExternalApprovalState = DocumentExternalApprovalState.Signed;
      }
    }

    #endregion

    #region Уведомления.
    public override void StartBlock33(Sungero.Docflow.Server.ApprovalSimpleNotificationArguments e)
    {
      // TODO: Убрать в случае исправление в стандартной разработке.
      var ruleStage = _obj.ApprovalRule.Stages.Where(s => s.Stage.StageType == Sungero.Docflow.ApprovalStage.StageType.Notice).FirstOrDefault(s => s.Number == _obj.StageNumber);
      if (ruleStage == null || ruleStage.Stage == null)
        return;
      var stage = ruleStage.Stage;

      // Задать тему.
      var mainDocument = _obj.DocumentGroup.OfficialDocuments.First();
      e.Block.Subject = Sungero.Docflow.PublicFunctions.ApprovalRuleBase.FormatStageSubject(stage, mainDocument);
      
      // Задать исполнителей.
      var performers = Sungero.Docflow.PublicFunctions.ApprovalStage.Remote.GetStagePerformers(_obj, stage);
      
      // Если исполнитель один и предыдущее задание было ему же, то уведомление не посылать.
      if (performers.Count == 1)
      {
        var lastAssignment = Assignments.GetAll()
          .Where(a => Equals(a.Task, _obj))
          .OrderByDescending(a => a.Completed)
          .FirstOrDefault(a => a.Status == Sungero.Workflow.AssignmentBase.Status.Completed);
        
        if (lastAssignment != null && Equals(lastAssignment.Performer, performers.First()))
          performers.Clear();
      }
      
      foreach (var performer in performers)
        e.Block.Performers.Add(performer);
      
      // Выдать права на документы.
      var recipients = Sungero.Docflow.PublicFunctions.ApprovalStage.Remote.GetStageRecipients(stage, _obj);
      foreach (var performer in recipients)
      {
        // На основной документ на чтение.
        var approvalDocument = _obj.DocumentGroup.OfficialDocuments.First();
        if (!approvalDocument.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, performer))
          approvalDocument.AccessRights.Grant(performer, DefaultAccessRightsTypes.Read);
        
        // На приложения на чтение.
        foreach (var document in _obj.AddendaGroup.OfficialDocuments)
        {
          if (document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, performer))
            continue;
          
          document.AccessRights.Grant(performer, DefaultAccessRightsTypes.Read);
        }
      }
    }
    #endregion
  }
}