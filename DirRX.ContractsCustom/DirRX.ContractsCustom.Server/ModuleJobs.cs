using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using System.Text;

namespace DirRX.ContractsCustom.Server
{
  public class ModuleJobs
  {

    /// <summary>
    /// Агент обновления статусов договоров из пакетов отправки.
    /// </summary>
    public virtual void SetContractStatusInPackagesJob()
    {
      var queueItems = ContractQueueItems.GetAll()
        .Where(q => q.DocumentId != null && q.ContractStatusSid != null)
        .OrderBy(q => q.DocumentId)
        .ThenBy(q => q.LastUpdate);
      
      foreach (IContractQueueItem item in queueItems)
      {
        var statusAction = item.ContractStatusAction;
        var statusType = item.ContractStatusType;
        var statusSid = item.ContractStatusSid;
        
        try
        {
          var contractDoc = Sungero.Contracts.ContractualDocuments.GetAll(c => c.Id == item.DocumentId).FirstOrDefault();
          if (contractDoc != null)
          {
            if (statusAction == PublicConstants.Module.StatusAction.RemoveAction)
              ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(contractDoc, Guid.Parse(statusSid), statusType);
            
            if (statusAction == PublicConstants.Module.StatusAction.AddAction)
              ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(contractDoc, Guid.Parse(statusSid), statusType, false);
            
            contractDoc.Save();
          }
          
          
          ContractQueueItems.Delete(item);
        }
        catch (Exception ex)
        {
          Transactions.Execute(
            () =>
            {
              Sungero.ExchangeCore.PublicFunctions.QueueItemBase.QueueItemOnError(item, ex.Message);
            });
          Logger.DebugFormat("SetContractStatusInPackagesJob: {0} Id = '{1}'.", ex.Message, item.DocumentId);
        }
      }
    }
    
    /// <summary>
    /// ФП: Поиск заданий на регистрацию и выполнение таких заданий если докумнет уже зарегистрирован.
    /// </summary>
    public virtual void JobSearchAssignRegistration()
    {
      var listAssignmentsOnRegistration = DirRX.Solution.ApprovalRegistrationAssignments.GetAll(s => s.Status == DirRX.Solution.ApprovalRegistrationAssignment.Status.InProcess);
      
      foreach (var assignment in listAssignmentsOnRegistration)
      {
        var document = assignment.DocumentGroup.OfficialDocuments.SingleOrDefault();
        
        if ((DirRX.Solution.Contracts.Is(document) || DirRX.Solution.SupAgreements.Is(document)) && document.RegistrationState == Sungero.Contracts.ContractBase.RegistrationState.Registered)
        {
          var lockInfo = Locks.GetLockInfo(assignment);
          if (!lockInfo.IsLocked)
            assignment.Complete(DirRX.Solution.ApprovalRegistrationAssignment.Result.Execute);
          else
            Logger.Debug(string.Format("Задание {0} заблокировано пользователем {1}.", assignment.Id, lockInfo.OwnerName));
        }
      }
    }

