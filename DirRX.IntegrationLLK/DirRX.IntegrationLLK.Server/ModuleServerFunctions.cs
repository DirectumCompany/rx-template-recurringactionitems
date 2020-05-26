using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Domain.Shared;
using Sungero.Workflow;

namespace DirRX.IntegrationLLK.Server
{
  public class ModuleFunctions
  {
    
    /// <summary>
    /// Получить роль ответственных за контрагентов.
    /// </summary>
    /// <returns>Роль ответственных за контрагентов.</returns>
    [Remote(IsPure = true), Public]
    public static IRole GetCounterpartiesResponsiblesRole()
    {
      return Roles.GetAll().SingleOrDefault(r => r.Sid == Constants.Module.RoleGuid.CounterpartiesResponsibleRole);
    }

    #region Работа с конфигурационным файлом.
    
    /// <summary>
    /// Получить значение из серверного конфигурационного файла по имени элемента.
    /// </summary>
    /// <param name="elementName">Имя элемента.</param>
    /// <returns>Значение элемента.</returns>
    [Remote, Public]
    public string GetServerConfigValue(string elementName)
    {
      // TODO: При запуске фонового процесса проверять наличие файла.
      var customConfigSettingsName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_CustomConfigSettings.xml");
      var xd = new XmlDocument();
      xd.Load(customConfigSettingsName);
      
      try
      {
        foreach (XmlNode node in xd.DocumentElement.ChildNodes)
        {
          if (node.Attributes["name"].Value == elementName)
            return node.Attributes["value"].Value;
        }
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Дополнительный конфигурационный файл имеет некорректный формат", ex);
      }
      
      return string.Empty;
    }
    
    /// <summary>
    /// Получить список значений из серверного конфигурационного файла по имени элемента.
    /// </summary>
    /// <param name="xpath">XPath селектор.</param>
    /// <returns>Список значений элемента.</returns>
    [Remote, Public]
    public List<string> GetServerConfigValues(string xpath)
    {
      // TODO: При запуске фонового процесса проверять наличие файла.
      var customConfigSettingsName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_CustomConfigSettings.xml");
      
      var xd = new XmlDocument();
      xd.Load(customConfigSettingsName);
      
      try
      {
        List<string> values = new List<string>();
        foreach (XmlNode node in xd.DocumentElement.SelectNodes(xpath))
          values.Add(node.Attributes["value"].Value);
        
        return values;
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Дополнительный конфигурационный файл имеет некорректный формат", ex);
      }

      return new List<string>();
    }
    
    #endregion
    
    #region Форматирование текста.
    
    /// <summary>
    /// Добавить строку текста уведомления.
    /// </summary>
    /// <param name="task">Текст уведомления.</param>
    /// <param name="line">Сообщение.</param>
    public static string NoticeTextAddLine(string activeText, string line)
    {
      if (string.IsNullOrEmpty(line))
        return activeText + Environment.NewLine;
      
      return activeText + line + Environment.NewLine;
    }
    
    /// <summary>
    /// Добавить составную часть строки через запятую.
    /// </summary>
    /// <param name="full">Полная строка.</param>
    /// <param name="part">Составная часть.</param>
    /// <returns>Объединенная строка.</returns>
    public string AddStringPart(string full, string part)
    {
      if (!string.IsNullOrEmpty(part) && part.Trim() != "NULL")
        return string.IsNullOrEmpty(full) ? part : string.Format("{0}, {1}", full, part);
      
      return full;
    }
    
    #endregion
    
    #region Уведомления по результатам интеграции.
    
    /// <summary>
    /// Отправить уведомление администратору о результатах интеграции.
    /// </summary>
    /// <param name="subject">Тема уведомления.</param>
    /// <param name="activeText">Текст уведомления.</param>
    public static void SendImportResultsNotice(string subject, string activeText)
    {
      string roleName = PublicFunctions.Module.Remote.GetServerConfigValue("EXCHANGE_RESULTS_ROLE");
      var defaultRoleGuid = Constants.Module.SynchronizationResponsibleRoleGuid;
      var role = string.IsNullOrEmpty(roleName) ? Roles.GetAll(x => x.Sid == defaultRoleGuid).FirstOrDefault() : Roles.GetAll(x => x.Name == roleName).FirstOrDefault();
      if (role != null)
      {
        var task = Sungero.Workflow.SimpleTasks.Create();
        task.Subject = subject;
        var step = task.RouteSteps.AddNew();
        step.AssignmentType = Sungero.Workflow.SimpleTask.AssignmentType.Notice;
        step.Performer = role;
        task.ActiveText = activeText;
        task.Start();
      }
    }
    
