using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Company;
using System.IO;
using System.Xml;

namespace DirRX.Solution.Server
{
  partial class CompanyFunctions
  {
    /// <summary>
    /// Отправка запроса в КССС для поиска данных о контрагенте.
    /// </summary>
    /// <param name="fieldName">Наименование параметра по которому происходит поиск.</param>
    /// <param name="fieldValue">Значение параметра.</param>
    /// <returns>Пустая строка, если запрос выполнен успешно, иначе текст ошибки.</returns>
    [Remote]
    public string CreateRequest(string fieldName, string fieldValue)
    {
      // Обработка дублирования запроса.
      var request = _obj.KSSSRequests.FirstOrDefault(r => Users.Equals(r.User, Users.Current) && !r.Completed.GetValueOrDefault());
      if (request != null)
      {
        // Если данные запроса совпадают, то выводим сообщение об успешной отправке запроса. Иначе выводим информацию с значением предыдущего запроса.
        if (request.FieldName == fieldName && request.FieldValue == fieldValue)
          return string.Empty;

        if (request.FieldName == Constants.Parties.Company.KSSSParams.CSCDIDFieldName)
          return DirRX.Solution.Companies.Resources.CodeKSSSInProcessFormat(request.FieldValue);
        
        if (request.FieldName == Constants.Parties.Company.KSSSParams.INNFieldName)
          return DirRX.Solution.Companies.Resources.INNRequestInProcessFormat(request.FieldValue);
      }
      
      // Чтение параметров подключения из конфигурационного файла.
      var xmlDocument = new XmlDocument();
      try
      {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_KSSSConfig.xml");
        xmlDocument.Load(configPath);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Невозможно считать конфигурационный файл {0}", ex.Message);
        return DirRX.Solution.Companies.Resources.ReadConfigError;
      }
      
      var url = GetParameter(xmlDocument, "URL");
      var userName = GetParameter(xmlDocument, "USER_NAME");
      var password = GetParameter(xmlDocument, "PASSWORD");
      
      if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
      {
        Logger.Error("Не заполнены параметры в конфигурационном файле");
        return DirRX.Solution.Companies.Resources.ParamsError;
      }
      
      // Формирование параметров тела запроса и последующая отправка.
      var requestID = Guid.NewGuid().ToString();
      var condition = new List<Structures.Parties.Company.Condition>() { Structures.Parties.Company.Condition.Create(fieldName, Constants.Parties.Company.KSSSParams.EqualsOption, fieldValue) };
      var counterpartyRequest = Structures.Parties.Company.CounterpartyRequest.Create(requestID, Constants.Parties.Company.KSSSParams.SystemID, Constants.Parties.Company.KSSSParams.CatalogName, condition);
      
      var result = 0;
      try
      {
        var client = CSBConnector.Client.Instance;
        var JSONBody = DirRX.Solution.PublicFunctions.Module.SerializeObjectToJSON(counterpartyRequest);
        result = client.CallHttpClient(url, userName, password, JSONBody);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Ошибка при формировании запроса: {0}", ex.Message);
        return DirRX.Solution.Companies.Resources.ServiceError;
      }
      
      if (result < 200 || result > 202)
        return DirRX.Solution.Companies.Resources.ServiceError;
      
      // Обработка ответа и сохранение данных.
      var KSSSRequests = _obj.KSSSRequests.AddNew();
      KSSSRequests.Description = result.ToString();
      KSSSRequests.RequestID = requestID;
      KSSSRequests.User = Users.Current;
      KSSSRequests.DateTime = Calendar.Now;
      KSSSRequests.FieldName = fieldName;
      KSSSRequests.FieldValue = fieldValue;
      KSSSRequests.Completed = false;
      _obj.Save();
      
      return string.Empty;
    }
    