    /// <summary>
    /// ФП: Поиск договоров с истекшим сроком возврата и отправка задачи исполнителю договора.
    /// </summary>
    public virtual void ControlReturnContractJob()
    {
      var contracualDocuments = new List<Sungero.Contracts.IContractualDocument>();
      
      // Получить договора на согласовании у контрагента.
      contracualDocuments.AddRange(DirRX.Solution.Contracts.GetAll().Where(t => t.ExternalApprovalState == DirRX.Solution.Contract.ExternalApprovalState.OnApproval));
      contracualDocuments.AddRange(DirRX.Solution.SupAgreements.GetAll().Where(t => t.ExternalApprovalState == DirRX.Solution.SupAgreement.ExternalApprovalState.OnApproval));
      
      foreach (var contract in contracualDocuments)
      {
        var searchTaskControl = DirRX.ContractsCustom.ControlReturnTasks.GetAll(t => t.AttachmentDetails.Any(g => g.AttachmentId == contract.Id)).Where(s => s.Status == Sungero.Workflow.Task.Status.InProcess);
        if (searchTaskControl.Any())
          continue;
        
        // Получаем элементы вкладки выдача где действие == Согласование с контрагентом и забираем элемент где дата возврата пуста.
        var endorsementTrackItem = contract.Tracking.Where(x => x.Action.Equals(Sungero.Contracts.ContractBaseTracking.Action.Endorsement)).Where(p => p.ReturnDate == null).SingleOrDefault();
        if (endorsementTrackItem == null)
          continue;
        
        // По УТЗ решили, что задачи формируются если срок возврата истек. если срок возврата более 30 дней, то задание формируется через 30 дней указанных в справочнике констант
        var deadlineConstant = ContractConstants.GetAll(c => c.Sid == Constants.Module.SendToPerformerConstantGuid.ToString()).FirstOrDefault();
        var dateInConst = Calendar.FromUserTime(endorsementTrackItem.DeliveryDate);
        
        if (deadlineConstant.Unit == DirRX.ContractsCustom.ContractConstant.Unit.Day)
          dateInConst.Value.AddDays(deadlineConstant.Period.Value);
        else if (deadlineConstant.Unit == DirRX.ContractsCustom.ContractConstant.Unit.Month)
          dateInConst.Value.AddMonths(deadlineConstant.Period.Value);
        else if (deadlineConstant.Unit == DirRX.ContractsCustom.ContractConstant.Unit.Year)
          dateInConst.Value.AddYears(deadlineConstant.Period.Value);
        else
          dateInConst = endorsementTrackItem.ReturnDeadline;

        // Поиск заданий выполненных с результатом Продлить срок.
        var assignmentsInControl = DirRX.ContractsCustom.ControlReturnAssignments.GetAll(x => x.Result.Equals(DirRX.ContractsCustom.ControlReturnAssignment.Result.Complete) &&
                                                                                         x.AttachmentDetails.Any(g => g.AttachmentId == contract.Id));
        
        // Если срок возврата истек или срок константы истек и ранее небыло заданий на продление срока.
        if (endorsementTrackItem.ReturnDeadline < Calendar.Today || dateInConst < Calendar.Today && !assignmentsInControl.Any())
        {
          // Получить исполнителя из договора или того кто указан на вкладке выдача.
          var performTask = contract.ResponsibleEmployee != null ? contract.ResponsibleEmployee : endorsementTrackItem.DeliveredTo;
          var valueConstMaxdeadline = ContractConstants.GetAll(c => c.Sid == Constants.Module.SendToPerformerConstantGuid.ToString()).FirstOrDefault();
          
          // Задача
          var task = DirRX.ContractsCustom.ControlReturnTasks.Create();
          task.AttachmentContractGroup.ContractualDocuments.Add(contract);
          task.PerformerTask = DirRX.Solution.Employees.As(performTask);
          task.Subject = DirRX.ContractsCustom.Resources.SignedInstanceNotReceived;
          
          if (task.Subject.Length > task.Info.Properties.Subject.Length)
            task.Subject = task.Subject.Substring(0, task.Info.Properties.Subject.Length);
          
          task.ActiveText = DirRX.ContractsCustom.Resources.ControlReturnTaskActiveText;
          task.Save();
          Logger.Debug(string.Format(DirRX.ContractsCustom.Resources.CreatedTask, task.Id, contract.Id));
          task.Start();
        }
      }
    }
    
    /// <summary>
    /// Отправить ответственным за интеграцию с учетными системами уведомление о результатах интеграции.
    /// </summary>
    /// <param name="subject">Тема уведомления.</param>
    /// <param name="activeText">Текст уведомления.</param>
    /// <param name="document">Договорной документ.</param>
    [Public]
    public static void SendImportResultsNotice(string subject, string activeText, Sungero.Contracts.IContractualDocument document)
    {
      var role = Roles.GetAll(x => x.Sid == IntegrationLLK.PublicConstants.Module.SynchronizationResponsibleRoleGuid).FirstOrDefault();
      if (role != null)
      {
        var task = Sungero.Workflow.SimpleTasks.Create();
        task.Subject = subject;
        var step = task.RouteSteps.AddNew();
        step.AssignmentType = Sungero.Workflow.SimpleTask.AssignmentType.Notice;
        step.Performer = role;
        task.ActiveText = activeText;
        
        if (document != null)
          task.Attachments.Add(document);

        task.Save();
        task.Start();
      }
    }
    
    #region Выполнение условия активации договорного документа.
    /// <summary>
    /// Выполнение условия активации договорного документа.
    /// </summary>
    public virtual void ConditionsActivationComplete()
    {
      Logger.DebugFormat("Старт фонового процесса: \"Выполнение условия активации договорных документов\".");
      var previousRunDate = DirRX.LocalActs.PublicFunctions.Module.GetLastNotificationDate(Constants.Module.LastConditionsActivationCompleteJobParamName);
      var currentDate = Calendar.Now;
      var contracts = DirRX.Solution.Contracts.GetAll(c => c.RegistrationState == DirRX.Solution.Contract.RegistrationState.Registered &&
                                                      c.RegistrationDate.Between(previousRunDate.Date, currentDate) &&
                                                      c.StartConditionsExists == true &&
                                                      c.AreConditionsCompleted == false).ToList();
      var supAgreements = DirRX.Solution.SupAgreements.GetAll(s => s.RegistrationState == DirRX.Solution.Contract.RegistrationState.Registered &&
                                                              s.RegistrationDate.Between(previousRunDate.Date, currentDate) &&
                                                              s.StartConditionsExists == true &&
                                                              s.AreConditionsCompleted == false).ToList();
      
      Logger.Debug("ConditionsActivationComplete: Отправка задач по выполнению условий активации договорных документов");
      SendСonfirmationActivationConditionTasks(contracts, supAgreements);
      
      DirRX.LocalActs.PublicFunctions.Module.UpdateLastNotificationDate(Constants.Module.LastConditionsActivationCompleteJobParamName, currentDate);
    }
    
