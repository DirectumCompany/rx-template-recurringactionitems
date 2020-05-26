using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalTask;

namespace DirRX.Solution.Client
{
  partial class ApprovalTaskActions
  {
    public override void Abort(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (DirRX.Solution.Contracts.Is(document) || DirRX.Solution.SupAgreements.Is(document))
      {
        var error = _obj.Status == Status.Draft ? string.Empty : Sungero.Docflow.PublicFunctions.Module.Remote.GetTaskAbortingError(_obj, Sungero.Docflow.Constants.Module.TaskMainGroup.ApprovalTask.ToString());
        if (!string.IsNullOrWhiteSpace(error))
          e.AddError(error);
        else if (this.GetReasonBeforeAbort(_obj, null))
        {
          var task = Sungero.Workflow.Tasks.As(_obj);
          task.Abort();
        }
        _obj.Save();
      }
      else
        base.Abort(e);
    }
    
    /// <summary>
    /// Вывод диалога запроса причины прекращения задачи согласования.
    /// </summary>
    /// <param name="activeText">Причина прекращения.</param>
    /// <returns>True, если пользователь нажал Ok.</returns>
    public bool GetReasonBeforeAbort(IApprovalTask task, string activeText)
    {
      var dialog = Dialogs.CreateInputDialog(ApprovalTasks.Resources.Confirmation);
      var abortingReason = dialog.AddMultilineString(task.Info.Properties.AbortingReason.LocalizedName, true, activeText);
      var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
      CommonLibrary.IBooleanDialogValue isDocumentClosed = null;
      if (document != null)
      {
        isDocumentClosed = dialog.AddBoolean(DirRX.Solution.ApprovalTasks.Resources.SetAsObsolete, true);
        dialog.SetOnButtonClick(args =>
                                {
                                  if (string.IsNullOrWhiteSpace(abortingReason.Value))
                                    args.AddError(ApprovalTasks.Resources.EmptyAbortingReason, abortingReason);
                                });
      }
      
      if (dialog.Show() == DialogButtons.Ok)
      {
        task.AbortingReason = abortingReason.Value;
        if (isDocumentClosed != null && isDocumentClosed.Value.Value == true)
        {
          document.LifeCycleState = DirRX.Solution.Contract.LifeCycleState.Obsolete;
          document.Save();
        }
        return true;
      }
      return false;
    }

    public override bool CanAbort(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanAbort(e);
    }


    public override void Start(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      if (!Functions.ApprovalTask.Remote.HasDocumentAndCanRead(_obj))
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      #region Проверка наличия рег. документа

      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      
      var order = DirRX.Solution.Orders.As(document);
      if (order != null && order.StandardForm != null && order.StandardForm.NeedRegulatoryDocument.GetValueOrDefault()
          && order.RegulatoryDocument == null)
      {
        e.AddError(DirRX.Solution.Orders.Resources.NeedRegulatoryDocError);
        return;
      }
      #endregion
      
      #region Проверка заявки на проверку контрагента.
      
      if (document != null && DirRX.PartiesControl.RevisionRequests.Is(document))
      {
        // Получение данных с сервера для повторной проверки вложенности документов.
        var revisionRequests = PartiesControl.PublicFunctions.RevisionRequest.Remote.GetRevisionRequest(document.Id);
        
        var revisionRequest = DirRX.Solution.PublicFunctions.ApprovalTask.Remote.GetRevisionRequestOfTaskInProcess(revisionRequests.Counterparty);
        if (revisionRequest != null)
        {
          e.AddError(Solution.ApprovalTasks.Resources.ApprovalRevisionRequestExistFormat(revisionRequest.CheckingReason.Name));
          return;
        }
        
        if (revisionRequests.Counterparty.IsDocumentsProvided.GetValueOrDefault())
        {
          var lockInfo = Locks.GetLockInfo(revisionRequests.Counterparty);
          if (lockInfo.IsLockedByOther)
          {
            e.AddError(lockInfo.LockedMessage);
            return;
          }
        }
        
        if (revisionRequests != null && revisionRequests.CheckingType != null)
        {
          bool isContinue = true;

          if (revisionRequests.CheckingType.DocProvision ==  PartiesControl.CheckingType.DocProvision.Desirable)
            isContinue = Functions.ApprovalTask.CheckDesirableBindingsDocumnets(_obj, revisionRequests);
          
          if (revisionRequests.CheckingType.DocProvision ==  PartiesControl.CheckingType.DocProvision.Necessarily)
            isContinue = Functions.ApprovalTask.CheckRequiredBindingsDocumnets(_obj, revisionRequests);
          
          if (!isContinue)
            return;
        }
      }
      #endregion
      
      #region Из базового обработчика.
      
      if (Sungero.Docflow.PublicFunctions.Module.ShowDialogGrantAccessRights(_obj, _obj.OtherGroup.All.ToList(), _obj.AddApprovers.Select(a => a.Approver).ToList()) == false)
        return;
      
      if (document != null)
      {
        if (!document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, _obj.Author))
          Functions.ApprovalTask.Remote.GrantRightsToAuthorSolution(_obj);
        
        // Если инициатор указан в этапе согласования, то установить его подпись сразу.
        var approvalStage = Functions.ApprovalTask.Remote.AuthorMustApproveDocumentSolution(_obj, _obj.Author, _obj.AddApprovers.Select(app => app.Approver).ToList());
        
        var author = _obj.Author;
        var documentHasBody = document.Versions.Any();
        
        Sungero.Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);
        
