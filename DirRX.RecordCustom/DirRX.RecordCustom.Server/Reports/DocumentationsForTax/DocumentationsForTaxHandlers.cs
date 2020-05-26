using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.RecordCustom
{
  partial class DocumentationsForTaxServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.DocumentationsForTax.OrderOnIncomingLettersTableName, DocumentationsForTax.ReportSessionId);
    }
    
    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      DocumentationsForTax.ReportDate = Calendar.UserNow;
      var incomingLetters = DirRX.Solution.IncomingLetters.GetAll().Where(x => x.RequirementNumberDirRX.Length > 0 && x.RegistrationState == Sungero.Docflow.OfficialDocument.RegistrationState.Registered
                                                                          && x.RegistrationDate.Between(Convert.ToDateTime(DocumentationsForTax.BeginDate), Convert.ToDateTime(DocumentationsForTax.EndDate))).ToList();
      incomingLetters = incomingLetters.Where(x => x.AccessRights.CanRead()).ToList();
      
      if (incomingLetters.Any())
      {
        DocumentationsForTax.ReportSessionId = Guid.NewGuid().ToString();
        Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.DocumentationsForTax.OrderOnIncomingLettersTableName, DocumentationsForTax.ReportSessionId);
        FillOrderIncomingLettersTable(DocumentationsForTax.ReportSessionId, incomingLetters);
      }
    }
    
    /// <summary>
    /// Заполнение таблицы отчета данными.
    /// </summary>
    /// <param name="reportSesseionId">Id сесиии.</param>
    /// <param name="incomingLetter">Входящее письмо</param>
    private static void FillOrderIncomingLettersTable(string reportSesseionId, List<DirRX.Solution.IIncomingLetter> incomingLetters)
    {
      var dataTableLines = new List<RecordCustom.Structures.DocumentationsForTax.DocumentsTaxGroup>();
      var outgoingLetters = DirRX.Solution.OutgoingLetters.GetAll().ToList().Where(outgoing => incomingLetters.Any(incoming => outgoing.InResponseTo != null && incoming.Id == outgoing.InResponseTo.Id));
      
      foreach (var incomingLetter in incomingLetters)
      {
        var docActionItems = GetActionItemsByDocument(incomingLetter).ToList();
        DirRX.Solution.IOutgoingLetter outgoingLetter = null;
        if (outgoingLetters != null)
          outgoingLetter = outgoingLetters.Where(x => DirRX.Solution.IncomingLetters.Equals(x.InResponseTo, incomingLetter)).FirstOrDefault();
        
        if (docActionItems.Any())
        {
          var sorted = new List<Sungero.RecordManagement.IActionItemExecutionTask>();
          var mainTasks = docActionItems.Where(x => x.Id == x.MainTaskId || docActionItems.All(y => y.Id != x.MainTaskId)).ToList();
          
          OrdeList(mainTasks, docActionItems, sorted);
          
          foreach (var item in sorted)
          {
            var structure = FillLineDocData(incomingLetter,outgoingLetter, reportSesseionId);
            structure.ActualDate = item.ActualDate.HasValue ? item.ActualDate.Value.ToString("d") : string.Empty;
            structure.Assignee = item.Assignee != null ? item.Assignee.DisplayValue : string.Empty;
            structure.CoAssignee = item.CoAssignees!= null ? string.Join(", ", item.CoAssignees.Select(a => a.Assignee.DisplayValue)) : string.Empty;
            structure.CreateDateActionItem = item.Created.HasValue ? item.Created.Value.ToString("d") : string.Empty;
            structure.MaxDeadline = item.MaxDeadline.HasValue ? item.MaxDeadline.Value.ToString("d") : string.Empty;
            if (!string.IsNullOrEmpty(structure.MaxDeadline) && Convert.ToDateTime(structure.MaxDeadline) < Calendar.UserNow)
              structure.IsExpired = 1;
            dataTableLines.Add(structure);
          }
          
        }
        else
          dataTableLines.Add(FillLineDocData(incomingLetter,outgoingLetter, reportSesseionId));
      }
      
      Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(RecordCustom.Constants.DocumentationsForTax.OrderOnIncomingLettersTableName, dataTableLines);
      
    }
    
    /// <summary>
    /// Заполнить структуру данными из входящего письма и исходящего письма в ответ.
    /// </summary>
    /// <param name="incomingLetter">Входящее письмо.</param>
    /// <param name="outgoingLetter">Исходящее письмо.</param>
    /// <param name="reportSessionId">ID сессии.</param>
    /// <returns>Заполеннная структура.</returns>
    private static RecordCustom.Structures.DocumentationsForTax.DocumentsTaxGroup FillLineDocData(DirRX.Solution.IIncomingLetter incomingLetter,DirRX.Solution.IOutgoingLetter outgoingLetter, string reportSessionId)
    {
      var structure = RecordCustom.Structures.DocumentationsForTax.DocumentsTaxGroup.Create();
      
      structure.IncomingLetterId = incomingLetter.Id;
      structure.Content = incomingLetter.Subject ?? string.Empty;
      structure.Correspondent = incomingLetter.Correspondent.DisplayValue ?? string.Empty;
      structure.DateOF = incomingLetter.Dated.HasValue ? incomingLetter.Dated.Value.ToString("d") : string.Empty;
      structure.DocumentDate = incomingLetter.RegistrationDate.HasValue ? incomingLetter.RegistrationDate.Value.ToString("d") : string.Empty;
      structure.Number = incomingLetter.InNumber ?? string.Empty;
      structure.IncomingLetterHyperLink = Hyperlinks.Get(incomingLetter);
      structure.PageCountIncoming = incomingLetter.PageCount;
      structure.ReportSessionId = reportSessionId;
      structure.RequarimentNumber = incomingLetter.RequirementNumberDirRX;
      structure.IsExpired = 0;
      structure.RegNumber = incomingLetter.RegistrationNumber ?? string.Empty;
      
      if (outgoingLetter != null){
        structure.OutgoingLetter = outgoingLetter.DisplayValue ?? string.Empty;
        structure.Prepared = outgoingLetter.PreparedBy.DisplayValue ?? string.Empty;
        structure.PageCountOutgoingLetter = outgoingLetter.PageCountDirRX;
        structure.OutgoingLetterId = outgoingLetter.Id;
        structure.OutgoingLetterHyperLink = Hyperlinks.Get(outgoingLetter);
      }
      
      return structure;
    }
    
    /// <summary>
    /// Сортировка списка поручений. Сортирует в глубину по иерархии поручений.
    /// </summary>
    /// <param name="curentList">Список главных поручений.</param>
    /// <param name="allRecord">Список всех поручений.</param>
    /// <param name="orderRecord">Список отсортированых поручений.</param>
    private static void OrdeList(List<Sungero.RecordManagement.IActionItemExecutionTask> curentList, List<Sungero.RecordManagement.IActionItemExecutionTask> allRecord, List<Sungero.RecordManagement.IActionItemExecutionTask> orderRecord)
    {
      foreach (var item in curentList)
      {
        orderRecord.Add(item);
        var list2 = allRecord.Where(x => x.MainTaskId == item.Id && x.Id != x.MainTaskId).ToList();
        if (list2.Any())
          OrdeList(list2, allRecord, orderRecord);
      }
    }
    
    /// <summary>
    /// Получить все поручения по документу.
    /// </summary>
    /// <param name="document">Входящее письмо.</param>
    /// <returns>Список поручений.</returns>
    private static IEnumerable<Sungero.RecordManagement.IActionItemExecutionTask> GetActionItemsByDocument(Sungero.Docflow.IOfficialDocument document)
    {
      var documentsGroupGuid = Sungero.Docflow.PublicConstants.Module.TaskMainGroup.ActionItemExecutionTask;
      
      var actionItems = Sungero.RecordManagement.ActionItemExecutionTasks.GetAll()
        .Where(t => t.AttachmentDetails.Any(g => g.GroupId == documentsGroupGuid && document.Id == g.AttachmentId) && t.Status != Sungero.RecordManagement.ActionItemExecutionTask.Status.Aborted)
        .Where(t => t.IsCompoundActionItem != true && t.ActionItemType != Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Additional);
      
      actionItems = actionItems.Where(x => (x.ParentAssignment != null && !DirRX.Solution.ActionItemExecutionAssignments.Is(x.ParentAssignment)) || x.ParentAssignment == null);
      
      return actionItems.ToList();
    }

  }
}