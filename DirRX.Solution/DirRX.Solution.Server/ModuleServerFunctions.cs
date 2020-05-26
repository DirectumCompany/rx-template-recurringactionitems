using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using System.IO;
using System.Xml;
using System.Reflection;
using Sungero.Domain.Shared;
using Sungero.Metadata;
using Newtonsoft.Json;
using CSBConnector;

namespace DirRX.Solution.Server
{
  public class ModuleFunctions
  {

    /// <summary>
    /// Находится ли сотрудник в прямом подчинении ГД.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>True если сотрудник находится в прямом подчинении ГД.</returns>
    [Public, Remote(IsPure = true)]
    public static bool IsSubordinateCEOManager(DirRX.Solution.IEmployee employee)
    {
      if (employee == null)
        return false;
      
      // Получение ГД.
      var CEO = DirRX.ActionItems.PublicFunctions.ActionItemsRole.Remote.GetCEO(employee);
      
      // Руководителем сотрудника является ГД.
      if (DirRX.Solution.Employees.Equals(employee.Manager, CEO))
        return true;
      // Сотрудник находится в подразделении, руководителем которого является ГД.
      if (employee.Manager == null && employee.Department != null &&
          DirRX.Solution.Employees.Equals(employee.Department.Manager, CEO))
        return true;
      // Сотрудник является руководителем подразделения и у головного подразделения руководителем является ГД.
      if (employee.Manager == null && employee.Department != null &&
          Sungero.Company.Employees.Equals(employee, employee.Department.Manager) &&
          employee.Department.HeadOffice != null &&
          DirRX.Solution.Employees.Equals(employee.Department.HeadOffice.Manager, CEO))
        return true;
      
      return false;
    }

    #region Отправка договора в КСШ.
    
    /// <summary>
    /// Отправка договора в КСШ.
    /// </summary>
    [Public, Remote]
    public int SendSAPContractToCSB(string json)
    {
      // Чтение параметров подключения из конфигурационного файла.
      var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_CSBConnectorConfig.xml");
      var url = DirRX.Solution.PublicFunctions.Company.GetParameter(configPath, "CONTRACT_SERVICE_URL");
      var login = DirRX.Solution.PublicFunctions.Company.GetParameter(configPath, "USER_NAME");
      var password = DirRX.Solution.PublicFunctions.Company.GetParameter(configPath, "USER_PASSWORD");
      
      if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        throw new Exception("Не заполнены параметры в конфигурационном файле");

      var responseCode = 0;
      try
      {
        var client = CSBConnector.Client.Instance;
        responseCode = client.CallHttpClient(url, login, password, json);
      }
      catch (Exception ex)
      {
        Logger.Error("При экспорте договоров и дополнительных соглашений в SAP возникла ошибка.", ex);
      }
      
      return responseCode;
    }
    
    /// <summary>
    /// Отправка в КСШ информации о включении/исключении контрагентов в стоп-лист.
    /// </summary>
    [Public]
    public int SendStoplistToCSB(string json)
    {
      // Чтение параметров подключения из конфигурационного файла.
      var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_CSBConnectorConfig.xml");
      var url = DirRX.Solution.PublicFunctions.Company.GetParameter(configPath, "STOPLIST_SERVICE_URL");
      var login = DirRX.Solution.PublicFunctions.Company.GetParameter(configPath, "USER_NAME");
      var password = DirRX.Solution.PublicFunctions.Company.GetParameter(configPath, "USER_PASSWORD");
      
      if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        throw new Exception("Не заполнены параметры в конфигурационном файле");

      var responseCode = 0;
      try
      {
        var client = CSBConnector.Client.Instance;
        responseCode = client.CallHttpClient(url, login, password, json);
      }
      catch (Exception ex)
      {
        Logger.Error("При отправке в КСШ информации о включении/исключении контрагентов в стоп-лист произошла ошибка.", ex);
      }
      
      return responseCode;
    }
    
    /// <summary>
    /// Преобразовать структуру с строку JSON.
    /// </summary>
    [Public]
    public string SerializeObjectToJSON(object entity)
    {
      var json = JsonConvert.SerializeObject(entity, Newtonsoft.Json.Formatting.Indented);
      Logger.Debug(json);
      return json;
    }

    #endregion