    /// <summary>
    /// Отправить уведомление руководителю о назначении замещения.
    /// </summary>
    public static void SendSubstitutionNotice(IAbsenceOfEmployee absence)
    {
      var employee = absence.Employee;
      var role = ActionItems.ActionItemsRoles.GetAll(r => r.Type == ActionItems.ActionItemsRole.Type.InitManager).FirstOrDefault();
      var manager = ActionItems.PublicFunctions.ActionItemsRole.Remote.GetRolePerformer(role, employee);
      
      if (manager != null)
      {
        var task = Sungero.Workflow.SimpleTasks.Create();
        task.Subject = Resources.AbsenceOfEmployeeSubstitutionSubjectFormat(employee.Name);
        var step = task.RouteSteps.AddNew();
        step.AssignmentType = Sungero.Workflow.SimpleTask.AssignmentType.Notice;
        step.Performer = manager;
        
        string activeText = string.Empty;
        activeText = NoticeTextAddLine(activeText, Resources.AbsenceOfEmployeeSubstitutionSubjectFormat(Sungero.Core.Hyperlinks.Get(employee)));
        task.ActiveText = activeText;
        task.Attachments.Add(absence);
        task.Start();
      }
    }
    
    #endregion
    
    #region Работа с внешними ссылками.
    
    /// <summary>
    /// Создать внешнюю ссылку.
    /// </summary>
    /// <param name="entityId">Идентификатор сущности DirectumRX.</param>
    /// <param name="entityType">Идентификатор типа сущности DirectumRX.</param>
    /// <param name="extEntityId">Идентификатор сущности внешней системы.</param>
    /// <param name="extEntityType">Тип сущности внешней системы.</param>
    /// <param name="extSystemId">Идентификатор внешней системы.</param>
    [Public]
    public static void SetExternalEntityLink(int entityId, string entityType, string extEntityId, string extEntityType, string extSystemId)
    {
      var extLink = Sungero.Commons.ExternalEntityLinks.GetAll(e => e.EntityId == entityId &&
                                                               e.EntityType.ToUpper() == entityType.ToUpper() &&
                                                               e.ExtEntityId == extEntityId &&
                                                               e.ExtEntityType == extEntityType).FirstOrDefault();
      if (extLink == null)
      {
        extLink = Sungero.Commons.ExternalEntityLinks.Create();
        extLink.EntityId = entityId;
        extLink.EntityType = entityType;
        extLink.ExtEntityId = extEntityId;
        extLink.ExtEntityType = extEntityType;
        extLink.ExtSystemId = extSystemId;
      }
      
      extLink.SyncDate = Calendar.Now;
      extLink.Save();
    }
    
    /// <summary>
    /// Получить внешнюю ссылку.
    /// </summary>
    /// <param name="typeGuid">GUID типа сущности.</param>
    /// <param name="entityGuid">GUID сущности.</param>
    /// <param name="extSystemId">Идентификатор внешней системы.</param>
    /// <returns>Внешняя ссылка.</returns>
    [Public]
    public static Sungero.Commons.IExternalEntityLink GetExternalLink(string typeGuid, string entityGuid, string extSystemId)
    {
      return Sungero.Commons.ExternalEntityLinks.GetAll(e => e.EntityType.ToUpper() == typeGuid.ToUpper() &&
                                                        e.ExtEntityId == entityGuid &&
                                                        e.ExtSystemId == extSystemId).FirstOrDefault();
    }
    
