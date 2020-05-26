using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.RevisionRequest;

namespace DirRX.PartiesControl.Server
{
  partial class RevisionRequestFunctions
  {
    /// <summary>
    /// Проверить, что пользователь входит в роль.
    /// </summary>
    /// <param name="user">Пользователь.</param>
    /// <param name="roleGuid">Guid роли.</param>
    /// <returns>Признак, что пользователь входит в роль.</returns>
    [Remote]
    public static bool CheckUserInRole(IUser user, Guid roleGuid)
    {
      var result = false;
      var role = Roles.GetAll().Where(r => r.Sid == roleGuid).FirstOrDefault();
      if (role != null)
        result = role.RecipientLinks.Where(l => Users.Equals(l.Member, user)).Any();
      
      return result;
    }
    
    /// <summary>
    /// Отправить запрос недостающих документов.
    /// </summary>
    /// <returns>Признак, что задача отправлена.</returns>
    [Remote(PackResultEntityEagerly = true)]
    public Sungero.Workflow.ISimpleTask SendRequestMissingDocuments()
    {
      var activeText = new StringBuilder();
      
      foreach (var row in _obj.BindingDocuments.Where(d => d.Received != true))
        activeText.AppendLine(string.Format(" - {0}", row.Document != null ? row.Document.DisplayValue : row.DocumentKind));
      
      var task = Sungero.Workflow.SimpleTasks.Create(DirRX.PartiesControl.RevisionRequests.Resources.RequestMissingDocumentsSubjectFormat(_obj.DisplayValue),
                                                     Calendar.UserToday.AddMonths(1), new [] {_obj.PreparedBy}, new [] {_obj});
      task.ActiveText = activeText.ToString();
      return task;
    }

    /// <summary>
    /// Заполнить документы.
    /// </summary>
    /// <returns>Признак, что область Обязательные документы изменена.</returns>
    [Remote]
    public bool AddDoucments()
    {
      var result = true;
      var previusRevision = RevisionRequests.GetAll()
        .Where(r => DirRX.Solution.Companies.Equals(r.Counterparty, _obj.Counterparty))
        .Where(r => !DirRX.PartiesControl.RevisionRequests.Equals(r, _obj))
        .OrderByDescending(r => r.CheckingDate.Value)
        .FirstOrDefault();
      if (previusRevision != null)
      {
        foreach (var row in previusRevision.BindingDocuments)
        {
          var modifiableRow = _obj.BindingDocuments.Where(r => r.DocumentKind == row.DocumentKind).FirstOrDefault();
          if (modifiableRow != null)
          {
            modifiableRow.DocumentKind = row.DocumentKind;
            modifiableRow.Document = row.Document;
          }
        }
      }
      else
        result = false;
      
      return result;
    }

    /// <summary>
    /// Создать заявку на проверку контрагента от основного документа.
    /// </summary>
    /// <param name="document">Основной договорной документ.</param>
    /// <returns>Заявка на проверку контрагента.</returns>
    [Public, Remote]
    public static IRevisionRequest CreateRevisionRequest(Sungero.Contracts.IContractualDocument document)
    {
      var counterparty = DirRX.Solution.Companies.As(document.Counterparty);
      if (counterparty == null)
        return null;
      
      var supervisor = DirRX.Solution.Employees.Null;
      
      var memoForPayment = ContractsCustom.MemoForPayments.As(document);
      if (memoForPayment != null)
        supervisor = memoForPayment.Supervisor;
      
      var request = CreateRequest(counterparty, document, null, supervisor);
      return request;
    }
    
    /// <summary>
    /// Создать заявки на проверку контрагента от основного документа.
    /// </summary>
    /// <param name="document">Основной договорной документ.</param>
    /// <returns>Список заявок на проверку контрагента.</returns>
    [Public, Remote]
    public static List<IRevisionRequest> CreateRevisionRequests(Sungero.Contracts.IContractualDocument document, List<Solution.ICompany> createRevisionRequestCounterparties)
    {
      var requests = new List<IRevisionRequest>();
      
      var category = DirRX.Solution.ContractCategories.Null;
      var supervisor = DirRX.Solution.Employees.Null;
      
      var contract = DirRX.Solution.Contracts.As(document);
      if (contract != null)
      {
        category = contract.DocumentGroup;
        if (contract.Supervisor != null)
          supervisor = contract.Supervisor;
        else if (category != null && category.Supervisor != null)
        {
          var roleRecipients = category.Supervisor.RecipientLinks.FirstOrDefault(x => x.Member != null && DirRX.Solution.Employees.Is(x.Member));
          if (roleRecipients != null)
            supervisor = DirRX.Solution.Employees.As(roleRecipients.Member);
        }
      }
      
      var supAgreement = DirRX.Solution.SupAgreements.As(document);
      if (supAgreement != null)
      {
        category = DirRX.Solution.Contracts.As(supAgreement.LeadingDocument).DocumentGroup;
        if (supAgreement.Supervisor != null)
          supervisor = supAgreement.Supervisor;
        else if (category != null && category.Supervisor != null)
        {
          var roleRecipients = category.Supervisor.RecipientLinks.FirstOrDefault(x => x.Member != null && DirRX.Solution.Employees.Is(x.Member));
          if (roleRecipients != null)
            supervisor = DirRX.Solution.Employees.As(roleRecipients.Member);
        }
      }
      // Для каждого контрагента создадим заявку.
      foreach (var counterparty in createRevisionRequestCounterparties)
      {
        var request = CreateRequest(counterparty, document, category, supervisor);
        requests.Add(request);
      }
      return requests;
    }
    