    /// <summary>
    /// Формирование пакетов на отправку.
    /// </summary>
    /// <param name="documents">Документы для формирования пакетов.</param>
    /// <param name="counterparty">Контрагент (передается для формирования пакета для многостороннего договора/ДС).</param>
    /// <returns>Список созданных пакетов к отправке и сообщение для диалога. Если не было документов для формирования пакетов на отправку, то об этом будет в тексте сообщения</returns>
    [Remote]
    public DirRX.Solution.Structures.Module.CreatePackagesResult CreatePackages(List<Sungero.Contracts.IContractualDocument> documents, Solution.ICompany counterparty)
    {
      var createdPackages = new List<DirRX.ContractsCustom.IShippingPackage>();
      var message = string.Empty;
      var companies = new List<DirRX.Solution.ICompany>();
      var deliveryMethods = new List<DirRX.Solution.IMailDeliveryMethod>();
      var addresses = new List<DirRX.PartiesControl.IShippingAddress>();
      var contacts = new List<DirRX.Solution.IContact>();
      
      if (documents.Count > 0)
      {
        // Пройдем по списку документов и соберем списки контрагентов, методов доставки, адресов и контактов для создания пакетов.
        foreach (var document in documents)
        {
          var contractDoc = DirRX.Solution.Contracts.As(document);
          if (contractDoc != null)
          {
            // Для многостороннего договора (всегда будет 1) получим запись с переданным в параметре контрагентом, для одностороннего - первую в списке.
            var cp = contractDoc.Counterparties.Where(c => contractDoc.IsManyCounterparties != true || Solution.Companies.Equals(c.Counterparty, counterparty)).FirstOrDefault();
            if (cp != null)
            {
              var company = cp.Counterparty;
              if (!companies.Contains(company))
                companies.Add(company);
              var deliveryMethod = cp.DeliveryMethod;
              if (!deliveryMethods.Contains(deliveryMethod))
                deliveryMethods.Add(deliveryMethod);
              var contact = cp.Contact;
              if (!contacts.Contains(contact))
                contacts.Add(contact);
              var address = cp.Address;
              if (!addresses.Contains(address))
                addresses.Add(address);
            }
          }
          var supAgreement = DirRX.Solution.SupAgreements.As(document);
          if (supAgreement != null)
          {
            // Для многостороннего ДС (всегда будет 1) получим запись с переданным в параметре контрагентом, для одностороннего - первую в списке.
            var cp = supAgreement.Counterparties.Where(c => supAgreement.IsManyCounterparties != true || Solution.Companies.Equals(c.Counterparty, counterparty)).FirstOrDefault();
            if (cp != null)
            {
              var company = cp.Counterparty;
              if (!companies.Contains(company))
                companies.Add(company);
              var deliveryMethod = cp.DeliveryMethod;
              if (!deliveryMethods.Contains(deliveryMethod))
                deliveryMethods.Add(deliveryMethod);
              var contact = cp.Contact;
              if (!contacts.Contains(contact))
                contacts.Add(contact);
              var address = cp.Address;
              if (!addresses.Contains(address))
                addresses.Add(address);
            }
          }
        }
        // Количество пакетов для создания.
        var totalPackagesCount = 0;
        // Создадим пакеты на отправку.
        foreach (var company in companies)
        {
          foreach (var deliveryMethod in deliveryMethods)
          {
            foreach (var contact in contacts)
            {
              foreach (var address in addresses)
              {
                try
                {
                  var packDocs = documents.Where(d => (Contracts.Is(d) && Contracts.As(d).Counterparties.Any
                                                       (cp => Solution.Companies.Equals(cp.Counterparty, company) &&
                                                        Solution.MailDeliveryMethods.Equals(cp.DeliveryMethod, deliveryMethod) &&
                                                        PartiesControl.ShippingAddresses.Equals(cp.Address, address) &&
                                                        Sungero.Parties.Contacts.Equals(cp.Contact, contact))) ||
                                                 (SupAgreements.Is(d) && SupAgreements.As(d).Counterparties.Any
                                                  (cp => Solution.Companies.Equals(cp.Counterparty, company) &&
                                                   Solution.MailDeliveryMethods.Equals(cp.DeliveryMethod, deliveryMethod) &&
                                                   PartiesControl.ShippingAddresses.Equals(cp.Address, address) &&
                                                   Sungero.Parties.Contacts.Equals(cp.Contact, contact))));
                  if (packDocs.Count() > 0)
                  {
                    totalPackagesCount += 1;
                    // Создадим пакет.
                    var package = DirRX.ContractsCustom.ShippingPackages.Create();
                    package.Counterparty = company;
                    package.DeliveryMethod = deliveryMethod;
                    package.Contact = contact;
                    package.ShippingAddress = address;
                    foreach (var doc in packDocs)
                    {
                      var row = package.Documents.AddNew();
                      row.Document = doc;
                      
                      // Запись в очередь на установку статуса договора "Оригинал документа помещен в пакет для отправки".
                      var item = ContractsCustom.ContractQueueItems.Create();
                      item.DocumentId = doc.Id;
                      item.ContractStatusAction = ContractsCustom.PublicConstants.Module.StatusAction.AddAction;
                      item.ContractStatusType = ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus;
                      item.ContractStatusSid = ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalPlacedForSendingGuid.ToString();
                      item.Save();
                    }
                    package.Save();
                    createdPackages.Add(package);
                  }
                }
                catch (Exception ex)
                {
                  Logger.Error(ex.Message);
                }
              }
            }
          }
        }
        // Сообщение для диалога.
        message = DirRX.Solution.Resources.CreatePackagesSuccessfullyFormat(createdPackages.Count, totalPackagesCount, documents.Count);
        
        // Запустить агент установки статусов договоров.
        ContractsCustom.Jobs.SetContractStatusInPackagesJob.Enqueue();
      }
      else
        message = DirRX.Solution.Resources.CreatePackageDocsNotFound;
      
      return DirRX.Solution.Structures.Module.CreatePackagesResult.Create(createdPackages, message);
    }
    
    
    /// <summary>
    /// Получить список всех сотрудников.
    /// </summary>
    /// <returns>Список всех сотрудников.</returns>
    [Remote(IsPure = true)]
    public IQueryable<DirRX.Solution.IEmployee> GetAllEmployees()
    {
      return DirRX.Solution.Employees.GetAll();
    }
    
    /// <summary>
    /// Вставка информации о подписанте по тэгам в PublicBody.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="performer">Подписывающий.</param>
    public static void SetSignatureInfoInPublicBody(Sungero.Docflow.IOfficialDocument document, IUser performer)
    {
      if (performer == null)
        return;
      if (document.LastVersion.AssociatedApplication.Extension != Constants.Module.SignatureInfo.DocxDocumentExtension)
        return;
      
      var employee = Sungero.Company.Employees.As(performer);
      var employeeName = Sungero.Company.PublicFunctions.Employee.GetShortName(employee, DeclensionCase.Nominative, false);
      var order = Solution.Orders.As(document);
      
      // Поиск формата имени по параметрам шаблона. По-умолчанию Фамилия И.О.
      if (order != null)
      {
        var template = order.StandardForm.Template;
        if (template != null)
        {
          var employeeTag = template.Parameters.FirstOrDefault(p => p.Name == Constants.Module.SignatureInfo.EmployeeTagName);
          if (employeeTag != null)
          {
            // Формат И.О.Фамилия.
            if (employeeTag.DataSource.Contains(Constants.Module.ConverterAttribute.InitialsAndLastName))
              employeeName = Sungero.Company.PublicFunctions.Employee.GetReverseShortName(employee);
            
            // Формат Фамилия Имя Отчество.
            if (employeeTag.DataSource.Contains(Constants.Module.ConverterAttribute.FullName))
              employeeName = employee.DisplayValue;
          }
        }
      }
      
      var signatureTagsInfo = new Dictionary<string, string>();
      signatureTagsInfo.Add(Constants.Module.SignatureInfo.EmployeeTagName, employeeName);
      if (employee.JobTitle != null)
        signatureTagsInfo.Add(Constants.Module.SignatureInfo.JobTitleTagName, employee.JobTitle.DisplayValue);
      
      var asposeHelper = AsposeHelper.AsposeHelper.Instance;
      try
      {
        using (var documentStream = GetDocumentStream(document))
        {
          document.LastVersion.PublicBody.Write(asposeHelper.SetTextInTags(documentStream, signatureTagsInfo));
          document.Save();
        }
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
      }
    }
    