    /// <summary>
    /// Отправка задач на подтверждение наступления условий активации договорного документа.
    /// </summary>
    /// <param name="contracts">Список договоров.</param>
    /// <param name="supAgreements">Список доп.соглашений.</param>
    public void SendСonfirmationActivationConditionTasks(List<DirRX.Solution.IContract> contracts, List<DirRX.Solution.ISupAgreement> supAgreements)
    {
      Logger.DebugFormat("SendСonfirmationActivationConditionTasks: Отправка {0} договоров, {1} дополнительных соглашений", contracts.Count, supAgreements.Count);
      
      var deadlineConstant = ContractConstants.GetAll(c => c.Sid == Constants.Module.ConfirmActivationConditionTaskDeadlineGuid.ToString()).FirstOrDefault();
      if (deadlineConstant == null || !deadlineConstant.Period.HasValue || deadlineConstant.Unit != DirRX.ContractsCustom.ContractConstant.Unit.Day)
        throw new Exception("Не заполнена константа \"Срок задачи на вложение документов подтверждающих наступление условий активации\", или в константе указана единица измерения не равная \"Дни\"");
      
      // Договоры.
      foreach (var contract in contracts)
      {
        var performer = contract.ResponsibleEmployee;
        if (performer == null)
        {
          Logger.ErrorFormat("SendСonfirmationActivationConditionTasks: Не найден исполнитель договора {0}, Id: {1}", contract.Name, contract.Id);
          continue;
        }
        
        var author = contract.DpoUser;
        if (author == null)
        {
          Logger.ErrorFormat("SendСonfirmationActivationConditionTasks: Не найден сотрудник ДПО в договоре {0}, Id: {1}", contract.Name, contract.Id);
          continue;
        }
        
        SendSimpleTask(author, performer, deadlineConstant.Period.Value, contract);
      }
      
      // Дополнительные соглашения.
      foreach (var supAgreement in supAgreements)
      {
        var performer = supAgreement.ResponsibleEmployee;
        
        if (performer == null)
        {
          Logger.ErrorFormat("SendСonfirmationActivationConditionTasks: Не найден исполнитель доп.соглашения {0}, Id: {1}", supAgreement.Name, supAgreement.Id);
          continue;
        }
        
        var author = supAgreement.DpoUser;
        if (performer == null)
        {
          Logger.ErrorFormat("SendСonfirmationActivationConditionTasks: Не найден сотрудник ДПО в доп.соглашении {0}, Id: {1}", supAgreement.Name, supAgreement.Id);
          continue;
        }
        
        SendSimpleTask(author, performer, deadlineConstant.Period.Value, supAgreement);
      }
    }
    
    /// <summary>
    /// Отправка простой задачи.
    /// </summary>
    /// <param name="author">Инициатор.</param>
    /// <param name="performer">Исполнитель.</param>
    /// <param name="deadline">Срок.</param>
    /// <param name="document">Документ.</param>
    public void SendSimpleTask(Solution.IEmployee author, Sungero.Company.IEmployee performer, int deadline, Sungero.Contracts.IContractualDocument document)
    {
      var task = Sungero.Workflow.SimpleTasks.Create();
      task.Subject = DirRX.ContractsCustom.Resources.ConfirmationActivationConditionsTaskSubject;
      if (Solution.Contracts.Is(document))
        task.ActiveText = DirRX.ContractsCustom.Resources.ConfirmationActivationConditionsTaskThema;
      if (Solution.SupAgreements.Is(document))
        task.ActiveText = DirRX.ContractsCustom.Resources.ConfirmationActivationConditionsTaskThemaSA;
      task.NeedsReview = true;
      task.Deadline = Calendar.Now.AddWorkingDays(deadline);
      task.Author = author;
      
      var step = task.RouteSteps.AddNew();
      step.AssignmentType = Sungero.Workflow.SimpleTask.AssignmentType.Assignment;
      step.Performer = performer;
      task.Attachments.Add(document);
      
      task.Save();
      task.Start();
    }
    #endregion

