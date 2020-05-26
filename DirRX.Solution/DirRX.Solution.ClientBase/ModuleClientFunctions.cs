using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Newtonsoft.Json;
using System.IO;

namespace DirRX.Solution.Client
{
  public class ModuleFunctions
  {

    /// <summary>
    /// Преобразовать структуру с строку JSON.
    /// </summary>
    [Public]
    public string SerializeObjectToJSONClient(object entity)
    {
      var json = JsonConvert.SerializeObject(entity, Newtonsoft.Json.Formatting.Indented);
      Logger.Debug(json);
      return json;
    }

    /// <summary>
    /// Вывести диалог запроса результата согласования договорного документа с контрагентом.
    /// </summary>
    /// <param name="documents">документ.</param>
    public void ShowApprovalResultDialog(Sungero.Contracts.IContractualDocument document)
    {
      var dialog = Dialogs.CreateInputDialog(DirRX.Solution.Contracts.Resources.CheckReturn, DirRX.Solution.Contracts.Resources.DocumentReturned);
      var comment = dialog.AddMultilineString(DirRX.Solution.Contracts.Resources.Comment, false);
      var notSignButton = dialog.Buttons.AddCustom(DirRX.Solution.Contracts.Resources.NotSign);
      dialog.Buttons.AddCancel();
      var result = dialog.Show();
      
      if (result == DialogButtons.Cancel)
        return;
      
      var approvalCheckReturnAssignmentCompletedResult = DirRX.Solution.Functions.Module.Remote.ApprovalCheckReturnAssignmentCompleted(document, false, comment.Value);
      if (String.IsNullOrEmpty(approvalCheckReturnAssignmentCompletedResult))
      {
        var resultStr = DirRX.Solution.Contracts.Resources.NotSign;
        Dialogs.NotifyMessage(DirRX.Solution.Contracts.Resources.ApprovalCheckReturnAssignmentCompletedFormat(resultStr));
      }
      else
        Dialogs.NotifyMessage(approvalCheckReturnAssignmentCompletedResult);
    }
    
    /// <summary>
    /// Вывести диалог с предуспреждением по массовой обработке документов в списке и возможностью открыть список необработанных документов.
    /// </summary>
    /// <param name="allDocsCount">Общее количество обрабатываемых в действии документов.</param>
    /// <param name="notUpdatedDocs">Список документов, которые не удалось обработать.</param>
    /// <param name="messagePartiallyFormat">Итоговый текст сообщения, в котороый будет подставлено общее и количество обработанных документов. Пример: Успешно зафиксирован результат Подписан у {0} из {1} документов</param>
    public void ShowActionResultDialog(int allDocsCount, List<Sungero.Contracts.IContractualDocument> notUpdatedDocs,
                                       string messagePartiallyFormat)
    {
      // Если обработали не все документы, то покажем в диалоге.
      var updatedCount = allDocsCount - notUpdatedDocs.Count;
      var message = string.Format(messagePartiallyFormat, updatedCount, allDocsCount);
      var dialog = Dialogs.CreateTaskDialog(message, MessageType.Warning);
      var showButton = dialog.Buttons.AddCustom(DirRX.Solution.Resources.ShowButtonTitle);
      dialog.Buttons.AddOk();
      var result = dialog.Show();
      if (result.Name == showButton.Name)
        notUpdatedDocs.Show();
    }
    
    
    /// <summary>
    /// Вывести диалог с результатом массовой обработки пакетов на отправку и возможностью открыть список пакетов.
    /// </summary>
    /// <param name="message">Текст сообщения с результатом массовой обработки пакетов.</param>
    /// <param name="packages">Список пакетов.</param>
    /// <param name="messageType">Тип сообщения.</param>
    public void ShowPackagesActionResultDialog(string message, List<DirRX.ContractsCustom.IShippingPackage> packages, Sungero.Core.MessageType messageType)
    {
      var dialog = Dialogs.CreateTaskDialog(message, messageType);
      dialog.Buttons.AddOk();
      
      // Если есть пакеты, то добавим кнопку чтобы их посмотреть.
      if (packages.Count > 0)
      {
        var showButton = dialog.Buttons.AddCustom(DirRX.Solution.Resources.ShowPackagesButtonTitle);
        var result = dialog.Show();
        
        if (result.Name == showButton.Name)
          packages.Show();
      }
      else
        dialog.Show();
    }
    