    /// <summary>
    /// Вставка информации о регистрационных данных по тэгам в PublicBody.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="regNumber">Регистрационный номер.</param>
    public static void SetRegInfoInPublicBody(Sungero.Docflow.IOfficialDocument document, string regNumber, DateTime? regDate)
    {
      if (document.LastVersion == null || document.LastVersion.AssociatedApplication.Extension != Constants.Module.SignatureInfo.DocxDocumentExtension)
        return;
      
      if (string.IsNullOrEmpty(regNumber) || !regDate.HasValue)
        return;

      var asposeHelper = AsposeHelper.AsposeHelper.Instance;
      try
      {
        using (var documentStream = GetDocumentStream(document))
        {
          var updateStream = asposeHelper.SetTextInTag(documentStream, Constants.Module.SignatureInfo.RegNameTagName, regNumber);
          updateStream = asposeHelper.SetDateInTag(updateStream, Constants.Module.SignatureInfo.RegDateTagName, regDate.Value);
          document.LastVersion.PublicBody.Write(updateStream);
          document.Save();
        }
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
      }
    }
    
    /// <summary>
    /// Получить поток с телом документа для последующей обработки.
    /// По сути HACK, который обходит ошибки при работе с MemoryStream.
    /// </summary>
    /// <param name="documents">Список документов для экспорта.</param>
    private static MemoryStream GetDocumentStream(Sungero.Docflow.IOfficialDocument document)
    {
      var directoryPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
      System.IO.Directory.CreateDirectory(directoryPath);
      var filePath = System.IO.Path.Combine(directoryPath, string.Format("{0}.{1}", Guid.NewGuid().ToString(), Constants.Module.SignatureInfo.DocxDocumentExtension));
      document.Export(filePath);
      
      var outDocumentStream = new MemoryStream();
      using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        file.CopyTo(outDocumentStream);
      System.IO.Directory.Delete(directoryPath, true);
      
      return outDocumentStream;
    }
    
    /// <summary>
    /// Заполнение руководителей сотрудника.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <param name="recipients">Коллекция сотрудников.</param>
    [Remote(IsPure = true), Public]
    public static void GetManagers(DirRX.Solution.IEmployee employee, List<IRecipient> recipients, int iteration)
    {
      // Ограничиваем количество итераций, т.к. есть вероятность зацикливания.
      // В каждый вызов функции передаём текущий номер итерации и увеличиваем её на 1, если достигли 20-ти, то возвращаем null.
      if (iteration > 20)
        return;
      else
        iteration++;
      
      // Приоритет у руководителя в карточке сотрудника, если он не указан, то ищем по подразделению.
      // Если сотрудник сам является руководителем своего подразделения, то ищем по головному подразделению.
      if (employee.Manager != null)
      {
        if (recipients.Contains(employee.Manager))
          return;
        else
        {
          recipients.Add(employee.Manager);
          GetManagers(employee.Manager, recipients, iteration);
        }
      }
      else if (employee.Department != null && employee.Department.Manager != null &&
               !Sungero.Company.Employees.Equals(employee, employee.Department.Manager))
      {
        if (recipients.Contains(employee.Department.Manager))
          return;
        else
        {
          recipients.Add(employee.Department.Manager);
          GetManagers(DirRX.Solution.Employees.As(employee.Department.Manager), recipients, iteration);
        }
      }
      else if (employee.Department != null && employee.Department.HeadOffice != null && employee.Department.HeadOffice.Manager != null)
      {
        if (recipients.Contains(employee.Department.HeadOffice.Manager))
          return;
        else
        {
          recipients.Add(employee.Department.HeadOffice.Manager);
          GetManagers(DirRX.Solution.Employees.As(employee.Department.HeadOffice.Manager), recipients, iteration);
        }
      }
      else
        return;
    }
    
    /// <summary>
    /// Установить статус риска независимо от прав.
    /// </summary>
    /// <param name="risk">Риск.</param>
    [Remote, Public]
    public static void SetRiskStatusClosed(LocalActs.IRisk risk)
    {
      if (risk.Status == LocalActs.Risk.Status.Closed)
        return;
      
      // HACK: если нет прав, то статус будет заполнен независимо от прав доступа.
      if (!risk.AccessRights.CanUpdate())
      {
        // CORE: использование сессии.
        using (var session = new Sungero.Domain.Session())
        {
          AddFullRightsInSession(session, risk);
          risk.Status = LocalActs.Risk.Status.Closed;
          risk.Save();
        }
      }
      else
      {
        risk.Status = LocalActs.Risk.Status.Closed;
        risk.Save();
      }
    }
    
    #region Скопировано из стандартной разработки.
    