    /// <summary>
    /// Перевод договора/ДС в состояние Исполнен.
    /// </summary>
    public virtual void SetContractExecutedJob()
    {
      Logger.DebugFormat("Старт фонового процесса: \"Перевод договора/ДС в состояние Исполнен\".");
      var previousRunDate = DirRX.LocalActs.PublicFunctions.Module.GetLastNotificationDate(Constants.Module.LastSetContractExecutedDateTimeDocflowParamName);
      var startTime = Calendar.Now;
      var contractualDocs = Sungero.Contracts.ContractualDocuments.GetAll(y => (Solution.Contracts.Is(y) || Solution.SupAgreements.Is(y)) &&
                                                                          y.ValidTill != null && y.LifeCycleState == DirRX.Solution.Contract.LifeCycleState.Active).ToList()
        .Where(x => x.ValidTill.Between(previousRunDate.Date, Calendar.Today));
      Logger.DebugFormat("contractualDocs.Count = {0}", contractualDocs.Count().ToString());
      foreach (var doc in contractualDocs)
      {
        try
        {
          Logger.DebugFormat("docId:{0}", doc.Id.ToString());
          var responsible = doc.ResponsibleEmployee;
          var contract = Solution.Contracts.As(doc);
          var supAgreement = Solution.SupAgreements.As(doc);
          if (contract != null && contract.CoExecutor != null)
            responsible = contract.CoExecutor;
          if (supAgreement != null && supAgreement.CoExecutor != null)
            responsible = supAgreement.CoExecutor;
          Logger.DebugFormat("responsible: {0}", responsible.Name);
          
          //Отправим задачу.
          var task = DirRX.ContractsCustom.ConfirmContractExecutedTasks.Create();
          task.Employee = Solution.Employees.As(responsible);
          task.AttachmentContractGroup.ContractualDocuments.Add(doc);
          task.Save();
          task.Start();
          Logger.DebugFormat("taskId:{0}", task.Id.ToString());
        }
        catch (Exception ex)
        {
          Logger.DebugFormat("Во время обработки документа с ИД {0} - произошла ошибка: {1}", doc.Id, ex.Message);
        }
      }
      
      DirRX.LocalActs.PublicFunctions.Module.UpdateLastNotificationDate(Constants.Module.LastSetContractExecutedDateTimeDocflowParamName, startTime);
    }

    #region Контроль наличия оригиналов для активированных договоров.
    
    /// <summary>
    /// Контроль наличия оригиналов для активированных договоров.
    /// </summary>
    public virtual void ContractOriginalsControlJob()
    {
      Logger.Debug("ContractOriginalsControlJob: Запуск процесса контроля наличия оригиналов для активированных договоров");
      
      string paramKey = Constants.Module.LastContractOriginalsControlJobParamName;
      var successDate = LocalActs.PublicFunctions.Module.GetLastNotificationDate(paramKey);
      Logger.DebugFormat("ContractOriginalsControlJob: Предыдущая дата успешного запуска {0}", successDate.ToString());
      var today = Calendar.Today;
      if (successDate > today)
        successDate = today;
      
      if (successDate < today)
        successDate = successDate.AddDays(1).Date;
      
      var activatedContracts = DirRX.Solution.Contracts.GetAll(c => c.ActivateDate.HasValue &&
                                                               c.LifeCycleState == DirRX.Solution.Contract.LifeCycleState.Active &&
                                                               (!c.ContractorOriginalSigning.HasValue ||
                                                                c.ContractorOriginalSigning != DirRX.Solution.Contract.ContractorOriginalSigning.Signed));
      
      var activatedSupAgreements = DirRX.Solution.SupAgreements.GetAll(a => a.ActivateDate.HasValue &&
                                                                       a.LifeCycleState == DirRX.Solution.SupAgreement.LifeCycleState.Active &&
                                                                       (!a.ContractorOriginalSigning.HasValue ||
                                                                        a.ContractorOriginalSigning != DirRX.Solution.SupAgreement.ContractorOriginalSigning.Signed));

      #region Отправка на контроль исполнителям по документам.
      Logger.Debug("ContractOriginalsControlJob: Отправка на контроль исполнителям по документам");
      
      var sendToPerformerConstant = ContractConstants.GetAll(c => c.Sid == Constants.Module.SendToPerformerConstantGuid.ToString()).FirstOrDefault();
      if (sendToPerformerConstant != null && sendToPerformerConstant.Period.HasValue && sendToPerformerConstant.Unit.HasValue)
      {
        var previousDateStart = GetPreviousDateByConstant(sendToPerformerConstant, successDate);
        var previousDateEnd = GetPreviousDateByConstant(sendToPerformerConstant, Calendar.Today);
        Logger.DebugFormat("ContractOriginalsControlJob: Дата активации, начиная с которой происходит поиск документов {0}", previousDateStart.ToString());
        Logger.DebugFormat("ContractOriginalsControlJob: Дата активации, по которую происходит поиск документов {0}", previousDateEnd.ToString());
        
        if (previousDateStart.HasValue && previousDateEnd.HasValue)
        {
          var sendToPerformerContracts = activatedContracts.Where(c => c.ActivateDate.Value.Date >= previousDateStart.Value &&
                                                                  c.ActivateDate.Value.Date <= previousDateEnd.Value).ToList();

          var sendToPerformerSupAgreements = activatedSupAgreements.Where(c => c.ActivateDate.Value.Date >= previousDateStart.Value &&
                                                                          c.ActivateDate.Value.Date <= previousDateEnd.Value).ToList();

          if (sendToPerformerContracts.Count != 0 || sendToPerformerSupAgreements.Count != 0)
            SendOriginalsControlTasks(sendToPerformerConstant, sendToPerformerContracts, sendToPerformerSupAgreements);
        }
      }
      #endregion
      
      #region Отправка на контроль кураторам по документам.
      Logger.Debug("ContractOriginalsControlJob: Отправка на контроль кураторам по документам");
      
      var sendToSupervisorConstant = ContractConstants.GetAll(c => c.Sid == Constants.Module.SendToSupervisorConstantGuid.ToString()).FirstOrDefault();
      if (sendToSupervisorConstant != null && sendToSupervisorConstant.Period.HasValue && sendToSupervisorConstant.Unit.HasValue)
      {
        var previousDateStart = GetPreviousDateByConstant(sendToSupervisorConstant, successDate);
        var previousDateEnd = GetPreviousDateByConstant(sendToSupervisorConstant, Calendar.Today);
        Logger.DebugFormat("ContractOriginalsControlJob: Дата активации, начиная с которой происходит поиск документов {0}", previousDateStart.ToString());
        Logger.DebugFormat("ContractOriginalsControlJob: Дата активации, по которую происходит поиск документов {0}", previousDateEnd.ToString());
        
        if (previousDateStart.HasValue && previousDateEnd.HasValue)
        {
          var sendToSupervisorContracts = activatedContracts.Where(c => c.ActivateDate.Value.Date >= previousDateStart.Value &&
                                                                   c.ActivateDate.Value.Date <= previousDateEnd.Value).ToList();
          
          var sendToSupervisorSupAgreements = activatedSupAgreements.Where(c => c.ActivateDate.Value.Date >= previousDateStart.Value &&
                                                                           c.ActivateDate.Value.Date <= previousDateEnd.Value).ToList();

          if (sendToSupervisorContracts.Count != 0 || sendToSupervisorSupAgreements.Count != 0)
            SendOriginalsControlTasks(sendToSupervisorConstant, sendToSupervisorContracts, sendToSupervisorSupAgreements);
        }
      }
      #endregion
      
      #region Отправка на контроль руководителям в прямом подчинении ГД по документам.
      Logger.Debug("ContractOriginalsControlJob: Отправка на контроль руководителям в прямом подчинении ГД по документам");
      
      var sendToManagerConstant = ContractConstants.GetAll(c => c.Sid == Constants.Module.SendToFirstManagerConstantGuid.ToString()).FirstOrDefault();
      if (sendToManagerConstant != null && sendToManagerConstant.Period.HasValue && sendToManagerConstant.Unit.HasValue)
      {
        var previousDateStart = GetPreviousDateByConstant(sendToManagerConstant, successDate);
        var previousDateEnd = GetPreviousDateByConstant(sendToManagerConstant, Calendar.Today);
        Logger.DebugFormat("ContractOriginalsControlJob: Дата активации, начиная с которой происходит поиск документов {0}", previousDateStart.ToString());
        Logger.DebugFormat("ContractOriginalsControlJob: Дата активации, по которую происходит поиск документов {0}", previousDateEnd.ToString());
        
        if (previousDateStart.HasValue && previousDateEnd.HasValue)
        {
          var sendToManagerContracts = activatedContracts.Where(c => c.ActivateDate.Value.Date >= previousDateStart.Value &&
                                                                c.ActivateDate.Value.Date <= previousDateEnd.Value).ToList();
          
          var sendToManagerSupAgreements = activatedSupAgreements.Where(c => c.ActivateDate.Value.Date >= previousDateStart.Value &&
                                                                        c.ActivateDate.Value.Date <= previousDateEnd.Value).ToList();

          if (sendToManagerContracts.Count != 0 || sendToManagerSupAgreements.Count != 0)
            SendOriginalsControlTasks(sendToManagerConstant, sendToManagerContracts, sendToManagerSupAgreements);
        }
      }
      #endregion
      
      LocalActs.PublicFunctions.Module.UpdateLastNotificationDate(paramKey, today);
    }
    