    /// <summary>
    /// Отправка запроса в КССС для поиска данных о контрагенте.
    /// </summary>
    /// <param name="fieldName">Наименование параметра по которому происходит поиск.</param>
    /// <param name="fieldValue">Значение параметра.</param>
    /// <returns>Пустая строка, если запрос выполнен успешно, иначе текст ошибки.</returns>
    [Remote]
    public string CreateRequest()
    {
      var fieldName = Constants.Parties.Company.KSSSParams.SendCounterpartyInfo;
      // Обработка дублирования запроса.
      var request = _obj.KSSSRequests.FirstOrDefault(r => Users.Equals(r.User, Users.Current) && !r.Completed.GetValueOrDefault());
      if (request != null)
      {
        // Если данные запроса совпадают, то выводим сообщение об успешной отправке запроса. Иначе выводим информацию с значением предыдущего запроса.
        if (request.FieldName == fieldName && request.FieldValue == fieldName)
          return string.Empty;

        if (request.FieldName == Constants.Parties.Company.KSSSParams.CSCDIDFieldName)
          return DirRX.Solution.Companies.Resources.CodeKSSSInProcessFormat(request.FieldValue);
        
        if (request.FieldName == Constants.Parties.Company.KSSSParams.INNFieldName)
          return DirRX.Solution.Companies.Resources.INNRequestInProcessFormat(request.FieldValue);
      }
      
      // Чтение параметров подключения из конфигурационного файла.
      var xmlDocument = new XmlDocument();
      try
      {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_KSSSConfig.xml");
        xmlDocument.Load(configPath);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Невозможно считать конфигурационный файл {0}", ex.Message);
        return DirRX.Solution.Companies.Resources.ReadConfigError;
      }
      
      var url = GetParameter(xmlDocument, "URLEXPORT");
      var userName = GetParameter(xmlDocument, "USER_NAME");
      var password = GetParameter(xmlDocument, "PASSWORD");
      
      if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
      {
        Logger.Error("Не заполнены параметры в конфигурационном файле");
        return DirRX.Solution.Companies.Resources.ParamsError;
      }
      
      // Формирование параметров тела запроса и последующая отправка.
      var requestID = Guid.NewGuid().ToString();
      var employee = Employees.Current != null ? Employees.Current.Name : String.Empty;
      var employeeEmail = Employees.Current != null ? Employees.Current.Email : String.Empty;
      var headCompany = (_obj.HeadCompany != null && Solution.Companies.Is(_obj.HeadCompany)) ? Solution.Companies.As(_obj.HeadCompany).KSSSContragentId.ToString() : String.Empty;
      var countryCode = _obj.Country != null ? _obj.Country.Code : String.Empty;
      var activeCodeKSSS = _obj.ActiveCodeKsss != null ? _obj.ActiveCodeKsss.KSSSContragentId.ToString() : String.Empty;
      var counterpartyRequest = Structures.Parties.Company.SendCountrparty.Create(String.Empty, _obj.Name, _obj.TIN, employee, employeeEmail,
                                                                                  requestID, _obj.LegalName, headCompany, 
                                                                                  _obj.Nonresident.GetValueOrDefault(false).ToString(),
                                                                                  _obj.CounterpartyType == CounterpartyType.Individual ? "true" : "false",
                                                                                  String.Empty, _obj.TRRC, _obj.PSRN, _obj.NCEO, _obj.NCEA,
                                                                                  countryCode, _obj.LegalAddress, _obj.PostalAddress, _obj.Phones,
                                                                                  _obj.Email, _obj.Homepage, _obj.Note,
                                                                                  _obj.Status == Status.Active ? "true" : "false",
                                                                                  activeCodeKSSS, String.Empty, String.Empty);
      
      var result = 0;
      try
      {
        var client = CSBConnector.Client.Instance;
        var JSONBody = DirRX.Solution.PublicFunctions.Module.SerializeObjectToJSON(counterpartyRequest);
        result = client.CallHttpClient(url, userName, password, JSONBody);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Ошибка при формировании запроса: {0}", ex.Message);
        return DirRX.Solution.Companies.Resources.ServiceError;
      }
      
      if (result < 200 || result > 202)
      {
        Logger.ErrorFormat("Ошибка при отправке запроса: {0}", result);
        return DirRX.Solution.Companies.Resources.ServiceError;
      }
      
      // Обработка ответа и сохранение данных.
      var KSSSRequests = _obj.KSSSRequests.AddNew();
      KSSSRequests.Description = result.ToString();
      KSSSRequests.RequestID = requestID;
      KSSSRequests.User = Users.Current;
      KSSSRequests.DateTime = Calendar.Now;
      KSSSRequests.FieldName = fieldName;
      KSSSRequests.FieldValue = fieldName;
      KSSSRequests.Completed = false;
      _obj.Save();
      
      return string.Empty;
    }
    