    /// <summary>
    /// Выдать полные права на сущность в рамках сессии.
    /// </summary>
    /// <param name="session">Сессия.</param>
    /// <param name="entity">Сущность, на которую будут выданы полные права.</param>
    private static void AddFullRightsInSession(Sungero.Domain.Session session, Sungero.Domain.Shared.IEntity entity)
    {
      if (session == null || entity == null)
        return;
      
      var submitAuthorizationManager = session.GetType()
        .GetField("submitAuthorizationManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        .GetValue(session);
      var authManagerType = submitAuthorizationManager.GetType();
      var authCache = (Dictionary<Sungero.Domain.Shared.IEntity, int>)authManagerType
        .GetField("authorizedOperationsCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        .GetValue(submitAuthorizationManager);
      if (!authCache.ContainsKey(entity))
        authCache.Add(entity, -1);
      else
        authCache[entity] = -1;
    }
    
    /// <summary>
    /// Получить задачу с полными правами в рамках сессии.
    /// </summary>
    /// <param name="session">Сессия.</param>
    /// <param name="taskId">Id задачи.</param>
    /// <returns>Задача.</returns>
    public static Sungero.Workflow.ITask GetCheckReturnTaskWithRights(Sungero.Domain.Session session, int taskId)
    {
      var innerSession = (Sungero.Domain.ISession)session.GetType()
        .GetField("InnerSession", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(session);
      var task = Sungero.Workflow.Tasks.As((IEntity)innerSession.Get(typeof(Sungero.Workflow.ITask), taskId));
      AddFullRightsInSession(session, task);
      return task;
    }
    
    #endregion
    
    [Remote]
    public void ChangeAuthentication()
    {
      Logger.Debug("Start set windows authentication");
      foreach (var employee in Sungero.Company.Employees.GetAll(e => e.Status == Sungero.Company.Employee.Status.Active))
      {
        if (employee.Login != null && !Locks.GetLockInfo(employee.Login).IsLocked)
        {
          employee.Login.TypeAuthentication = Sungero.CoreEntities.Login.TypeAuthentication.Windows;
          employee.Login.Save();
        }
        else
          Logger.Debug(string.Format("Login is locked, employee id {0}", employee.Id));
      }
    }
    
    /// <summary>
    /// Прекратить задачу.
    /// </summary>
    /// <param name="taskId">ИД задачи.</param>
    [Remote, Public]
    public static void AbortTask(int taskId)
    {
      using (var session = new Sungero.Domain.Session())
      {
        // Получить задачу.
        var task = GetCheckReturnTaskWithRights(session, taskId);
        // Если не получить задания в работе с полными правами, то задача не прекратится, т.к. нет прав на задания остальных согласющих.
        var assignments = GetCheckReturnAssignmentsWithRights(session, taskId);
        task.Abort();
      }
    }
    
    /// <summary>
    /// Получить задание с полными правами в рамках сессии.
    /// </summary>
    /// <param name="session">Сессия.</param>
    /// <param name="taskId">Id задачи.</param>
    /// <returns>Задание контроля возврата (ApprovalCheckReturnAssignments, CheckReturns, ReturnDocuments).</returns>
    public static IQueryable<Sungero.Workflow.IAssignment> GetCheckReturnAssignmentsWithRights(Sungero.Domain.Session session, int taskId)
    {
      var innerSession = (Sungero.Domain.ISession)session.GetType()
        .GetField("InnerSession", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(session);
      
      var assignments = innerSession.GetAll<Sungero.Workflow.IAssignment>()
        .Where(a => a.Task != null && a.Task.Id == taskId && a.Status == Sungero.Workflow.AssignmentBase.Status.InProcess);
      foreach (var assignment in assignments)
        AddFullRightsInSession(session, assignment);
      return assignments;
    }
    
    /// <summary>
    /// Отправить инициатору согласования уведомление (подзадачей).
    /// </summary>
    /// <param name="assignment">Задание, к которому создается подзадача.</param>
    /// <param name="subject">Тема уведомления.</param>
    [Remote, Public]
    public static void CreateAuthorNotice(Sungero.Workflow.IAssignment assignment, string subject)
    {
      var subTask = Sungero.Workflow.SimpleTasks.CreateAsSubtask(assignment);
      subTask.Subject = subject;
      subTask.AssignmentType = Sungero.Workflow.SimpleTask.AssignmentType.Notice;
      var step = subTask.RouteSteps.AddNew();
      step.Performer = assignment.Task.Author;
      step.AssignmentType = Sungero.Workflow.SimpleTask.AssignmentType.Notice;
      subTask.NeedsReview = false;
      subTask.Start();
    }
    
    /// <summary>
    /// Проверка включения текущего пользователя в замещения по процессам.
    /// </summary>
    /// <param name="user">Замещаемый.</param>
    /// <param name="processes">Процессы.</param>
    /// <param name="withDefaultSubstitution">С учётом стандартных замещений.</param>
    /// <returns>True, если у текущего пользователя настроены замещения по данным процессам.</returns>
    [Public, Remote(IsPure = true)]
    public bool IsProcessSubstitute(IUser user, bool withDefaultSubstitution, List<Enumeration> processes)
    {
      var isProcessSubstitute = false;
      var substitution  = ProcessSubstitutionModule.ProcessSubstitutions.GetAll(s => Users.Equals(s.Employee, user)).FirstOrDefault();
      if (substitution != null)
        isProcessSubstitute = substitution.SubstitutionCollection.Any(s => Users.Equals(Users.Current, s.Substitute) && processes.Contains(s.Process.GetValueOrDefault()));
      
      return withDefaultSubstitution ? (isProcessSubstitute || Substitutions.ActiveSubstitutedUsersWithoutSystem.Contains(user)) : isProcessSubstitute;
    }
    
    /// <summary>
    /// Проверка включения текущего пользователя в роль с одним пользователем с учётом стандартных замещений и замещений по процессам.
    /// </summary>
    /// <param name="roleSid">Гуид роли.</param>
    /// <param name="processes">Процессы./param>
    /// <returns>True, если пользователь включён в роль с учётом замещений.</returns>
    [Remote(IsPure = true), Public]
    public bool IncludedInRoleWithSubsitute(Guid roleSid, List<Enumeration> processes)
    {
      var role = Roles.GetAll().FirstOrDefault(r => r.Sid == roleSid);
      if (role != null)
        return Users.Current.IncludedIn(role) || Roles.GetAllUsersInGroup(role).Any(u => IsProcessSubstitute(u, true, processes));

      return false;
    }
    
    /// <summary>
    /// Проверка что текущий сотрудник является замещающим ответственного с учётом стандартных замещений и замещений по процессам.
    /// </summary>
    /// <param name="responsible">Ответственный.</param>
    /// <returns>True, если пользователь является замещающим.</returns>
    [Remote(IsPure = true), Public]
    public bool IsSubsitute(IUser responsible)
    {
      return Substitutions.ActiveSubstitutedUsers.Contains(responsible);
    }
    
    /// <summary>
    /// Зафиксировать результат подписания договорного документа.
    /// </summary>
    /// <param name="documents">Список документов.</param>
    /// <param name="isSigned">Результат подписания: true - подписан, false - не подписан.</param>
    /// <returns>Список документов, для которых не удалось зафиксировать результат подписания.</returns>
    [Remote]
    public static List<Sungero.Contracts.IContractualDocument> ChangeDocSigningOriginalState(List<Sungero.Contracts.IContractualDocument> documents, bool isSigned)
    {
      var notProcessedDocs = new List<Sungero.Contracts.IContractualDocument>();
      var approvalTaskDocumentGroupGuid = Sungero.Docflow.Constants.Module.TaskMainGroup.ApprovalTask;
      
      foreach (var doc in documents)
      {
        var isChange = true;
        var lockInfo = Locks.GetLockInfo(doc);
        if (lockInfo.IsLockedByOther)
        {
          notProcessedDocs.Add(doc);
          isChange = false;
          Logger.DebugFormat(DirRX.Solution.Resources.DocSignIsLockedMessage, doc.Name, lockInfo.LockedMessage);
        }
        else
        {
          var docGuid = doc.GetEntityMetadata().GetOriginal().NameGuid;
          // Найдем задание на организацию подписания оригинала, сформированное по этапу с признаком IsAssignmentOnSigningOriginals
          var assigments = DirRX.Solution.ApprovalCheckingAssignments.GetAll(a => a.Status == Sungero.Workflow.AssignmentBase.Status.InProcess &&
                                                                             a.Task.AttachmentDetails.Any(att => att.AttachmentId == doc.Id &&
                                                                                                          att.EntityTypeGuid == docGuid &&
                                                                                                          att.GroupId == approvalTaskDocumentGroupGuid ) &&
                                                                             a.ApprovalStage.IsAssignmentOnSigningOriginals == true);
          if (assigments.Any())
          {
            var assigment = assigments.FirstOrDefault();
            var lockAsgInfo = Locks.GetLockInfo(assigment);
            if (lockAsgInfo.IsLockedByOther)
            {
              notProcessedDocs.Add(doc);
              isChange = false;
              Logger.DebugFormat(DirRX.Solution.Resources.DocSignAssigmentIsLockedMessage, doc.Name, lockAsgInfo.LockedMessage);
            }
            else
            {
              // Выполним задание с результатом Выполнить/На доработку в зависимости от результата подписания.
              var signResult = isSigned ? DirRX.Solution.ApprovalCheckingAssignment.Result.Accept : DirRX.Solution.ApprovalCheckingAssignment.Result.ForRework;
              // Имитация действия отказать, для прекращения задачи.
              if (!isSigned)
              {
                assigment.Denied = true;
                assigment.ActiveText = DirRX.Solution.Contracts.Resources.NotSign;
              }
              assigment.Complete(signResult);
            }
          }
          else
          {
            // Отправить уведомление, что документ не подписан.
            if (!isSigned)
            {
              var notice = Sungero.Workflow.SimpleTasks.CreateWithNotices(DirRX.Solution.Resources.SubjectNoticeDeniedSign, new[] { doc.ResponsibleEmployee }, new[] { doc });
              notice.Start();
            }
          }
          
          if (isChange)
          {
            ChangeContractualDocSigningOriginalState(doc, isSigned);
            doc.Save();
          }
        }
      }
      
      return notProcessedDocs;
    }
    
    /// <summary>
    /// Выполнить задание по контролю возврата договорного документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="isSigned">Результат подписания: true - подписан, false - не подписан.</param>
    /// <param name="comment">Комментарий.</param>
    /// <returns>Причина невыполнения задания.</returns>
    [Remote]
    public static string ApprovalCheckReturnAssignmentCompleted(Sungero.Contracts.IContractualDocument document, bool isSigned, string comment)
    {
      var approvalTaskDocumentGroupGuid = Sungero.Docflow.Constants.Module.TaskMainGroup.ApprovalTask;
      var docGuid = document.GetEntityMetadata().GetOriginal().NameGuid;
      var result = string.Empty;
      
      AccessRights.AllowRead(
        () =>
        {
          Logger.DebugFormat("Поиск задания в рамках регламента");
          // Найти задание на контроль возврата документа
          var assignments = DirRX.Solution.ApprovalCheckReturnAssignments.GetAll(a => a.Status == Sungero.Workflow.AssignmentBase.Status.InProcess &&
                                                                                 a.Task.AttachmentDetails.Any(att => att.AttachmentId == document.Id &&
                                                                                                              att.EntityTypeGuid == docGuid &&
                                                                                                              att.GroupId == approvalTaskDocumentGroupGuid ));
          if (!assignments.Any())
          {
            Logger.DebugFormat("Задание не найдено");
            var lockInfo = Locks.GetLockInfo(document);
            if (lockInfo.IsLockedByOther)
            {
              Logger.DebugFormat(DirRX.Solution.Resources.DocSignIsLockedMessage, document.Name, lockInfo.LockedMessage);
              result = DirRX.Solution.Resources.DocSignIsLockedMessageFormat(document.Name, lockInfo.LockedMessage);
            }
            else
            {
              try
              {
                Logger.DebugFormat("Смена статуса документа");
                // Поле "Подписание оригиналов к/а" меняется на «Подписан» или «Не подписан».
                if (Contracts.Is(document))
                  Contracts.As(document).ContractorOriginalSigning = isSigned ?
                    Solution.Contract.ContractorOriginalSigning.Signed : Solution.Contract.ContractorOriginalSigning.NotSigned;
                if (SupAgreements.Is(document))
                  SupAgreements.As(document).ContractorOriginalSigning = isSigned ?
                    Solution.SupAgreement.ContractorOriginalSigning.Signed :  Solution.SupAgreement.ContractorOriginalSigning.NotSigned;
                
                // Cтатус согласования с контрагентом меняется на «Подписан» или «Не подписан».
                document.ExternalApprovalState = isSigned ? Sungero.Docflow.OfficialDocument.ExternalApprovalState.Signed : Sungero.Docflow.OfficialDocument.ExternalApprovalState.Unsigned;
                // На закладке «Выдача» в документе фиксируются Результат возврата и Дата возврата.
                var tracking = document.Tracking.Where(r => (r.Action ==  Solution.ContractTracking.Action.Endorsement || r.Action ==  Solution.ContractTracking.Action.OriginalSend) &&
                                                       r.IsOriginal == true && r.ReturnResult == null && r.ReturnDeadline != null && r.ReturnDate == null);
                
                foreach (var row in tracking)
                {
                  // Если комментарий - системный "на согласовании у контрагента", то очистить его, т.к. документ вернулся.
                  if (row.Note == ApprovalTasks.Resources.CommentOnEndorsement)
                    row.Note = null;
                  
                  row.ReturnResult = isSigned ?
                    Sungero.Docflow.OfficialDocumentTracking.ReturnResult.Signed :
                    Sungero.Docflow.OfficialDocumentTracking.ReturnResult.NotSigned;
                  row.ReturnDate = Calendar.GetUserToday(Users.Current);
                }
                document.Save();
                
                // Отправить уведомление, что контрагент не подписал документ.
                if (!isSigned)
                {
                  var notice = Sungero.Workflow.SimpleTasks.CreateWithNotices(DirRX.Solution.Resources.CounterpartyNotSignedFormat(comment), new[] { document.ResponsibleEmployee }, new[] { document });
                  notice.Start();
                }
              }
              catch (Exception ex)
              {
                Logger.DebugFormat(DirRX.Solution.Resources.DocSetSignStateErrorMessage, document.Name, ex.Message + Environment.NewLine, ex.StackTrace);
                result = DirRX.Solution.Resources.DocSetSignStateErrorMessageFormat(document.Name, ex.Message);
              }
            }
          }
          else
          {
            Logger.DebugFormat("Задание найдено");
            var assigment = assignments.FirstOrDefault();
            var lockInfo = Locks.GetLockInfo(assigment);
            if (lockInfo.IsLockedByOther)
            {
              Logger.DebugFormat(DirRX.Solution.Resources.DocSignAssigmentIsLockedMessage, document.Name, lockInfo.LockedMessage);
              result = DirRX.Solution.Resources.DocSignAssigmentIsLockedMessageFormat(document.Name, lockInfo.LockedMessage);
            }
            else
            {
              // Если нашли задание на возврат скан-копии, то установим поле "Подписание оригиналов к/а"
              var task = Solution.ApprovalTasks.As(assigment.Task);
              if (task != null)
              {
                var taskStage = task.ApprovalRule.Stages.Where(s => s.Number == assigment.StageNumber).FirstOrDefault();
                if (taskStage != null)
                {
                  var stage = Solution.ApprovalStages.As(taskStage.Stage);
                  if (stage.KindOfDocumentNeedReturn == Solution.ApprovalStage.KindOfDocumentNeedReturn.CopyScan)
                  {
                    Logger.DebugFormat("Нашли задание на возврат скан-копии - установим поле \"Подписание оригиналов к/а\"");
                    var docLockInfo = Locks.GetLockInfo(document);
                    if (docLockInfo.IsLockedByOther)
                    {
                      Logger.DebugFormat(DirRX.Solution.Resources.DocSignIsLockedMessage, document.Name, docLockInfo.LockedMessage);
                      result = DirRX.Solution.Resources.DocSignIsLockedMessageFormat(document.Name, docLockInfo.LockedMessage);
                    }
                    else
                    {
                      try
                      {
                        Logger.DebugFormat("Смена статуса документа");
                        
                        // Cтатус согласования с контрагентом меняется на «Подписан» или «Не подписан».
                        document.ExternalApprovalState = isSigned ?
                          Sungero.Docflow.OfficialDocument.ExternalApprovalState.Signed : Sungero.Docflow.OfficialDocument.ExternalApprovalState.Unsigned;
                        
                        // Поле "Подписание оригиналов к/а" меняется на «Подписан» или «Не подписан».
                        if (Contracts.Is(document))
                          Contracts.As(document).ContractorOriginalSigning = isSigned ?
                            Solution.Contract.ContractorOriginalSigning.Signed : Solution.Contract.ContractorOriginalSigning.NotSigned;
                        if (SupAgreements.Is(document))
                          SupAgreements.As(document).ContractorOriginalSigning = isSigned ?
                            Solution.SupAgreement.ContractorOriginalSigning.Signed :  Solution.SupAgreement.ContractorOriginalSigning.NotSigned;
                        
                        document.Save();
                      }
                      catch (Exception ex)
                      {
                        Logger.DebugFormat(DirRX.Solution.Resources.DocSetSignStateErrorMessage, document.Name, ex.Message + Environment.NewLine, ex.StackTrace);
                        result = DirRX.Solution.Resources.DocSetSignStateErrorMessageFormat(document.Name, ex.Message);
                      }
                    }
                  }
                }
              }
              
              
              // Выполним задание с результатом Подписан/Не подписан в зависимости от результата подписания.
              var signResult = isSigned ? DirRX.Solution.ApprovalCheckReturnAssignment.Result.Signed : DirRX.Solution.ApprovalCheckReturnAssignment.Result.NotSigned;
              assigment.ActiveText = comment;
              assigment.Complete(signResult);
            }
          }
        });
      
      return result;
    }
    
    /// <summary>
    /// Зафиксировать результат подписания контрагентом договорного документа.
    /// </summary>
    /// <param name="documents">Список документов.</param>
    /// <returns>Список документов, для которых не удалось зафиксировать результат подписания.</returns>
    [Remote]
    public static List<Sungero.Contracts.IContractualDocument> ChangeDocCounterpartySigningOriginalState(List<Sungero.Contracts.IContractualDocument> documents)
    {
      var notProcessedDocs = new List<Sungero.Contracts.IContractualDocument>();
      
      foreach (var doc in documents)
      {
        Logger.DebugFormat("Начало проверки документа");
        var approvalCheckReturnAssignmentCompletedResult = DirRX.Solution.Functions.Module.ApprovalCheckReturnAssignmentCompleted(doc, true, string.Empty);
        if (!String.IsNullOrEmpty(approvalCheckReturnAssignmentCompletedResult))
        {
          notProcessedDocs.Add(doc);
          Logger.DebugFormat(DirRX.Solution.Resources.DocCounterpartySigningErrorMessageFormat(doc.Name, approvalCheckReturnAssignmentCompletedResult));
        }
      }
      return notProcessedDocs;
    }
    
    /// <summary>
    /// Заполнение поля "Подписание оригиналов".
    /// </summary>
    /// <param name="document">Договорной документ (договор или доп. соглашение).</param>
    /// <param name="isSigned">Результат подписания, true - подписан, false - не подписан.</param>
    public static void ChangeContractualDocSigningOriginalState(Sungero.Docflow.IOfficialDocument document, bool isSigned)
    {
      var contract = Solution.Contracts.As(document);
      var supAgreement = Solution.SupAgreements.As(document);
      if (isSigned)
      {
        if (contract != null)
        {
          contract.OriginalSigning = Solution.Contract.OriginalSigning.Signed;
          if (contract.Counterparty != null)
            contract.CounterpartyStatus = contract.Counterparty.CounterpartyStatus;
        }
        
        if (supAgreement != null)
        {
          supAgreement.OriginalSigning = Solution.SupAgreement.OriginalSigning.Signed;
          if (supAgreement.Counterparty != null)
            supAgreement.CounterpartyStatus = supAgreement.Counterparty.CounterpartyStatus;
        }
      }
      else
      {
        if (contract != null)
          contract.OriginalSigning = Solution.Contract.OriginalSigning.NotSigned;
        if (supAgreement != null)
          supAgreement.OriginalSigning = Solution.SupAgreement.OriginalSigning.NotSigned;
      }
    }
    
    /// <summary>
    /// Зафиксировать факт передачи скан-копий на подписание.
    /// </summary>
    /// <param name="documents">Список договоров.</param>
    /// <returns>Список договоров, для которых не удалось зафиксировать факт передачи скан-копий на подписание.</returns>
    [Remote]
    public static List<Sungero.Contracts.IContractualDocument> SetStateOnSigningCopy(List<Sungero.Contracts.IContractualDocument> documents)
    {
      var notProcessedDocs = new List<Sungero.Contracts.IContractualDocument>();
      // В выбранных документах будет зафиксирован факт передачи скан-копий на подписание (значение в области Статус по процессу, группа Движение скан копий - Скан-копия передана на подписание).
      foreach (var doc in documents)
      {
        var lockInfo = Locks.GetLockInfo(doc);
        if (lockInfo.IsLockedByOther)
        {
          notProcessedDocs.Add(doc);
          Logger.DebugFormat(DirRX.Solution.Resources.DocSetSignCopyStateErrorMessage, doc.Name, lockInfo.OwnerName, lockInfo.LockedMessage);
        }
        else
        {
          ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(doc,
                                                                                ContractsCustom.PublicConstants.Module.ContractStatusGuid.ScanSendedBusinessUnitForSigningGuid,
                                                                                ContractsCustom.PublicConstants.Module.ContractStatusType.ScanMoveStatus,
                                                                                false);
          doc.Save();
        }
      }
      return notProcessedDocs;
    }
    
    /// <summary>
    /// Отменить факт передачи скан-копий на подписание.
    /// </summary>
    /// <param name="documents">Список договоров.</param>
    /// <returns>Список договоров, для которых не удалось отменить факт передачи скан-копий на подписание.</returns>
    [Remote]
    public static List<Sungero.Contracts.IContractualDocument> RemoveStateOnSigningCopy(List<Sungero.Contracts.IContractualDocument> documents)
    {
      var notProcessedDocs = new List<Sungero.Contracts.IContractualDocument>();
      // В выбранных документах будет отменен факт передачи скан-копий на подписание (значение в области Статус по процессу, группа Движение скан копий - Скан-копия передана на подписание).
      foreach (var doc in documents)
      {
        var lockInfo = Locks.GetLockInfo(doc);
        if (lockInfo.IsLockedByOther)
        {
          notProcessedDocs.Add(doc);
          Logger.DebugFormat(DirRX.Solution.Resources.DocSetSignCopyRemoveStateErrorMessage, doc.Name, lockInfo.OwnerName, lockInfo.LockedMessage);
        }
        else
        {
          ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(doc,
                                                                                   ContractsCustom.PublicConstants.Module.ContractStatusGuid.ScanSendedBusinessUnitForSigningGuid,
                                                                                   ContractsCustom.PublicConstants.Module.ContractStatusType.ScanMoveStatus);
          doc.Save();
        }
      }
      return notProcessedDocs;
    }
    
    /// <summary>
    /// Зафиксировать результат подписания скан-копий договорных документов.
    /// </summary>
    /// <param name="documents">Список документов.</param>
    /// <param name="isSigned">Результат подписания: true - подписан, false - не подписан.</param>
    /// <returns>Список документов, для которых не удалось зафиксировать результат подписания скан-копии.</returns>
    [Remote]
    public static List<Sungero.Contracts.IContractualDocument> ChangeDocsSigningCopyState(List<Sungero.Contracts.IContractualDocument> documents, bool isSigned)
    {
      var notProcessedDocs = new List<Sungero.Contracts.IContractualDocument>();
      var approvalTaskDocumentGroupGuid = Sungero.Docflow.Constants.Module.TaskMainGroup.ApprovalTask;
      
      foreach (var doc in documents)
      {
        var isChange = true;
        var lockInfo = Locks.GetLockInfo(doc);
        if (lockInfo.IsLockedByOther)
        {
          notProcessedDocs.Add(doc);
          isChange = false;
          Logger.DebugFormat(DirRX.Solution.Resources.DocSetSignedCopyStateErrorMessage, doc.Name, lockInfo.OwnerName, lockInfo.LockedMessage);
        }
        else
        {
          var docGuid = doc.GetEntityMetadata().GetOriginal().NameGuid;
          // Найдем задание на организацию подписания оригинала, сформированное по этапу с признаком IsAssignmentOnSigningScans
          var assigments = DirRX.Solution.ApprovalCheckingAssignments.GetAll(a => a.Status == Sungero.Workflow.AssignmentBase.Status.InProcess &&
                                                                             a.Task.AttachmentDetails.Any(att => att.AttachmentId == doc.Id &&
                                                                                                          att.EntityTypeGuid == docGuid &&
                                                                                                          att.GroupId == approvalTaskDocumentGroupGuid ) &&
                                                                             a.ApprovalStage.IsAssignmentOnSigningScans == true);
          if (assigments.Any())
          {
            var assigment = assigments.FirstOrDefault();
            var lockAsgInfo  = Locks.GetLockInfo(assigment);
            if (lockAsgInfo.IsLockedByOther)
            {
              notProcessedDocs.Add(doc);
              isChange = false;
              Logger.DebugFormat(DirRX.Solution.Resources.DocSignAssigmentIsLockedMessage, doc.Name, lockAsgInfo.OwnerName, lockAsgInfo.LockedMessage);
            }
            else
            {
              // Выполним задание с результатом Выполнить/На доработку в зависимости от результата подписания.
              var signResult = isSigned ? DirRX.Solution.ApprovalCheckingAssignment.Result.Accept : DirRX.Solution.ApprovalCheckingAssignment.Result.ForRework;
              // Имитация действия отказать, для прекращения задачи.
              if (!isSigned)
              {
                assigment.Denied = true;
                assigment.ActiveText = DirRX.Solution.Contracts.Resources.NotSign;
              }
              assigment.Complete(signResult);
            }
          }
          else
          {
            // Отправить уведомление, что документ не подписан.
            if (!isSigned)
            {
              var notice = Sungero.Workflow.SimpleTasks.CreateWithNotices(DirRX.Solution.Resources.SubjectNoticeDeniedSign, new[] { doc.ResponsibleEmployee }, new[] { doc });
              notice.Start();
            }
          }
          
          if (isChange)
          {
            ChangeContractualDocSigningCopyState(doc, isSigned);
            doc.Save();
          }
        }
      }
      return notProcessedDocs;
    }
    
    /// <summary>
    /// Зафиксировать результат подписания скан-копии договорного документа.
    /// </summary>
    /// <param name="document">Договорной документ (договор или доп. соглашение).</param>
    /// <param name="isSigned">Результат подписания, true - подписан, false - не подписан.</param>
    public static void ChangeContractualDocSigningCopyState(Sungero.Docflow.IOfficialDocument document, bool isSigned)
    {
      var contract = Solution.Contracts.As(document);
      var supAgreement = Solution.SupAgreements.As(document);
      if (isSigned)
      {
        ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(Sungero.Contracts.ContractualDocuments.As(document),
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusGuid.ScanSignedByAllSidesGuid,
                                                                              ContractsCustom.PublicConstants.Module.ContractStatusType.ScanMoveStatus,
                                                                              true);
      }
      else
      {
        ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(Sungero.Contracts.ContractualDocuments.As(document),
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusGuid.ScanSendedBusinessUnitForSigningGuid,
                                                                                 ContractsCustom.PublicConstants.Module.ContractStatusType.ScanMoveStatus);
        if (contract != null)
          contract.InternalApprovalState = Solution.Contract.InternalApprovalState.Aborted;
        if (supAgreement != null)
          supAgreement.InternalApprovalState = Solution.SupAgreement.InternalApprovalState.Aborted;
      }
    }
    
    /// <summary>
    /// Получить значение константы "Срок задачи обеспечения возврата оригиналов".
    /// </summary>
    /// <returns></returns>
    [Public, Remote(IsPure = true)]
    public static int GetDeadlineConstantValue()
    {
      var deadline = 0;
      var deadlineConstant = DirRX.ContractsCustom.ContractConstants.GetAll(c => c.Sid == DirRX.ContractsCustom.PublicConstants.Module.OriginalsControlTaskDeadlineConstantGuid.ToString()).FirstOrDefault();
      if (deadlineConstant == null || !deadlineConstant.Period.HasValue || deadlineConstant.Unit != DirRX.ContractsCustom.ContractConstant.Unit.Day)
        Logger.Error("ApprovalSendingAssigmentsComplete: Не заполнена константа \"Срок задачи обеспечения возврата оригиналов\", или в константе указана единица измерения не равная \"Дни\"");
      else
        deadline = deadlineConstant.Period.Value;
      return deadline;
    }
  }
}