    /// <summary>
    /// Отправка задачи на контроль наличия оригиналов для активированных договоров.
    /// </summary>
    /// <param name="constant">Константа по значениям которой происходит отправка.</param>
    /// <param name="contracts">Договоры.</param>
    /// <param name="supAgreements">Дополнительные соглашения.</param>
    public void SendOriginalsControlTasks(IContractConstant constant, List<DirRX.Solution.IContract> contracts, List<DirRX.Solution.ISupAgreement> supAgreements)
    {
      Logger.DebugFormat("SendOriginalsControlTasks: Отправка {0} договоров, {1} дополнительных соглашений", contracts.Count, supAgreements.Count);
      
      var deadlineConstant = ContractConstants.GetAll(c => c.Sid == Constants.Module.OriginalsControlTaskDeadlineConstantGuid.ToString()).FirstOrDefault();
      if (deadlineConstant == null || deadlineConstant.Unit != DirRX.ContractsCustom.ContractConstant.Unit.Day)
        Logger.Error("SendOriginalsControlTasks: Не запонена константа \"Срок задачи обеспечения возврата оригиналов\", или в константе указана единица измерения не равная \"Дни\"");
      
      string periodType = string.Empty;
      if (constant.Unit == DirRX.ContractsCustom.ContractConstant.Unit.Day)
        periodType = "дней";
      if (constant.Unit == DirRX.ContractsCustom.ContractConstant.Unit.Month)
        periodType = "месяцев";
      if (constant.Unit == DirRX.ContractsCustom.ContractConstant.Unit.Year)
        periodType = "лет";

      #region Договоры.
      foreach (var contract in contracts)
      {
        Sungero.Company.IEmployee performer = null;

        if (constant.Sid == Constants.Module.SendToPerformerConstantGuid.ToString())
          performer = contract.ResponsibleEmployee;
        if (constant.Sid == Constants.Module.SendToSupervisorConstantGuid.ToString())
          performer = contract.Supervisor;
        if (constant.Sid == Constants.Module.SendToFirstManagerConstantGuid.ToString())
          performer = ActionItems.PublicFunctions.ActionItemsRole.Remote.GetInitCEOManager(DirRX.Solution.Employees.As(contract.ResponsibleEmployee));
        
        if (performer == null)
        {
          Logger.ErrorFormat("SendOriginalsControlTasks: Не найден исполнитель договора {0}, Id: {1}", contract.Name, contract.Id);
          continue;
        }
        
        var task = Sungero.Workflow.SimpleTasks.Create();
        task.Subject = Resources.OriginalsControlTaskSubject;
        task.ActiveText = Resources.OriginalsControlTaskTextTemplateFormat(constant.Period.ToString(), periodType);
        task.NeedsReview = false;
        task.Deadline = Calendar.Now.AddWorkingDays(deadlineConstant.Period.Value);
        
        var step = task.RouteSteps.AddNew();
        step.AssignmentType = Sungero.Workflow.SimpleTask.AssignmentType.Assignment;
        step.Performer = performer;
        
        task.Attachments.Add(contract);
        
        task.Save();
        task.Start();
      }
      #endregion
      
      #region Дополнительные соглашения.
      foreach (var supAgreement in supAgreements)
      {
        Sungero.Company.IEmployee performer = null;

        if (constant.Sid == Constants.Module.SendToPerformerConstantGuid.ToString())
          performer = supAgreement.ResponsibleEmployee;
        if (constant.Sid == Constants.Module.SendToSupervisorConstantGuid.ToString())
          performer = supAgreement.Supervisor;
        if (constant.Sid == Constants.Module.SendToFirstManagerConstantGuid.ToString())
          performer = ActionItems.PublicFunctions.ActionItemsRole.Remote.GetInitCEOManager(DirRX.Solution.Employees.As(supAgreement.ResponsibleEmployee));

        if (performer == null)
        {
          Logger.ErrorFormat("SendOriginalsControlTasks: Не найден исполнитель доп. соглашения {0}, Id: {1}", supAgreement.Name, supAgreement.Id);
          continue;
        }

        var task = Sungero.Workflow.SimpleTasks.Create();
        task.Subject = Resources.OriginalsControlTaskSubject;
        task.ActiveText = Resources.OriginalsControlTaskTextTemplateFormat(constant.Period.ToString(), periodType);
        task.NeedsReview = false;
        task.Deadline = Calendar.Now.AddWorkingDays(deadlineConstant.Period.Value);
        
        var step = task.RouteSteps.AddNew();
        step.AssignmentType = Sungero.Workflow.SimpleTask.AssignmentType.Assignment;
        step.Performer = performer;

        task.Attachments.Add(supAgreement);

        task.Save();
        task.Start();
      }
      #endregion
    }
    