        // Запросить подтверждение подписания и отправки.
        var question = ApprovalTasks.Resources.AreYouSureYouWantSendDocumentForApproval;
        if (approvalStage.HasApprovalStage)
        {
          if (documentHasBody)
            question = ApprovalTasks.Resources.AreYouSureYouWantSignAndSendDocumentForApproval;
          if (_obj.AddendaGroup.OfficialDocuments.Any())
            question = ApprovalTasks.Resources.AreYouSureYouWantSignAndSendDocumentAndAddendaForApproval;
        }
        var dialog = Dialogs.CreateTaskDialog(question, MessageType.Question);
        dialog.Buttons.AddOkCancel();
        dialog.Buttons.Default = DialogButtons.Ok;
        if (dialog.Show() != DialogButtons.Ok)
          return;
        
        if (approvalStage.HasApprovalStage)
        {
          if (documentHasBody && approvalStage.NeedStrongSign && !Functions.ApprovalTask.GetCertificates().Any())
          {
            e.AddError(ApprovalTasks.Resources.CertificateNeeded);
            return;
          }
          
          try
          {
            if (!Sungero.Docflow.PublicFunctions.OfficialDocument.EndorseWithAddenda(document, _obj.AddendaGroup.OfficialDocuments.ToList(), null, string.Empty, author))
            {
              e.AddError(ApprovalTasks.Resources.ToStartNeedSignDocument);
              return;
            }
          }
          catch (CommonLibrary.Exceptions.PlatformException ex)
          {
            if (!ex.IsInternal)
            {
              var message = string.Format("{0}.", ex.Message.TrimEnd('.'));
              e.AddError(message);
              return;
            }
            else
              throw;
          }
        }
      }
      
      #endregion
      
      #region Диалог подтверждения
      
      var contractualDocument = document != null ? Sungero.Contracts.ContractualDocuments.As(document) : null;
      
