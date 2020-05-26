using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalTask;

namespace DirRX.Solution.Server
{
  partial class ApprovalTaskFunctions
  {
    /// <summary>
    /// Отправка уведомления подписчикам.
    /// </summary>
    /// <param name="subject">Тема.</param>
    /// <param name="subscribers">Подписчики</param>
    /// <param name="attachment">Вложение</param>
    public void SendNoticeToSubscribers()
    {
      var notice = Sungero.Workflow.SimpleTasks.CreateWithNotices(DirRX.ActionItems.SendNoticeQueueItems.Resources.NewSubscriberFormat(_obj.Subject),
                                                                  _obj.Subscribers.Select(x => x.Subscriber).ToArray());
      notice.Attachments.Add(_obj);
      notice.Start();
    }
    
    /// <summary>
    /// Выдача прав подписчикам.
    /// </summary>
    /// <param name="subject">Тема.</param>
    /// <param name="subscribers">Подписчики</param>
    /// <param name="attachment">Вложение</param>
    public void GrantReadSubscribersRights()
    {
      foreach (var subscriber in  _obj.Subscribers.Select(x => x.Subscriber).ToList())
      {
        _obj.AccessRights.Grant(subscriber, new [] {DefaultAccessRightsTypes.Read});
      }
    }
    
    /// <summary>
    /// Получить ожидаемый срок по задаче.
    /// </summary>
    /// <returns>Срок по задаче.</returns>
    [Public, Remote(IsPure = true)]
    public new DateTime? GetExpectedDate()
    {
      return base.GetExpectedDate();
    }
    
    /// <summary>
    /// Проверить наличие согласуемого документа в задаче и наличие хоть каких то прав на него.
    /// </summary>
    /// <returns>True, если с документом можно работать.</returns>
    [Remote(IsPure = true)]
    public override bool HasDocumentAndCanRead()
    {
      return base.HasDocumentAndCanRead();
    }
    
    /// <summary>
    /// Сохранить текущие права на вложения задачи.
    /// </summary>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>Словарь с документами и списком прав на них.</returns>
    public static System.Collections.Generic.Dictionary<Sungero.Docflow.IOfficialDocument, List<Sungero.Core.GrantedAccessRights>> GetCurrentAttachmentsRights(IApprovalTask task)
    {
      var rightsDictionary = new Dictionary<Sungero.Docflow.IOfficialDocument, List<GrantedAccessRights>>();
      
      // На основной документ на изменение
      var approvalDocument = task.DocumentGroup.OfficialDocuments.First();
      rightsDictionary.Add(approvalDocument, approvalDocument.AccessRights.Current.ToList());
      
      // На приложения на изменение, но не выше, чем у инициатора.
      foreach (var document in task.AddendaGroup.OfficialDocuments)
      {
        rightsDictionary.Add(document, document.AccessRights.Current.ToList());
      }
      
      return rightsDictionary;
    }
    
    /// <summary>
    /// Отобрать выданные ранее права.
    /// </summary>
    /// <param name="rightsDictionary">Словарь с документами и списком прав на них.</param>
    public static void RestoreAttachmentsRights(System.Collections.Generic.Dictionary<Sungero.Docflow.IOfficialDocument, List<Sungero.Core.GrantedAccessRights>> rightsDictionary)
    {
      // На приложения на изменение, но не выше, чем у инициатора.
      foreach (var document in rightsDictionary.Keys)
      {
        var accessRightsList = document.AccessRights.Current;
        foreach (var accessRights in accessRightsList)
          if (!rightsDictionary[document].Any(a => Sungero.CoreEntities.Recipients.Equals(a.Recipient, accessRights.Recipient)))
            document.AccessRights.Revoke(accessRights.Recipient, accessRights.AccessRightsType);
        document.AccessRights.Save();
      }
    }
    
    [Public, Remote(IsPure = true)]
    public static DirRX.PartiesControl.IRevisionRequest GetRevisionRequestOfTaskInProcess(DirRX.Solution.ICompany counterparty)
    {
      return Solution.ApprovalTasks
        .GetAll(t => t.Status == Solution.ApprovalTask.Status.InProcess)
        .ToList()
        .Select(t => t.DocumentGroup.OfficialDocuments.FirstOrDefault())
        .OfType<DirRX.PartiesControl.IRevisionRequest>()
        .FirstOrDefault(r => Solution.Companies.Equals(r.Counterparty, counterparty));
    }