    /// <summary>
    /// Получить нашу организацию по внешнему GUID.
    /// </summary>
    /// <param name="entityGuid">GUID нашей организации.</param>
    /// <returns>Наша организация.</returns>
    [Public]
    public static DirRX.Solution.IBusinessUnit GetBusinessUnitByExtGUID(string entityGuid)
    {
      string businessUnitTypeGuid = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(typeof(DirRX.Solution.IBusinessUnit)).NameGuid.ToString();
      
      var extLink = GetExternalLink(businessUnitTypeGuid, entityGuid, Constants.Module.SSPDSystemCode);
      if (extLink != null)
        return DirRX.Solution.BusinessUnits.GetAll(d => d.Id == extLink.EntityId).FirstOrDefault();
      
      return null;
    }
    
    /// <summary>
    /// Получить организацию по внешнему GUID.
    /// </summary>
    /// <param name="entityGuid">GUID организации.</param>
    /// <returns>Организация.</returns>
    [Public]
    public static DirRX.Solution.ICompany GetOrganizationByExtGUID(string entityGuid)
    {
      string companyTypeGuid = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(typeof(DirRX.Solution.ICompany)).NameGuid.ToString();
      
      var extLink = GetExternalLink(companyTypeGuid, entityGuid, Constants.Module.SSPDSystemCode);
      if (extLink != null)
        return DirRX.Solution.Companies.GetAll(d => d.Id == extLink.EntityId).FirstOrDefault();
      
      return null;
    }
    
    /// <summary>
    /// Получить подразделение по внешнему GUID.
    /// </summary>
    /// <param name="entityGuid">GUID подразделения.</param>
    /// <returns>Подразделение.</returns>
    [Public]
    public static DirRX.Solution.IDepartment GetDepartmentByExtGUID(string entityGuid)
    {
      string departmentTypeGuid = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(typeof(DirRX.Solution.IDepartment)).NameGuid.ToString();
      
      var extLink = GetExternalLink(departmentTypeGuid, entityGuid, Constants.Module.SSPDSystemCode);
      if (extLink != null)
        return DirRX.Solution.Departments.GetAll(d => d.Id == extLink.EntityId).FirstOrDefault();
      
      return null;
    }
    
    /// <summary>
    /// Получить подразделение организации по внешнему GUID.
    /// </summary>
    /// <param name="entityGuid">GUID подразделения организации.</param>
    /// <returns>Подразделение организации.</returns>
    [Public]
    public static IDepartCompanies GetOrgDepartmentByExtGUID(string entityGuid)
    {
      string orgDepartmentTypeGuid = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(typeof(IDepartCompanies)).NameGuid.ToString();
      
      var extLink = GetExternalLink(orgDepartmentTypeGuid, entityGuid, Constants.Module.SSPDSystemCode);
      if (extLink != null)
        return DepartCompanieses.GetAll(d => d.Id == extLink.EntityId).FirstOrDefault();
      
      return null;
    }
    
    /// <summary>
    /// Получить сотрудника по внешнему GUID.
    /// </summary>
    /// <param name="entityGuid">GUID сотрудника.</param>
    /// <returns>Сотрудник.</returns>
    [Public]
    public static DirRX.Solution.IEmployee GetEmployeeByExtGUID(string entityGuid)
    {
      string employeeTypeGuid = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(typeof(DirRX.Solution.IEmployee)).NameGuid.ToString();
      
      var extLink = GetExternalLink(employeeTypeGuid, entityGuid, Constants.Module.SSPDSystemCode);
      if (extLink != null)
        return DirRX.Solution.Employees.GetAll(d => d.Id == extLink.EntityId).FirstOrDefault();
      
      return null;
    }
    
    /// <summary>
    /// Получить контакт по внешнему GUID.
    /// </summary>
    /// <param name="entityGuid">GUID контакта.</param>
    /// <returns>Контакт.</returns>
    [Public]
    public static DirRX.Solution.IContact GetContactByExtGUID(string entityGuid)
    {
      string contactTypeGuid = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(typeof(DirRX.Solution.IContact)).NameGuid.ToString();
      
      var extLink = GetExternalLink(contactTypeGuid, entityGuid, Constants.Module.SSPDSystemCode);
      if (extLink != null)
        return DirRX.Solution.Contacts.GetAll(d => d.Id == extLink.EntityId).FirstOrDefault();
      
      return null;
    }