    /// <summary>
    /// Вывести диалог с результатом массовой обработки пакетов на отправку и возможностью открыть список пакетов.
    /// </summary>
    /// <param name="message">Текст сообщения с результатом массовой обработки пакетов.</param>
    /// <param name="packages">Список пакетов.</param>
    [Public]
    public void ShowPackagesActionResultDialog(string message, List<DirRX.ContractsCustom.IShippingPackage> packages)
    {
      ShowPackagesActionResultDialog(message, packages, MessageType.Warning);
    }
    
    
    /// <summary>
    /// Показать окно выбора сотрудников.
    /// </summary>
    [Public]
    public IQueryable<DirRX.Solution.IEmployee> GetSelectedEmployees(List<DirRX.Solution.IEmployee> currentSubscribers)
    {
      return Solution.Functions.Module.Remote.GetAllEmployees().ShowSelectMany()
        .Where(e => !currentSubscribers.Any(s => DirRX.Solution.Employees.Equals(s, e)))
        .AsQueryable();
    }
    
    #region Скопировано из стандартного решения.
    
    /// <summary>
    /// Согласовать документ.
    /// </summary>
    /// <param name="assignment">Задание с документом.</param>
    /// <param name="endorse">Признак согласования документа, true - согласовать документ, false - не согласовывать.</param>
    /// <param name="autoComment">Автоматический комментарий.</param>
    /// <param name="eventArgs">Аргумент обработчика вызова.</param>
    public static void EndorseDocument(Sungero.Workflow.IAssignment assignment, bool endorse, string autoComment, Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      var task = ApprovalTasks.As(assignment.Task);
      var document = task.DocumentGroup.OfficialDocuments.Single();
      
      if (!document.HasVersions && !endorse)
        return;
      
      try
      {
        // Добавить в комментарий ЭП результат выполнения задания, если пользователь ничего не указал.
        var activeText = string.IsNullOrWhiteSpace(assignment.ActiveText) ? autoComment : assignment.ActiveText;
        var isSigned = endorse ?
          Sungero.Docflow.PublicFunctions.OfficialDocument.EndorseWithAddenda(document, task.AddendaGroup.OfficialDocuments.ToList(), null, activeText, assignment.Performer) :
          Signatures.NotEndorse(document.LastVersion, null, activeText, assignment.Performer);
        
        if (!isSigned)
          eventArgs.AddError(ApprovalTasks.Resources.ToPerformNeedSignDocument);
      }
      catch (CommonLibrary.Exceptions.PlatformException ex)
      {
        if (!ex.IsInternal)
        {
          var message = ex.Message.EndsWith(".") ? ex.Message : string.Format("{0}.", ex.Message);
          eventArgs.AddError(message);
        }
        else
          throw;
      }
    }
    
    #endregion
    