    /// <summary>
    /// Получить руководителя сотрудника в прямом подчинении ГД.
    /// </summary>
    /// <param name="performer">Сотрудник.</param>
    /// <returns>Руководитель.</returns>
    public Sungero.Company.IEmployee GetFirstManager(Sungero.Company.IEmployee performer)
    {
      return ActionItems.PublicFunctions.ActionItemsRole.Remote.GetInitCEOManager(DirRX.Solution.Employees.As(performer));
    }
    
    /// <summary>
    /// Получить дату смещенную назад на значение, указанное в константе.
    /// </summary>
    /// <param name="contractConstant">Константа.</param>
    /// <returns>Вычисленная дата.</returns>
    public DateTime? GetPreviousDateByConstant(IContractConstant contractConstant, DateTime date)
    {
      DateTime? previousDate = null;
      if (contractConstant.Unit == DirRX.ContractsCustom.ContractConstant.Unit.Day)
        previousDate = date.AddDays(-1 * contractConstant.Period.Value);
      if (contractConstant.Unit == DirRX.ContractsCustom.ContractConstant.Unit.Month)
        previousDate = date.AddMonths(-1 * contractConstant.Period.Value);
      if (contractConstant.Unit == DirRX.ContractsCustom.ContractConstant.Unit.Year)
        previousDate = date.AddYears(-1 * contractConstant.Period.Value);
      
      return previousDate;
    }

    #endregion
    