    /// <summary>
    /// Проверка возможности одобрения контрагента.
    /// </summary>
    /// <returns></returns>
    [Remote(IsPure = true)]
    public bool CanApproveCounterparty(int assignmentID)
    {
      var checkingAssignment = DirRX.Solution.ApprovalCheckingAssignments.GetAll(a => a.Id == CallContext.GetCallerEntityId(DirRX.Solution.ApprovalCheckingAssignments.Info)).FirstOrDefault();
      if (checkingAssignment != null)
      {
        return checkingAssignment.ApprovalStage != null && checkingAssignment.ApprovalStage.NeedCounterpartyApproval.GetValueOrDefault() &&
          (Sungero.CoreEntities.Users.Equals(Users.Current, checkingAssignment.Performer) ||
           Solution.PublicFunctions.Module.Remote.IsProcessSubstitute(checkingAssignment.Performer, true,
                                                                      new List<Enumeration>() { DirRX.ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.Others }));
      }
      
      return false;
    }
    
    /// <summary>
    /// Получить заявку по id.
    /// </summary>
    /// <param name="id">id.</param>
    /// <returns>Заявка.</returns>
    [Remote(IsPure = true), Public]
    public static IRevisionRequest GetRevisionRequest(int id)
    {
      return RevisionRequests.Get(id);
    }
    
    /// <summary>
    /// Добавить анкету контрагента в заявку на проверку.
    /// </summary>
    /// <param name="request">Заявка.</param>
    [Public]
    public static void AddCounterpartyInformation(IRevisionRequest request)
    {
      var counterpartyInformation = Sungero.Docflow.CounterpartyDocuments.Create();
      counterpartyInformation.Counterparty = request.Counterparty;
      counterpartyInformation.DocumentKind = IntegrationLLK.PublicFunctions.Module.GetDocumentKind(PartiesControl.Constants.Module.CounterpartyInformationKind);
      counterpartyInformation.Subject = RevisionRequests.Resources.CounterpartyDocumentSubject;
      counterpartyInformation.AccessRights.Grant(Roles.GetAll(r => r.Sid == Constants.Module.SecurityServiceRole).FirstOrDefault(), DefaultAccessRightsTypes.FullAccess);
      counterpartyInformation.Save();
      
      var securityServiceDocumentsItem = request.SecurityServiceDocuments.AddNew();
      securityServiceDocumentsItem.Document = counterpartyInformation;
      securityServiceDocumentsItem.DocumentKind = DirRX.PartiesControl.Resources.CounterpartyInformationKind;
      securityServiceDocumentsItem.Format = DirRX.PartiesControl.RevisionRequestSecurityServiceDocuments.Format.Copy;
      securityServiceDocumentsItem.Comment = RevisionRequests.Resources.SecurityServiceDocumentsItemComment;
    }
    
    /// <summary>
    /// Создать заявку на проверку контрагента.
    /// </summary>
    /// <param name="counterparty">Контрагент.</param>
    /// <param name="document">Документ-основание.</param>
    /// <param name="category">Категория.</param>
    /// <param name="supervisor">Куратор.</param>
    /// <returns>Созданная заявка на проверку контрагента.</returns>
    public static IRevisionRequest CreateRequest(Solution.ICompany counterparty, Sungero.Contracts.IContractualDocument document,
                                                 Solution.IContractCategory category, Solution.IEmployee supervisor)
    {
      var request = RevisionRequests.Create();
      
      request.Counterparty = counterparty;
      request.MainDocument = Sungero.Contracts.ContractualDocuments.As(document);
      if (category != null)
        request.CheckingReason = category.CheckingReason;
      request.Supervisor = supervisor;
      request.PreparedBy = DirRX.Solution.Employees.Current;
      
      AddCounterpartyInformation(request);
      
      request.Save();
      
      return request;
    }
  }
}