    #endregion
    
    [Public]
    public static Sungero.Docflow.IDocumentKind GetDocumentKind(Guid documentKindEntityGuid)
    {
      var externalLink = Sungero.Docflow.PublicFunctions.Module.GetExternalLink(Constants.Module.DocumentKindTypeGuid, documentKindEntityGuid);
      
      return Sungero.Docflow.DocumentKinds.GetAll().Where(x => x.Id == externalLink.EntityId).FirstOrDefault();
    }
    
    /// <summary>
    /// Возвращает документ по указанному идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор документа.</param>
    /// <returns>Документ.</returns>
    [Remote]
    public static Sungero.Docflow.IOfficialDocument LocateDocumentById(int id)
    {
      return Sungero.Docflow.OfficialDocuments.GetAll().Where(d => d.Id == id).FirstOrDefault();
    }
    
    /// <summary>
    /// Отправляет задачи с вложениями в соответствии с настроенными параметрами отправки.
    /// </summary>
    /// <param name="attachment">Вложение.</param>
    [Remote]
    public void ExecuteTasks(IEntity attachment)
    {
      var doc = Sungero.Docflow.OfficialDocuments.As(attachment);
      if (doc == null)
        throw new ArgumentException(DirRX.IntegrationLLK.Resources.IncorrectTypeOfCreatedEntity, "attachment");
      
      // Получение списка задач отправленных на согласование по регламенту во вложении которого есть данный документ. Key - задача, Value - списко исполнителей связанных заданий по этапам.
      var baseAssignment = GetApprovalAssignment(doc);
      
      if (baseAssignment == null)
        return;
      
      // Если задания по регламенту существуют, то выполнить их.
      if (baseAssignment != null)
      {
        var checkReturAssignment = ApprovalCheckReturnAssignments.As(baseAssignment);
        if (checkReturAssignment != null)
          checkReturAssignment.Complete(Sungero.Docflow.ApprovalCheckReturnAssignment.Result.Signed);
        else
          ApprovalCheckingAssignments.As(baseAssignment).Complete(Sungero.Docflow.ApprovalCheckingAssignment.Result.Accept);
      }
      else
      {
        Logger.Debug("Не найдены активные задачи на согласование по регламенту.");
      }
    }
    
    /// <summary>
    /// Возвращает словарь с задачами по согласованию с регламентом для указанного документа.
    /// </summary>
    /// <param name="doc">Документ для которого ищутся задачи.</param>
    /// <returns>Словарь с задачами по согласованию с регламентом для указанного документа. Key - задача с согласованием по регламенту, Value - список исполнителей связанных заданий по этапам.</returns>
    private IAssignmentBase GetApprovalAssignment(IOfficialDocument doc)
    {
      // Поиск задач по регламенту со статусом в работе.
      var approvalTasks = DirRX.Solution.ApprovalTasks.GetAll().Where(c => c.Status == Sungero.Workflow.Task.Status.InProcess);
      var assignments = new List<IAssignmentBase>();
      foreach (var task in approvalTasks)
      {
        // Если во вложениях к задаче есть нужный документ, то попытаться определить текущее задание.
        if (task.DocumentGroup.All.Select(c => OfficialDocuments.As(c)).Any(c => c != null && OfficialDocuments.Equals(doc, c)))
        {
          // Поиск связанных с задачей заданий по этапу "Контроль возврата" и "Задание с возможностью отправки на доработку".
          var сheckingStage =  DirRX.Solution.PublicFunctions.ApprovalTask.GetStage(task, Sungero.Docflow.ApprovalStage.StageType.SimpleAgr);
          if (сheckingStage != null)
            assignments = AssignmentBases.GetAll().Where(c => Tasks.Equals(c.Task, task) && ApprovalCheckingAssignments.Is(c) && сheckingStage.IsAssignmentOnSigningOriginals == true).ToList();
          else
            assignments = AssignmentBases.GetAll().Where(c => Tasks.Equals(c.Task, task) && ApprovalCheckReturnAssignments.Is(c)).ToList();
        }
      }
      return assignments.FirstOrDefault();
    }
  }
}