    /// <summary>
    /// Перевести договорные документы в статус Действующий
    /// </summary>
    public virtual void ChangeStateContractualDocumentsOnActive()
    {
      Logger.DebugFormat("Старт фонового процесса: \"Перевод договорных докуменов в статус Действующий\".");
      
      // Договоры.
      var contracts = DirRX.Solution.Contracts.GetAll(c => c.LifeCycleState == DirRX.Solution.Contract.LifeCycleState.Draft &&
                                                      c.RegistrationState == DirRX.Solution.Contract.RegistrationState.Registered &&
                                                      c.ActivateDate.HasValue &&
                                                      Calendar.Today >= c.ActivateDate &&
                                                      (c.StartConditionsExists == false || (c.StartConditionsExists == true && c.AreConditionsCompleted == true)) &&
                                                      (
                                                        (c.ContractActivate == DirRX.Solution.Contract.ContractActivate.Copy &&
                                                         c.InternalApprovalState == DirRX.Solution.Contract.InternalApprovalState.Signed &&
                                                         c.ExternalApprovalState == DirRX.Solution.Contract.ExternalApprovalState.Signed) ||
                                                        (c.ContractActivate == DirRX.Solution.Contract.ContractActivate.Original &&
                                                         c.OriginalSigning == DirRX.Solution.Contract.OriginalSigning.Signed &&
                                                         c.ContractorOriginalSigning == DirRX.Solution.Contract.ContractorOriginalSigning.Signed)
                                                       ));

      
      var countChangeC = uint.MinValue;
      
      foreach (var contract in contracts)
      {
        try
        {
          var lockInfo = Locks.GetLockInfo(contract);
          var isLockedByOther = lockInfo != null && lockInfo.IsLocked;
          if (!isLockedByOther)
          {
            contract.LifeCycleState = DirRX.Solution.Contract.LifeCycleState.Active;
            contract.Save();
            Logger.DebugFormat("Переведен статус в состояние Действующий в договоре с ИД {0}.", contract.Id);
            countChangeC++;
          }
        }
        catch (Exception ex)
        {
          Logger.DebugFormat("Во время изменения статуса договора с ИД {0} - произошла ошибка: {1}", contract.Id, ex.Message);
        }
      }
      Logger.DebugFormat("Переведен статус в состояние Действующий в {0} договорах.", countChangeC);
      
      // ДС.
      var supAgreements = DirRX.Solution.SupAgreements.GetAll(s => s.LifeCycleState == DirRX.Solution.SupAgreement.LifeCycleState.Draft &&
                                                              s.RegistrationState == DirRX.Solution.SupAgreement.RegistrationState.Registered &&
                                                              s.ActivateDate.HasValue &&
                                                              Calendar.Today >= s.ActivateDate &&
                                                              (s.StartConditionsExists == false || (s.StartConditionsExists == true && s.AreConditionsCompleted == true)) &&
                                                              (
                                                                (s.ContractActivate == DirRX.Solution.SupAgreement.ContractActivate.Copy &&
                                                                 s.InternalApprovalState == DirRX.Solution.SupAgreement.InternalApprovalState.Signed &&
                                                                 s.ExternalApprovalState == DirRX.Solution.SupAgreement.ExternalApprovalState.Signed) ||
                                                                (s.ContractActivate == DirRX.Solution.SupAgreement.ContractActivate.Original &&
                                                                 s.OriginalSigning == DirRX.Solution.SupAgreement.OriginalSigning.Signed &&
                                                                 s.ContractorOriginalSigning == DirRX.Solution.SupAgreement.ContractorOriginalSigning.Signed)
                                                               ));
      var countChangeS = uint.MinValue;
      
      foreach (var supAgreement in supAgreements)
      {
        try
        {
          var lockInfo = Locks.GetLockInfo(supAgreement);
          var isLockedByOther = lockInfo != null && lockInfo.IsLocked;
          if (!isLockedByOther)
          {
            supAgreement.LifeCycleState = DirRX.Solution.SupAgreement.LifeCycleState.Active;
            supAgreement.Save();
            Logger.DebugFormat("Переведен статус в состояние Действующий в доп. соглашении с ИД {0}.", supAgreement.Id);
            countChangeS++;
          }
        }
        catch (Exception ex)
        {
          Logger.DebugFormat("Во время изменения статуса доп. соглашения с ИД {0} - произошла ошибка: {1}", supAgreement.Id, ex.Message);
        }
      }
      Logger.DebugFormat("Изменен статус в {0} доп.соглашениях.", countChangeS);
      Logger.DebugFormat("Выполнение фонового процесса: \"Перевод договорных докуменов в статус Действующий\" закончено. Изменен статус у {0} элементов.", countChangeC + countChangeS);
    }

    