    /// <summary>
    /// Выполнение задания с результатом "На переработку".
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="e">Объект типа Sungero.Domain.Client.ExecuteActionArgs.</param>
    /// <param name="result">Результат выполнения.</param>
    public static void Recycling(Sungero.Workflow.IAssignment assignment, Sungero.Domain.Client.ExecuteActionArgs e, Enumeration result)
    {
      // TODO: Есть возможность вызова базового результата выполнения в действии. См. задание с возможностью доработки.
      var haveRights = true;
      var otherDocuments = new List<Sungero.Domain.Shared.IEntity>();
      var isConfirm = false;
      
      if (Solution.ApprovalAssignments.Is(assignment))
      {
        var approvalAssignment = Solution.ApprovalAssignments.As(assignment);
        haveRights = approvalAssignment.DocumentGroup.OfficialDocuments.Any();
        otherDocuments = approvalAssignment.OtherGroup.All.ToList();
        approvalAssignment.ForRecycle = true;
      }
      
      if (Solution.ApprovalSigningAssignments.Is(assignment))
      {
        var approvalSigningAssignment = Solution.ApprovalSigningAssignments.As(assignment);
        haveRights = approvalSigningAssignment.DocumentGroup.OfficialDocuments.Any();
        otherDocuments = approvalSigningAssignment.OtherGroup.All.ToList();
        isConfirm = approvalSigningAssignment.Stage.IsConfirmSigning.GetValueOrDefault();
        approvalSigningAssignment.ForRecycle = true;
      }
      
      if (!haveRights)
      {
        e.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return;
      }
      
      // Если права на вложения не выданы, то функция подменит диалог подтверждения на запрос выдачи прав.
      // Если все права на вложения выданы, то будет показан стандартный диалог подтверждения,
      // поэтому у действий убраны соответствующие галочки, иначе будет дублирование сообщений.
      if (!Sungero.Docflow.PublicFunctions.Module.ShowDialogGrantAccessRights(assignment, otherDocuments, e.Action))
        return;
      
      // Валидация заполненности активного текста.
      if (string.IsNullOrWhiteSpace(assignment.ActiveText))
      {
        e.AddError(DirRX.Solution.Resources.ActiveTextEmpty);
        return;
      }
      
      // Подписание согласующей подписью с результатом "не согласовано". Кроме результата "Подтвердить подписание".
      if (!isConfirm)
        Functions.Module.EndorseDocument(assignment, false, string.Empty, e);
      
      assignment.Complete(result);
      
      e.CloseFormAfterAction = true;
    }
    
    [Public]
    public static void SetWinAuthentication()
    {
      DirRX.Solution.Functions.Module.Remote.ChangeAuthentication();
    }
    
    public static void CreateDocPackages(List<Sungero.Contracts.IContractualDocument> docs)
    {
      var counterparty = Solution.Companies.Null;
      // Если документ один и это многосторонний договор/ДС, то запросим для какого контрагента формировать пакет.
      if (docs.Count == 1)
      {
        var document = docs.FirstOrDefault();
        var contract = Solution.Contracts.As(document);
        var supAgreement = Solution.SupAgreements.As(document);
        if ((contract != null && contract.IsManyCounterparties == true)||
            (supAgreement != null && supAgreement.IsManyCounterparties == true))
        {
          var possibleCounterparties = new List<Solution.ICompany>();
          if (contract != null)
            possibleCounterparties = contract.Counterparties.Select(c => c.Counterparty).ToList();
          if (supAgreement != null)
            possibleCounterparties = supAgreement.Counterparties.Select(c => c.Counterparty).ToList();
          
          // Запросим контрагента из списка контрагентов документа.
          var dialog = Dialogs.CreateInputDialog(DirRX.Solution.Resources.SelectCounterpartyToCreatePackageDialogTitle,
                                                 DirRX.Solution.Resources.SelectCounterpartyToCreatePackageDialogText);
          var counterpartyField = dialog.AddSelect(Solution.Contracts.Info.Properties.Counterparty.LocalizedName, true, Solution.Companies.Null).From(possibleCounterparties);
          dialog.Buttons.AddOkCancel();
          dialog.Buttons.Default = DialogButtons.Ok;
          if (dialog.Show() != DialogButtons.Ok)
            return;
          counterparty = counterpartyField.Value;
        }
      }
      var createPackagesResult = Functions.Module.Remote.CreatePackages(docs, counterparty);
      var createdPackages = createPackagesResult.Packages;
      var message = createPackagesResult.Message;
      var messageType = createdPackages.Count == 0 ? MessageType.Warning : MessageType.Information;
      Functions.Module.ShowPackagesActionResultDialog(message, createdPackages, messageType);
    }
  }
}