using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PartiesControl.Server
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Получение списка всех документов контрагентов с версиями.
    /// </summary>
    [Remote]
    public IQueryable<DirRX.Solution.ICounterpartyDocument> GetAllCounterpartyDoc()
    {
      return DirRX.Solution.CounterpartyDocuments.GetAll().Where(d => d.HasVersions);
    }
    
    /// <summary>
    /// Удаление версии документа.
    /// </summary>
    [Remote]
    public void ClearVerCounterpartyDoc(List<DirRX.Solution.ICounterpartyDocument> docs)
    {
      foreach (var doc in docs)
      {
        //var vers = doc.Versions;
        //foreach (var ver in vers)
        doc.DeleteVersion(doc.Versions.FirstOrDefault());
        doc.Save();
      }
    }
    
    

    /// <summary>
    /// Данные для отчета по проверкам СБ.
    /// </summary>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public static List<Structures.SecurityReport.SecurityReportTableLine> GetSecurityReportData(DateTime periodFrom, DateTime periodTo, DirRX.Solution.ICompany counterparty, ICheckingResult checkingResult)
    {
      var result = new List<Structures.SecurityReport.SecurityReportTableLine>();

      var requests = RevisionRequests.GetAll(r => r.CheckingDate.HasValue &&
                                             r.CheckingDate >= periodFrom &&
                                             r.CheckingDate <= periodTo);

      if (counterparty != null)
        requests = requests.Where(r => DirRX.Solution.Companies.Equals(r.Counterparty, counterparty));
      
      if (checkingResult != null)
        requests = requests.Where(r => CheckingResults.Equals(r.CheckingResult, checkingResult));
      
      foreach (var request in requests)
      {
        if (request.Counterparty == null)
          continue;
        
        var line = new Structures.SecurityReport.SecurityReportTableLine();
        line.RequestId = request.Id;
        line.CounterpartyName = request.Counterparty.Name;
        line.CounterpartyReqs = GetCounterpartyRequisites(request.Counterparty);
        line.MainDocumentName = request.MainDocument == null ? "-" : request.MainDocument.Name;
        line.CheckingDate = request.CheckingDate.HasValue ? request.CheckingDate.Value.ToShortDateString() : string.Empty;
        line.CheckingResult = request.CheckingResult == null ? string.Empty : request.CheckingResult.Name;
        line.Comment = request.SecurityComment;
        line.Note = request.Note;
        result.Add(line);
      }
      
      return result;
    }

    /// <summary>
    /// Сформировать строку с реквизитами контрагента для отчета.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    [Public]
    public static string GetCounterpartyRequisites(DirRX.Solution.ICompany counterparty)
    {
      string requisites = string.Empty;
      
      if (!string.IsNullOrEmpty(counterparty.TIN))
        requisites += string.Format("ИНН:{0};{1}", counterparty.TIN, Environment.NewLine);
      if (!string.IsNullOrEmpty(counterparty.TRRC))
        requisites += string.Format("КПП:{0};{1}", counterparty.TRRC, Environment.NewLine);
      if (!string.IsNullOrEmpty(counterparty.PSRN))
        requisites += string.Format("ОГРН:{0};{1}", counterparty.PSRN, Environment.NewLine);
      if (!string.IsNullOrEmpty(counterparty.NCEO))
        requisites += string.Format("ОКПО:{0};{1}", counterparty.NCEO, Environment.NewLine);
      
      return requisites.Trim();
    }
    

    /// <summary>
    /// Проверить решение результата проверки контрагента.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <returns></returns>
    [Public, Remote(IsPure = true)]
    public bool IsCounterpartyResultApproved(DirRX.Solution.ICompany counterparty)
    {
      var checkingResult = counterparty.CheckingResult;
      if (checkingResult != null)
        return checkingResult.Decision == PartiesControl.CheckingResult.Decision.Approved;
      
      return false;
    }
    
    /// <summary>
    /// Данные для специализированного отчета ГД.
    /// </summary>
    /// <returns>Данные для отчета.</returns>
    [Public]
    public static List<Structures.DocumentControlReport.DocumentControlReportTableLine> GetDocumentControlReportData()
    {
      var result = new List<Structures.DocumentControlReport.DocumentControlReportTableLine>();
      
      var requests = RevisionRequests.GetAll(r => r.CheckingDate.HasValue && r.AllDocsReceived == false);
      
      foreach (var request in requests.OrderBy(r => r.CheckingDate))
      {
        var receivedDate = request.CheckingDate.Value.AddWorkingDays(request.PreparedBy, 30);
        if (receivedDate < Calendar.Today)
        {
          // Список непредоставленных документов.
          var documents = request.BindingDocuments.Where(d => d.Document != null &&
                                                         d.Format == DirRX.PartiesControl.RevisionRequestBindingDocuments.Format.Original &&
                                                         (d.Sent != true || d.Received != true))
            .Select(d => d.DocumentKind).ToList();
          
          documents.AddRange(request.SecurityServiceDocuments.Where(d => d.Document != null &&
                                                                    d.Format == DirRX.PartiesControl.RevisionRequestSecurityServiceDocuments.Format.Original &&
                                                                    (d.Sent != true || d.Received != true))
                             .Select(d => d.DocumentKind).ToList());
          
          if (documents.Any())
          {
            var line = new Structures.DocumentControlReport.DocumentControlReportTableLine();
            line.Responsible = request.PreparedBy != null ?
              string.Format("{0}{1}{2}", Sungero.Company.PublicFunctions.Employee.GetShortName(request.PreparedBy, false), Environment.NewLine, request.PreparedBy.JobTitle.DisplayValue) :
              string.Empty;
            line.DepartmentResponsible = request.Department != null ? request.Department.DisplayValue : string.Empty;
            line.Supervisor = string.Format("{0}{1}{2}", Sungero.Company.PublicFunctions.Employee.GetShortName(request.Supervisor, false), Environment.NewLine, request.Supervisor.JobTitle.DisplayValue);
            line.Counterparty = request.Counterparty.DisplayValue;
            line.Date = receivedDate.ToShortDateString();
            line.Days = WorkingTime.GetDurationInWorkingDays(receivedDate, Calendar.Today, request.PreparedBy).ToString();
            line.Documents = string.Join("; ", documents);
            
            result.Add(line);
          }
        }
      }
      
      return result;
    }
    
    /// <summary>
    /// Создать роль.
    /// </summary>
    /// <param name="roleName">Название роли.</param>
    /// <param name="roleDescription">Описание роли.</param>
    /// <param name="roleGuid">Guid роли. Игнорирует имя. // CORE: использование System.Guid.</param>
    /// <returns>Новая роль.</returns>
    [Public]
    public static IRole CreateSingleRole(string roleName, string roleDescription, Guid roleGuid)
    {
      Logger.DebugFormat("Init: Create Role {0}", roleName);
      var role = Roles.GetAll(r => r.Sid == roleGuid).FirstOrDefault();
      
      if (role == null)
      {
        role = Roles.Create();
        role.Name = roleName;
        role.Description = roleDescription;
        role.Sid = roleGuid;
        role.IsSystem = true;
        role.IsSingleUser = true;
        role.RecipientLinks.AddNew().Member = Users.Current;
        role.Save();
      }
      else
      {
        if (role.Name != roleName)
        {
          Logger.DebugFormat("Role '{0}'(Sid = {1}) renamed as '{2}'", role.Name, role.Sid, roleName);
          role.Name = roleName;
          role.Save();
        }
        if (role.Description != roleDescription)
        {
          Logger.DebugFormat("Role '{0}'(Sid = {1}) update Description '{2}'", role.Name, role.Sid, roleDescription);
          role.Description = roleDescription;
          role.Save();
        }
        if (role.IsSingleUser == false)
        {
          role.IsSingleUser = true;
          role.RecipientLinks.Clear();
          role.RecipientLinks.AddNew().Member = Users.Current;
          role.Save();
        }
      }
      return role;
    }
    
    /// <summary>
    /// Получить роль сотрудника архива.
    /// </summary>
    /// <returns>Роль.</returns>
    [Remote(IsPure = true), Public]
    public static IRole GetArchiveResponsibleRole()
    {
      return Roles.GetAll(r => r.Sid == Constants.Module.ArchiveResponsibleRole).FirstOrDefault();
    }
    
    /// <summary>
    /// Формирование уведомления с отчётом для ГД.
    /// </summary>
    /// <returns>Сформированное уведомление.</returns>
    [Remote(IsPure = true), Public]
    public static Sungero.Workflow.ISimpleTask CreateDocumentControlTask()
    {
      var CEO = Sungero.Company.BusinessUnits.GetAll().FirstOrDefault(u => u.HeadCompany == null).CEO;
      
      if (PublicFunctions.Module.GetDocumentControlReportData().Any())
      {
        var report = DirRX.PartiesControl.Reports.GetDocumentControlReport();
        if (report == null)
          return null;
        
        var internalReport = (Sungero.Reporting.Shared.IInternalReport)report;
        if (report == null)
          return null;
        
        var reportStream = new System.IO.MemoryStream();
        internalReport.InternalExecute(reportStream);
        
        var document = Sungero.Docflow.SimpleDocuments.Create();
        document.Name = Reports.Resources.DocumentControlReport.ReportTitleFormat(Calendar.Today.ToShortDateString());
        document.CreateVersion();
        var documentVersion = document.LastVersion;
        documentVersion.Body.Write(reportStream);
        documentVersion.AssociatedApplication = Sungero.Content.AssociatedApplications.GetByExtension("pdf");
        document.Save();
        
        var assignee = Roles.GetAll(r => r.Sid == PartiesControl.PublicConstants.Module.CEOReportAssigneeRole).FirstOrDefault();
        var performer = assignee != null ? Recipients.As(assignee) : CEO;
        
        var task = Sungero.Workflow.SimpleTasks.CreateWithNotices(Resources.DocumentControlTaskName, new[] { performer }, new[] { document });
        
        return task;
      }
      
      return Sungero.Workflow.SimpleTasks.CreateWithNotices(Resources.CEONoticeTitle, new[] { CEO });
    }
  }
}