    /// <summary>
    ///  Автоматичесткое выполнение заданий служебных пользователей.
    /// </summary>
    public virtual void ApprovalSendingAssigmentsComplete()
    {
      Logger.Debug("ApprovalSendingAssigmentsComplete start.");
      
      #region Выполнение заданий на отправку оригиналов.
      Logger.Debug("ApprovalSendingAssigmentsComplete: Выполнение заданий на отправку оригиналов.");
      
      var deadline = 0;
      var deadlineConstant = ContractConstants.GetAll(c => c.Sid == Constants.Module.OriginalsControlTaskDeadlineConstantGuid.ToString()).FirstOrDefault();
      if (deadlineConstant == null || !deadlineConstant.Period.HasValue || deadlineConstant.Unit != DirRX.ContractsCustom.ContractConstant.Unit.Day)
        Logger.Error("ApprovalSendingAssigmentsComplete: Не заполнена константа \"Срок задачи обеспечения возврата оригиналов\", или в константе указана единица измерения не равная \"Дни\"");
      else
        deadline = deadlineConstant.Period.Value;

      
      var documentsInPackages = DirRX.ContractsCustom.ShippingPackages.GetAll(p => p.PackageStatus == DirRX.ContractsCustom.ShippingPackage.PackageStatus.Sent).ToList().SelectMany(d => d.Documents.Select(x => x.Document)).ToList();
      // На закладке «Выдача» нет строки «Отправка контрагенту» с признаком «Оригинал»
      var documents = documentsInPackages.Where(d => !d.Tracking.Where(t => t.Action == Sungero.Docflow.OfficialDocumentTracking.Action.Sending && t.IsOriginal == true).Any());
      Logger.Debug(string.Format("ApprovalSendingAssigmentsComplete. Найдено документов для обработки: {0}", documents.Count()));
      
      foreach (var document in documents)
      {
        try
        {
          var offDocument = Sungero.Docflow.OfficialDocuments.As(document);
          Logger.Debug(string.Format("ApprovalSendingAssigmentsComplete. Обработка документа: id={0} {1}", offDocument.Id, offDocument.Name));
          
          var assigments = DirRX.Solution.ApprovalSendingAssignments.GetAll(a => a.Status == Sungero.Workflow.AssignmentBase.Status.InProcess &&
                                                                            DirRX.Solution.ApprovalStages.Is(a.Stage) &&
                                                                            DirRX.Solution.ApprovalStages.As(a.Stage).KindOfDocumentNeedSend == DirRX.Solution.ApprovalStage.KindOfDocumentNeedSend.Original).ToList()
            .Where(a => Sungero.Docflow.OfficialDocuments.Equals(offDocument, a.DocumentGroup.OfficialDocuments.FirstOrDefault()));
          // Если есть задания, то выполним.
          if (assigments.Count() > 0)
          {
            foreach (var assigment in assigments)
            {
              Logger.Debug(string.Format("ApprovalSendingAssigmentsComplete. Обработка задания с ИД = {0}", assigment.Id));
              var lockInfo = Locks.GetLockInfo(assigment);
              if (lockInfo.IsLockedByOther)
              {
                Logger.Debug(string.Format("ApprovalSendingAssigmentsComplete. Невозможно выполнить задание с ИД = {0}, т.к. задание на подписание заблокировано пользователем {1}: {2}",
                                           assigment.Id, lockInfo.OwnerName, lockInfo.LockedMessage));
              }
              else
              {
                // Выполним задание
                assigment.Complete(DirRX.Solution.ApprovalSendingAssignment.Result.Complete);
              }
            }
          }
          else
          {
            var contract = Solution.Contracts.As(offDocument);
            var supAgreement = Solution.SupAgreements.As(offDocument);
            // Отправителя получим из пакета.
            Sungero.Company.IEmployee performer = null;
            var package = DirRX.ContractsCustom.ShippingPackages.GetAll(p => p.PackageStatus == DirRX.ContractsCustom.ShippingPackage.PackageStatus.Sent &&
                                                                        p.Documents.Where(d => d.Document.Equals(offDocument)).Any())
              .OrderByDescending(p => p.SendedDate).FirstOrDefault();
            if (package != null && package.SendedEmployee != null)
              performer = package.SendedEmployee;
            else
              Logger.Debug(string.Format("ApprovalSendingAssigmentsComplete. Невозможно заполнить закладку Выдача для документа id={0} {1}, т.к. не найден пакет на отправку или не указан сотрудник, отправивший пакет.",
                                         offDocument.Id, offDocument.Name));
            if (performer != null)
            {
              Functions.Module.AddOriginalSendedToCounterpartyTracking(offDocument, performer, Calendar.Now.AddWorkingDays(deadline));
            }
          }
        }
        catch (Exception ex)
        {
          Logger.DebugFormat("Во время обработки документа с ИД {0} - произошла ошибка: {1} {2}", document.Id, ex.Message, ex.StackTrace);
        }
      }
      


      #endregion
    }
    
    #region Загрузить курсы валют с сайта ЦБ РФ.

    /// <summary>
    /// Загрузить курсы валют с сайта ЦБ РФ.
    /// </summary>
    public virtual void DownloadRates()
    {
      Logger.Debug("DownloadRates start.");
      var currencies = Functions.CurrencyRate.GetCurrencies();
      foreach (var currInfo in currencies)
      {
        DateTime startDate;
        var lastDate = Functions.CurrencyRate.GetLastRateDate(currInfo.Currency);
        // Курсы будут получены, начиная со следующего за последней датой дня.
        startDate = lastDate.HasValue ? lastDate.Value : Calendar.BeginningOfYear(Calendar.Now);
        if (startDate < Calendar.Today)
        {
          var newRates = Functions.CurrencyRate.GetRatesForCurrency(currInfo, startDate.AddDays(1));
          Functions.CurrencyRate.CreateRates(currInfo.Currency, newRates);
        }
      }
    }
    
    #endregion
    
  }
}