      if (contractualDocument != null)
      {
        // Признак необходимости отправить запрос для контрагента после диалога.
        var dialogMessage = new List<string>();
        var createRevisionRequest = false;
        var createRevisionRequestCounterparties = new List<Solution.ICompany>();
        var needCounterpartyChecking = false;
        var needCheckingCounterparties = new List<Solution.ICompany>();
        var needComment = true;
        
        var supAgreement = Solution.SupAgreements.As(document);
        // Если указано доп. соглашение - может понадобиться договор из карточки доп. соглашения.
        var contract = supAgreement == null ? DirRX.Solution.Contracts.As(document) : DirRX.Solution.Contracts.As(supAgreement.LeadingDocument);
        
        if (contract != null && contract.DocumentGroup == null)
        {
          e.AddError(DirRX.Solution.ApprovalTasks.Resources.NeedCategoryMessage);
          return;
        }
        
        // Проверка статуса контрагента у договорных документов. Не проверять служебного тендерного контрагента.
        var tenderCompany = ContractsCustom.PublicFunctions.Module.Remote.GetTenderPurchaseCounterparty();
        if (Solution.DocumentKinds.As(document.DocumentKind).NotCheckCounterparty != true && contractualDocument.Counterparty != null &&
            !Sungero.Parties.Counterparties.Equals(contractualDocument.Counterparty, tenderCompany))
        {
          // Для договора и ДС проверяем контрагентов по коллекции + соберем списки с контрагентами для проверки.
          if (contract != null)
          {
            foreach (var cRow in contract.Counterparties)
            {
              var counterparty = cRow.Counterparty;
              var cNeedCounterpartyChecking = Solution.PublicFunctions.Company.IsStatusNotCorrectToCategory(counterparty, Solution.ContractCategories.As(contract.DocumentGroup));
              Logger.DebugFormat("Task start: counterparty {0} IsStatusNotCorrectToCategory {1}", counterparty.Name, cNeedCounterpartyChecking.ToString());
              
              if (cNeedCounterpartyChecking)
              {
                needCheckingCounterparties.Add(counterparty);
                
                var isNotApproved = Solution.PublicFunctions.Company.IsNotApproved(counterparty);
                var isStopList = Solution.PublicFunctions.Company.IsStopList(counterparty);
                var cNeedCreateRevisionRequest = Solution.PublicFunctions.Company.NeedCreateRevisionRequest(counterparty, cNeedCounterpartyChecking);
                Logger.DebugFormat("Task start: counterparty {0} isNotApproved {1} isStopList {2} cNeedCreateRevisionRequest {3}", 
                                   counterparty.Name, isNotApproved.ToString(), isStopList.ToString(), cNeedCreateRevisionRequest.ToString());
                
                // Если статус «Стоп-лист».
                if (isStopList)
                  dialogMessage.Add(DirRX.Solution.ApprovalTasks.Resources.SendApprovalDialogMessageStoplistStatusHyperlinkFormat(counterparty.Name));
                // Если контрагент не одобрен.
                else if (isNotApproved)
                  dialogMessage.Add(DirRX.Solution.ApprovalTasks.Resources.SendApprovalDialogMessageNotAllowedStatusHyperlinkFormat(counterparty.Name));
                
                if (cNeedCreateRevisionRequest)
                {
                  // Создаем заявку на проверку контрагента.
                  createRevisionRequestCounterparties.Add(counterparty);
                  dialogMessage.Add(DirRX.Solution.ApprovalTasks.Resources.SendApprovalDialogMessageCheckingRequiredStatusHyperlinkFormat(counterparty.Name));
                }
              }
            }
            needCounterpartyChecking = needCheckingCounterparties.Count > 0;
            createRevisionRequest = createRevisionRequestCounterparties.Count > 0;
            needComment = !needCounterpartyChecking || createRevisionRequestCounterparties.Count < needCheckingCounterparties.Count;
          }
          // Для служебки смотрим проверяем контрагента по свойству в карточке.
          else
          {
            var counterparty = DirRX.Solution.Companies.As(contractualDocument.Counterparty);
            var isNotApproved = Solution.PublicFunctions.Company.IsNotApproved(counterparty);
            var isStopList = Solution.PublicFunctions.Company.IsStopList(counterparty);
            var isCheckingRequired = Solution.PublicFunctions.Company.IsCheckingRequired(counterparty);
            // Если статус «Стоп-лист» или «Требуется проверка».
            needCounterpartyChecking = isStopList || isCheckingRequired || isNotApproved;
            
            if (needCounterpartyChecking)
            {
              // Если статус «Стоп-лист».
              if (isStopList)
                dialogMessage.Add(PartiesControl.CounterpartyStatuses.Resources.SendApprovalDialogMessageStoplistStatus);
              // Если контрагент не одобрен.
              else if (isNotApproved)
                dialogMessage.Add(PartiesControl.CounterpartyStatuses.Resources.SendApprovalDialogMessageNotAllowedStatus);
              
              // Если статус контрагента «Требуется проверка», то создаем заявку на проверку контрагента.
              if (Solution.PublicFunctions.Company.NeedCreateRevisionRequest(counterparty, false))
              {
                createRevisionRequest = true;
                needComment = false;
                dialogMessage.Add(PartiesControl.CounterpartyStatuses.Resources.SendApprovalDialogMessageCheckingRequiredStatus);
              }
            }
          }
        }
        
        // Проверить бессрочный договор.
        if (contract != null && supAgreement == null && contract.IsTermless == true)
        {
          dialogMessage.Add(DirRX.Solution.ApprovalTasks.Resources.TermlessContract);
          needComment = true;
        }
        
        // Проверить общий срок договора и ДС.
        if (contract != null)
        {
          var validFrom = contract.ValidFrom;	// Действует с
          var validTill = supAgreement != null ? supAgreement.ValidTill : contract.ValidTill;		// Действует по
          
          
          if (validFrom.HasValue && validTill.HasValue)
          {
            var durationYears = ContractsCustom.PublicFunctions.Module.GetDateDifferenceInYear(validFrom.Value, validTill.Value);
            
            // Получить общий срок из константы.
            var generalPeriod = ContractsCustom.PublicFunctions.Module.Remote.
              GetContractConstant(ContractsCustom.PublicConstants.Module.GeneralPeriodContractAndAdditAgreementGuid.ToString()).Period;
            if (durationYears > generalPeriod)
            {
              if (supAgreement != null)
                dialogMessage.Add(DirRX.Solution.ApprovalTasks.Resources.PeriodSupAgreementMoreFormat(generalPeriod.ToString()));
              else
                dialogMessage.Add(DirRX.Solution.ApprovalTasks.Resources.PeriodContractMoreFormat(generalPeriod.ToString()));
              needComment = true;
            }
          }
          else if (supAgreement != null && supAgreement.DocumentValidity.HasValue)
          {
            // Получить общий срок из константы.
            var generalPeriod = ContractsCustom.PublicFunctions.Module.Remote.
              GetContractConstant(ContractsCustom.PublicConstants.Module.GeneralPeriodContractAndAdditAgreementGuid.ToString()).Period;
            var monthCount = generalPeriod * 12;
            if (supAgreement.DocumentValidity > monthCount)
            {
              dialogMessage.Add(DirRX.Solution.ApprovalTasks.Resources.PeriodValiditySupAgreementMoreFormat(generalPeriod.ToString()));
              needComment = true;
            }
          }
          else if (supAgreement == null && contract.DocumentValidity.HasValue)
          {
            // Получить общий срок из константы.
            var generalPeriod = ContractsCustom.PublicFunctions.Module.Remote.
              GetContractConstant(ContractsCustom.PublicConstants.Module.GeneralPeriodContractAndAdditAgreementGuid.ToString()).Period;
            var monthCount = generalPeriod * 12;
            if (contract.DocumentValidity > monthCount)
            {
              dialogMessage.Add(DirRX.Solution.ApprovalTasks.Resources.PeriodValidityContractMoreFormat(generalPeriod.ToString()));
              needComment = true;
            }
          }
        }
        
        // Если договор или доп. соглашение по договору сбыта - проверить страны поставки.
        if (contract != null && contract.ContractFunctionality == DirRX.Solution.Contract.ContractFunctionality.Sale)
        {
          var destinationCountries = new List<DirRX.Solution.IContractDestinationCountries>();
          var includedInDisputedTerritoriesCountries = new List<string>();
          if (supAgreement != null)
            includedInDisputedTerritoriesCountries.AddRange(supAgreement.DestinationCountries
                                                            .Where(x => x.DestinationCountry.IsIncludedInDisputedTerritories == true)
                                                            .Select(y => y.DestinationCountry.Name)
                                                            .ToList());
          else
            includedInDisputedTerritoriesCountries.AddRange(contract.DestinationCountries
                                                            .Where(x => x.DestinationCountry.IsIncludedInDisputedTerritories == true)
                                                            .Select(y => y.DestinationCountry.Name)
                                                            .ToList());
          // Если хотя бы одна страна относится к спорным территориям, вывести диалог.
          if (includedInDisputedTerritoriesCountries.Count() > 0)
          {
            dialogMessage.Add(DirRX.Solution.ApprovalTasks.Resources.CountriesInDisputedTerritoriesFormat(string.Join(", ", includedInDisputedTerritoriesCountries)));
            needComment = true;
          }
        }
        
        // Вывести диалог.
        if (dialogMessage.Any())
        {
          var dialog = Dialogs.CreateInputDialog(Sungero.Workflow.Notices.Info.LocalizedName);
          
          // Определить итоговый текст.
          var resultDialogMessage = string.Join(Environment.NewLine, dialogMessage);
          if (dialogMessage.Count == 1)
          { // Собрать в одну строку.
            if (needCounterpartyChecking && createRevisionRequest)
              resultDialogMessage += " " + DirRX.Solution.ApprovalTasks.Resources.CounterpartyRequestAsk;
            else if (needCounterpartyChecking)
              resultDialogMessage += " " + DirRX.Solution.ApprovalTasks.Resources.CounterpartyConfirmation + " " + DirRX.Solution.ApprovalTasks.Resources.Continue;
            else
              resultDialogMessage += " " + DirRX.Solution.ApprovalTasks.Resources.ContractConfirmation + " " + DirRX.Solution.ApprovalTasks.Resources.Continue;
          }
          else
          {	// Добавить вопрос с новой строки, потому что текст большой.
            resultDialogMessage += Environment.NewLine;
            if (needCounterpartyChecking && createRevisionRequest)
            {
              resultDialogMessage += createRevisionRequestCounterparties.Count > 1 ? DirRX.Solution.ApprovalTasks.Resources.CounterpartiesRequest : DirRX.Solution.ApprovalTasks.Resources.CounterpartyRequest;
              resultDialogMessage += " " + DirRX.Solution.ApprovalTasks.Resources.ContractConfirmation;
            }
            else if (needCounterpartyChecking)
              resultDialogMessage += DirRX.Solution.ApprovalTasks.Resources.ContractCounterpartyConfirmation;
            else
              resultDialogMessage += DirRX.Solution.ApprovalTasks.Resources.ContractConfirmation;
            resultDialogMessage += " " + DirRX.Solution.ApprovalTasks.Resources.Continue;
          }
          
          dialog.Text = string.Join(Environment.NewLine, resultDialogMessage);
          
          var comment = dialog.AddMultilineString(DirRX.Solution.ApprovalTasks.Resources.Comment, true);
          comment.IsVisible = needComment;
          comment.IsRequired = needComment;
          
          var sendButton = dialog.Buttons.AddCustom(Sungero.Workflow.Tasks.Info.Actions.Start.LocalizedName);
          dialog.Buttons.AddCancel();
          
          var result = dialog.Show();
          if (result != sendButton)
            return;
          
          if (needComment)
            _obj.CommentOfInitiatorContract = comment.Value;
          
          // Создадим заявки на проверку контрагента.
          if (createRevisionRequest)
          {
            // Если надо создать заявки для нескольких контрагентов.
            if (createRevisionRequestCounterparties.Count > 0)
            {
              var requests = PartiesControl.PublicFunctions.RevisionRequest.Remote.CreateRevisionRequests(contractualDocument, createRevisionRequestCounterparties);
              foreach (var request in requests)
                Functions.ApprovalTask.Remote.StartRevisionSimpleTask(request);
            }
            else
            {
              var request = PartiesControl.PublicFunctions.RevisionRequest.Remote.CreateRevisionRequest(contractualDocument);
              if (request != null)
                Functions.ApprovalTask.Remote.StartRevisionSimpleTask(request);
            }
          }
        }
      }
      
      #endregion
      
      _obj.Start();
      e.CloseFormAfterAction = true;
    }

    public override bool CanStart(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanStart(e);
    }

    public virtual void AddSubscribers(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var subscribers = DirRX.Solution.PublicFunctions.Module.GetSelectedEmployees(_obj.Subscribers.Select(s => s.Subscriber).ToList());
      if (subscribers.Any())
      {
        var isSend = DirRX.ActionItems.PublicFunctions.SendNoticeQueueItem.Remote.CreateSendNoticeQueueItem(subscribers.ToList(), _obj, null);
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
      return _obj.AccessRights.CanUpdate() &&
        (_obj.Status == ActionItemExecutionTask.Status.InProcess ||
         _obj.Status == ActionItemExecutionTask.Status.UnderReview);
    }

  }

}