    /// <summary>
    /// Обработка параметров конфигурационного файла.
    /// </summary>
    /// <param name="document">XML документ.</param>
    /// <param name="parameter">Параметр.</param></param>
    /// <returns>Значение параметра, null, если исключение.</returns>
    private static string GetParameter(XmlDocument document, string parameter)
    {
      try
      {
        foreach (XmlNode node in document.DocumentElement.ChildNodes)
        {
          if (node.Attributes["name"].Value == parameter)
            return node.Attributes["value"].Value;
        }
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Невозможно получить параметр с именем {0} {1} {2}", parameter, Environment.NewLine, ex);
      }
      
      return null;
    }
    
    /// <summary>
    /// Обработка параметров конфигурационного файла.
    /// </summary>
    /// <param name="document">Путь к XML документу.</param>
    /// <param name="parameter">Параметр.</param></param>
    /// <returns>Значение параметра, null, если исключение.</returns>
    [Public]
    public static string GetParameter(string configPath, string parameter)
    {
      try
      {
        var xmlDocument = new XmlDocument();
        xmlDocument.Load(configPath);
        return GetParameter(xmlDocument, parameter);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Невозможно считать конфигурационный файл {0} {1} {2}", configPath, Environment.NewLine, ex);
      }
      
      return null;
    }
    
    /// <summary>
    /// Задача уполномоченному сотруднику на включение/исключение контрагента в стоп-лист.
    /// </summary>
    /// <param name="isInclude">Признак включения в стоп-лист, иначе исключение.</param>
    /// <returns>Задача.</returns>
    [Remote]
    public Sungero.Workflow.ISimpleTask GetIncludeExcludeStoplistTask(IRole role, string comment, bool isInclude)
    {
      if (role == null)
        return null;
      
      string subject = isInclude ? Companies.Resources.IncludeStoplistTaskSubject : Companies.Resources.ExcludeStoplistTaskSubject;
      var performerTask = Sungero.Workflow.SimpleTasks
        .GetAll(t => (t.Status == Sungero.Workflow.Task.Status.InProcess ||
                      t.Status == Sungero.Workflow.Task.Status.UnderReview) &&
                t.Subject.Contains(subject) &&
                t.RouteSteps.Any(s => Equals(s.Performer, role)))
        .ToList()
        .FindAll(t => t.Attachments.Any(a => Equals(a, _obj)))
        .OrderByDescending(t => t.Created.Value)
        .FirstOrDefault();

      if (performerTask != null)
        return performerTask;

      var task = Sungero.Workflow.SimpleTasks.Create();
      task.Subject = string.Format("{0}: {1}", subject, _obj.Name);
      task.ActiveText = isInclude ? Companies.Resources.IncludeStoplistTaskTextFormat(_obj.Name, comment) : Companies.Resources.ExcludeStoplistTaskTextFormat(_obj.Name, comment);
      task.Attachments.Add(_obj);
      
      var step = task.RouteSteps.AddNew();
      step.AssignmentType = Sungero.Workflow.SimpleTask.AssignmentType.Assignment;
      step.Performer = role;
      step.Deadline = Calendar.Today.AddWorkingDays(role, 3);

      return task;
    }
    