    /// <summary>
    /// Отправить простую задачу для инициации проверки контрагента.
    /// </summary>
    /// <param name="revisionRequest">Заявка на проверку.</param>
    [Remote]
    public static void StartRevisionSimpleTask(DirRX.PartiesControl.IRevisionRequest revisionRequest)
    {
      var task = Sungero.Workflow.SimpleTasks.Create(DirRX.Solution.ApprovalTasks.Resources.RevisionSimpleTaskSubject, Calendar.Now.AddWorkingDays(2), new IRecipient[] { Users.Current});
      task.Attachments.Add(revisionRequest);
      task.NeedsReview = false;
      task.Start();
    }
    
    #region Скопировано из стандартной.
    
    /// <summary>
    /// Помечает задачу для отправки на доработку, если не удалось вычислить исполнителя этапа.
    /// </summary>
    /// <param name="stage">Этап, исполнителя которого не удалось вычислить.</param>
    public void FillReworkReasonWhenAssigneeNotFound(IApprovalStage stage)
    {
      _obj.IsStageAssigneeNotFound = true;
      var hyperlink = Hyperlinks.Get(stage);
      _obj.ReworkReason = ApprovalTasks.Resources.ReworkReasonWhenAssigneeNotFoundFormat(hyperlink);
    }
    