    /// <summary>
    /// Уведомление исполнителю, руководителю клиентского сервиса о включении/исключении контрагента в стоп-лист.
    /// </summary>
    /// <param name="isInclude">Признак включения в стоп-лист, иначе исключение.</param>
    [Remote]
    public void SendNoticeIncludeExcludeStoplist(IRole role, bool isInclude)
    {
      // Исполнитель - инициатор задачи уполномоченному сотруднику.
      IUser performer = null;
      
      // TODO: Убрать логику отправки уведомлений инициатору.
      //      if (role != null && !Users.Current.IncludedIn(role))
      //      {
      //        string subject = isInclude ? Companies.Resources.IncludeStoplistTaskSubject : Companies.Resources.ExcludeStoplistTaskSubject;
      //        var performerTask = Sungero.Workflow.SimpleTasks
      //          .GetAll(t => t.Subject.Contains(subject) &&
      //                  t.RouteSteps.Any(s => Equals(s.Performer, role)))
      //          .ToList()
      //          .FindAll(t => t.Attachments.Any(a => Equals(a, _obj)))
      //          .OrderByDescending(t => t.Created.Value)
      //          .FirstOrDefault();
      //        if (performerTask != null)
      //          performer = performerTask.Author;
      //      }
      
      // Руководитель клиентского сервиса.
      var clientServiceManager = Roles.GetAll(r => r.Sid == PartiesControl.PublicConstants.Module.ClientServiceManagerRole).FirstOrDefault();
      
      // Роль для уведомлений по стоп-листу.
      var stopListNoticeRole = Roles.GetAll(r => r.Sid == PartiesControl.PublicConstants.Module.StopListNoticeRole).FirstOrDefault();
      
      if (performer == null && clientServiceManager == null)
        return;

      var task = Sungero.Workflow.SimpleTasks.Create();
      task.Subject = isInclude ? Companies.Resources.IncludeStoplistNoticeSubjectFormat(_obj.Name) : Companies.Resources.ExcludeStoplistNoticeSubjectFormat(_obj.Name);
      task.ActiveText = isInclude ? Companies.Resources.IncludeStoplistNoticeTextFormat(_obj.Name) : Companies.Resources.ExcludeStoplistNoticeTextFormat(_obj.Name);
      task.Attachments.Add(_obj);
      
      if (performer != null)
      {
        var step = task.RouteSteps.AddNew();
        step.AssignmentType = Sungero.Workflow.SimpleTask.AssignmentType.Notice;
        step.Performer = performer;
      }
      
      if (clientServiceManager != null)
      {
        var step = task.RouteSteps.AddNew();
        step.AssignmentType = Sungero.Workflow.SimpleTask.AssignmentType.Notice;
        step.Performer = clientServiceManager;
      }
      
      if (stopListNoticeRole != null)
      {
        var step = task.RouteSteps.AddNew();
        step.AssignmentType = Sungero.Workflow.SimpleTask.AssignmentType.Notice;
        step.Performer = stopListNoticeRole;
      }
      
      task.Start();
    }

    /// <summary>
    /// Формирование и отправка в КСШ информации о включении/исключении контрагентов в стоп-лист.
    /// </summary>
    /// <param name="stoplistRecord">Запись истории включения в стоп-лист.</param>
    [Public, Remote]
    public static string CreateAndSendStoplistToCSB(ICompanyStoplistHistory stoplistRecord, string stoplistAction, string stoplistStatus, string comment)
    {
      var eventGUID = string.Empty;
      var status = string.Empty;
      var responsibleName = string.Empty;
      var action = string.Empty;
      
      if (stoplistAction == Constants.Parties.Company.CSBStoplistAction.Include)
      {
        action = "B";
        responsibleName = stoplistRecord.IncUser.Name;
      }
      else
      {
        action = "A";
        responsibleName = stoplistRecord.ExcUser.Name;
      }

      if (stoplistStatus == Constants.Parties.Company.CSBStoplistStatus.Started)
      {
        status = "X";
        eventGUID = Guid.NewGuid().ToString();
      }
      else
      {
        status = "Z";
        eventGUID = stoplistRecord.EventGUID;
      }
      
      var sapStoplistEvent = new DirRX.Solution.Structures.Module.SAPStoplistEvent();
      sapStoplistEvent.KSSSContragentId = stoplistRecord.Company.KSSSContragentId.ToString();
      sapStoplistEvent.StopListState = action;
      sapStoplistEvent.GUIDEvent = eventGUID;
      sapStoplistEvent.EventState = status;
      sapStoplistEvent.EventDate = Calendar.Today.ToString("yyyyMMdd");
      sapStoplistEvent.IDReason = stoplistRecord.Reason.Id;
      sapStoplistEvent.Reason = stoplistRecord.Reason.Name;
      sapStoplistEvent.Comment = comment;
      sapStoplistEvent.Employee = responsibleName;
      
      var json = DirRX.Solution.PublicFunctions.Module.SerializeObjectToJSON(sapStoplistEvent);
      int responseStatus = 0;
      try
      {
        responseStatus = DirRX.Solution.PublicFunctions.Module.SendStoplistToCSB(json);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("При отправке в КСШ информации о включении/исключении контрагентов в стоп-лист произошла ошибка: ", ex);
      }
      
      if (responseStatus >= 200 && responseStatus <= 202)
      {
        if (action == Constants.Parties.Company.CSBStoplistAction.Include)
          Logger.DebugFormat("Информация о включении контрагента с ИД {0} в стоп-лист успешно передан в КСШ с кодом ответа: {1}", stoplistRecord.Company.Id, responseStatus);
        if (action == Constants.Parties.Company.CSBStoplistAction.Include)
          Logger.DebugFormat("Информация о исключении контрагента с ИД {0} в стоп-лист успешно передан в КСШ с кодом ответа: {1}", stoplistRecord.Company.Id, responseStatus);
        
        return eventGUID;
      }
      
      return string.Empty;
    }

    [Public, Remote(IsPure = true)]
    public IQueryable<PartiesControl.IRevisionRequest> OpenRevisionRequests()
    {
      return PartiesControl.RevisionRequests.GetAll().Where(x => Solution.Companies.Equals(x.Counterparty, _obj));
    }
    
    /// <summary>
    /// Список договоров/ДС, которые были заключены с контрагентом, пока он находился в статусе «Стоп-лист» или «Не одобрен».
    /// </summary>
    /// <returns>Список договоров/ДС</returns>
    [Remote(IsPure = true)]
    public IQueryable<Sungero.Contracts.IContractualDocument> GetUnapprovedCounterpartyContracts()
    {
      // Статусы контрагента «Стоп-лист», «Не одобрен».
      var statuses = new List<DirRX.PartiesControl.ICounterpartyStatus>();
      // «Стоп-лист».
      var stoplistStatus = DirRX.PartiesControl.PublicFunctions.CounterpartyStatus.Remote.GetCounterpartyStatus(PartiesControl.PublicConstants.CounterpartyStatus.DefaultStatus.StopListSid);
      statuses.Add(stoplistStatus);
      // «Не одобрен»  указан в поле CounterpartyStatus записи справочника CheckingResult с значением Decision = NotApproved.
      var notApprovedCheckingResult = DirRX.PartiesControl.CheckingResults.GetAll(r => r.Decision == DirRX.PartiesControl.CheckingResult.Decision.NotApproved).FirstOrDefault();
      if (notApprovedCheckingResult != null && notApprovedCheckingResult.CounterpartyStatus != null)
        statuses.Add(notApprovedCheckingResult.CounterpartyStatus);
      
      // Доступные состояния документа.
      // TODO дополнить состояниями после добавления
      var сycleStates = new List<Enumeration>();
      сycleStates.Add(Sungero.Docflow.OfficialDocument.LifeCycleState.Active);
      сycleStates.Add(Solution.Contract.LifeCycleState.Terminated);
      сycleStates.Add(Solution.Contract.LifeCycleState.Closed);
      
      return Sungero.Contracts.ContractualDocuments.GetAll(c => Solution.Companies.Equals(c.Counterparty, _obj) && c.LifeCycleState.HasValue && сycleStates.Contains(c.LifeCycleState.Value) &&
                                                           ((Solution.Contracts.Is(c) && statuses.Contains(Solution.Contracts.As(c).CounterpartyStatus)) ||
                                                            (Solution.SupAgreements.Is(c) && statuses.Contains(Solution.SupAgreements.As(c).CounterpartyStatus))));

    }
  }
}