    /// <summary>
    /// Выдать права на вложения, не выше прав инициатора задачи.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="performers">Исполнители.</param>
    public static void GrantRightForAttachmentsToPerformers(IApprovalTask task, List<IRecipient> performers)
    {
      foreach (var performer in performers)
      {
        // На основной документ на изменение
        var approvalDocument = task.DocumentGroup.OfficialDocuments.First();
        if (!approvalDocument.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, performer))
          approvalDocument.AccessRights.Grant(performer, DefaultAccessRightsTypes.Change);
        
        // На приложения на изменение, но не выше, чем у инициатора.
        foreach (var document in task.AddendaGroup.OfficialDocuments)
        {
          if (document.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, performer))
            continue;

          var rightType = document.AccessRights.CanUpdate(task.Author) ? DefaultAccessRightsTypes.Change : DefaultAccessRightsTypes.Read;
          document.AccessRights.Grant(performer, rightType);
        }
      }
    }
    
    /// <summary>
    /// Получить ключ для подписи.
    /// </summary>
    /// <param name="signature">Подпись.</param>
    /// <param name="versionNumber">Номер версии.</param>
    /// <returns>Ключ для подписи.</returns>
    [Public]
    public static string GetSignatureKey(Sungero.Domain.Shared.ISignature signature, int versionNumber)
    {
      // Если подпись не "несогласующая" она должна схлапываться вне версий.
      if (signature.SignatureType != SignatureType.NotEndorsing)
        versionNumber = 0;
      
      if (signature.Signatory != null)
      {
        if (signature.SubstitutedUser != null && !signature.SubstitutedUser.Equals(signature.Signatory))
          return string.Format("{0} - {1}:{2}:{3}", signature.Signatory.Id, signature.SubstitutedUser.Id, signature.SignatureType == SignatureType.Approval, versionNumber);
        else
          return string.Format("{0}:{1}:{2}", signature.Signatory.Id, signature.SignatureType == SignatureType.Approval, versionNumber);
      }
      else
        return string.Format("{0}:{1}:{2}", signature.SignatoryFullName, signature.SignatureType == SignatureType.Approval, versionNumber);
    }
    
    /// <summary>
    /// Получить локализованное имя результата согласования по подписи.
    /// </summary>
    /// <param name="signature">Подпись.</param>
    /// <param name="emptyIfNotValid">Вернуть пустую строку, если подпись не валидна.</param>
    /// <returns>Локализованный результат подписания.</returns>
    [Public]
    public static string GetEndorsingResultFromSignature(Sungero.Domain.Shared.ISignature signature, bool emptyIfNotValid)
    {
      var result = string.Empty;
      
      if (emptyIfNotValid && !signature.IsValid)
        return result;
      
      switch (signature.SignatureType)
      {
        case SignatureType.Approval:
          result = ApprovalTasks.Resources.ApprovalFormApproved;
          break;
        case SignatureType.Endorsing:
          result = ApprovalTasks.Resources.ApprovalFormEndorsed;
          break;
        case SignatureType.NotEndorsing:
          result = ApprovalTasks.Resources.ApprovalFormNotEndorsed;
          break;
      }
      
      return result;
    }
    
    /// <summary>
    /// Добавить перенос в конец строки, если она не пуста.
    /// </summary>
    /// <param name="row">Строка.</param>
    /// <returns>Результирующая строка.</returns>
    [Public]
    public static string AddEndOfLine(string row)
    {
      return string.IsNullOrWhiteSpace(row) ? string.Empty : row + Environment.NewLine;
    }
    
    /// <summary>
    /// Выдать права на вложения автору задачи.
    /// </summary>
    [Remote]
    public void GrantRightsToAuthorSolution()
    {
      base.GrantRightsToAuthor();
    }
    
    /// <summary>
    /// Получить признак наличия согласования автором задачи или исполнителем задания доработки.
    /// </summary>
    /// <param name="assignee">Автор задачи или исполнитель задания доработки.</param>
    /// <param name="approvers">Список согласующих, в который может попасть инициатор.</param>
    /// <returns>Признак согласования инициатором и признак необходимости усиленной подписи.</returns>
    [Remote(IsPure = true)]
    public DirRX.ContractsCustom.Structures.Module.IApprovalStatus AuthorMustApproveDocumentSolution(IUser assignee, List<IRecipient> approvers)
    {
      // HACK: Нельзя использовать структуру из базового модуля как возвращаемый тип функции, поэтому структура продублирована в нашем решении.
      var approvalStatusBase = base.AuthorMustApproveDocument(assignee, approvers);
      var approvalStatus = new DirRX.ContractsCustom.Structures.Module.ApprovalStatus();
      approvalStatus.HasApprovalStage = approvalStatusBase.HasApprovalStage;
      approvalStatus.NeedStrongSign = approvalStatusBase.NeedStrongSign;
      return approvalStatus;
    }
    
    #endregion
    
    /// <summary>
    /// Обновить статус контрагента при согласовании
    /// </summary>
    /// <param name="memoForPayments"></param>
    public static void ResetCounterpartyStatus(DirRX.ContractsCustom.IMemoForPayment memoForPayments)
    {
      if (memoForPayments != null && memoForPayments.Counterparty.CounterpartyStatus.ForOneDeal.GetValueOrDefault())
      {
        memoForPayments.Counterparty.CounterpartyStatus =
          DirRX.PartiesControl.CounterpartyStatuses.GetAll(s => s.Sid == DirRX.PartiesControl.PublicConstants.CounterpartyStatus.DefaultStatus.CheckingRequiredSid).FirstOrDefault();
        memoForPayments.Counterparty.CheckingResult = null;
        memoForPayments.Counterparty.CheckingDate = null;
        memoForPayments.Counterparty.CheckingValidDate = null;
        
        memoForPayments.Counterparty.Save();
      }
    }
    
    /// <summary>
    /// Получить схлапываемый этап указанного типа.
    /// </summary>
    /// <param name="task">Задача согласования.</param>
    /// <param name="currentStageNumber">Номер текущего этапа.</param>
    /// <param name="specificStageType">Целевой тип.</param>
    /// <returns></returns>
    [Remote(IsPure = true)]
    public static IApprovalStage GetCollapsedStage(IApprovalTask task, int? currentStageNumber, Enumeration specificStageType)
    {
      var currentStage = task.ApprovalRule.Stages.Where(s => s.Number == currentStageNumber).FirstOrDefault();
      if (currentStage != null)
      {
        var stage = Sungero.Docflow.Structures.Module.DefinedApprovalStageLite.Create(currentStage.Stage, currentStage.Number, currentStage.StageType);
        var collapsedStages = GetCollapsedStages(task, stage);
        var collapsedStage = collapsedStages.Where(s => s.StageType == specificStageType).FirstOrDefault();
        if (collapsedStage != null)
          return ApprovalStages.As(collapsedStage.Stage);
      }
      return null;
    }

  }
}