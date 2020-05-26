using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalTask;

namespace DirRX.Solution.Client
{
  partial class ApprovalTaskFunctions
  {

    /// <summary>
    /// Проверка и выполнение действий в зависимости от статуса контрагента.
    /// </summary>
    /// <param name="contractualDocument">Договорной документ.</param>
    /// <param name="counterpartyStatus">Статус контрагента.</param>
    /// <returns>Признак отправки задачи, иначе прервать отправку.</returns>
    public bool CheckCounterpartyStatus(Sungero.Contracts.IContractualDocument contractualDocument, DirRX.Solution.ICompany counterparty)
    {
      string dialogTitle = string.Empty;
      string dialogMessage = string.Empty;
      bool needComent = true;

      if (counterparty.CounterpartyStatus.Sid == DirRX.PartiesControl.PublicConstants.CounterpartyStatus.DefaultStatus.StopListSid)
      {
        dialogTitle = PartiesControl.CounterpartyStatuses.Resources.SendApprovalDialogTitleStoplistStatus;
        dialogMessage = PartiesControl.CounterpartyStatuses.Resources.SendApprovalDialogMessageStoplistStatus;
      }
      else if (counterparty.CheckingResult != null && counterparty.CheckingResult.Decision == PartiesControl.CheckingResult.Decision.NotApproved)
      {
        dialogTitle = PartiesControl.CounterpartyStatuses.Resources.SendApprovalDialogTitleNotAllowedStatus;
        dialogMessage = PartiesControl.CounterpartyStatuses.Resources.SendApprovalDialogMessageNotAllowedStatus;
      }
      else
      {
        needComent = false;
        dialogTitle = PartiesControl.CounterpartyStatuses.Resources.SendApprovalDialogTitleCheckingRequiredStatus;
        dialogMessage = PartiesControl.CounterpartyStatuses.Resources.SendApprovalDialogMessageCheckingRequiredStatus;
      }
      
      var dialog = Dialogs.CreateInputDialog(dialogTitle, dialogMessage);
      
      var comment = dialog.AddMultilineString(PartiesControl.CounterpartyStatuses.Resources.SendApprovalDialogInitiatorComment, true);
      comment.IsVisible = needComent;
      comment.IsRequired = needComent;
      
      var sendButton = dialog.Buttons.AddCustom(PartiesControl.CounterpartyStatuses.Resources.SendApprovalDialogSendButtonTitle);
      var cancelButton = dialog.Buttons.AddCancel();
      
      if (dialog.Show() == sendButton)
      {
        if (needComent)
          _obj.CommentOfInitiatorContract = comment.Value;
        else
        {
          // Если статус контрагента не совпадает с указанным в категории или "Требуется проверка", то создать заявку на проверку контрагента и отправить ее на согласование.
          var request = PartiesControl.PublicFunctions.RevisionRequest.Remote.CreateRevisionRequest(contractualDocument);
          if (request != null)
            Functions.ApprovalTask.Remote.StartRevisionSimpleTask(request);
        }
      }
      else
        return false;
      
      return true;
    }
    
    #region Согласование заявок на проверку контрагентов.
    
    /// <summary>
    /// Проверить вложения документов.
    /// </summary>
    /// <returns>Список невложеных документов.</returns>
    private IEnumerable<DirRX.PartiesControl.IRevisionRequestBindingDocuments> CheckAttachBindingsDocumnets(PartiesControl.IRevisionRequest revisionRequest)
    {
      return revisionRequest.BindingDocuments.Where(d => d.Document == null);
    }
    
    /// <summary>
    /// Получение строки со списком невложенных документов.
    /// </summary>
    /// <param name="documentKinds">Список документов.</param>
    /// <returns>Список невложеных документов.</returns>
    private string GetAttachBindingsDocumnets(List<string> documentKinds)
    {
      var result = new System.Text.StringBuilder();
      foreach (var documentKind in documentKinds)
        result.AppendLine(documentKind);
      return result.ToString();
    }
    
    /// <summary>
    /// Проверить вложения документов, в случае, если предоставление документов «Желательно».
    /// </summary>
    public bool CheckDesirableBindingsDocumnets(PartiesControl.IRevisionRequest revisionRequest)
    {
      var allAttached = CheckAttachBindingsDocumnets(revisionRequest);
      
      if (allAttached.Any())
      {
        var dialog = Dialogs.CreateInputDialog(ApprovalTasks.Resources.SendRevisionRequestApprovalDialogTitle);
        var reqDocs = allAttached.Where(d => d.IsRequired.HasValue && d.IsRequired.Value);
        if (reqDocs.Any())
        {
          var text = DirRX.Solution.ApprovalTasks.Resources.AttachedReqDocTextFormat(GetAttachBindingsDocumnets(reqDocs.Select(d => d.DocumentKind).ToList()));
          
          var desireDocs = allAttached.Where(d => !d.IsRequired.HasValue || !d.IsRequired.Value);
          if (desireDocs.Any())
            text = DirRX.Solution.ApprovalTasks.Resources.AttachedDesireDocTextFormat(text, GetAttachBindingsDocumnets(desireDocs.Select(d => d.DocumentKind).ToList()));
          
          dialog.Text = text;
          var continueButton = dialog.Buttons.AddOk();
          dialog.Show();
          
          return false;
        }
        else
        {
          dialog.Text = ApprovalTasks.Resources.SendRevisionRequestDesirableTextFormat(GetAttachBindingsDocumnets(allAttached.Select(d => d.DocumentKind).ToList()));
          var continueButton = dialog.Buttons.AddCustom(ApprovalTasks.Resources.SendRevisionRequestContinueTitle);
          var returnButton = dialog.Buttons.AddCustom(ApprovalTasks.Resources.SendRevisionRequestReturnTitle);
          
          var result = dialog.Show();
          if (result == returnButton)
            revisionRequest.Show();
          
          return result == continueButton;
        }
      }
      else
        return true;
    }
    
    /// <summary>
    /// Проверить вложения документов, в случае, если предоставление документов «Обязательно».
    /// </summary>
    public bool CheckRequiredBindingsDocumnets(PartiesControl.IRevisionRequest revisionRequest)
    {
      var allAttached = CheckAttachBindingsDocumnets(revisionRequest);
      
      if (allAttached.Any())
      {
        var dialog = Dialogs.CreateInputDialog(ApprovalTasks.Resources.SendRevisionRequestApprovalDialogTitle);
        dialog.Text = ApprovalTasks.Resources.SendRevisionRequestRequiredTextFormat(GetAttachBindingsDocumnets(allAttached.Select(d => d.DocumentKind).ToList()));
        var continueButton = dialog.Buttons.AddOk();
        dialog.Show();
        
        return false;
      }
      else
        return true;
    }
    
    #endregion
    
    /// <summary>
    /// Проверить возможность отправки задания на доработку.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="errorMessage">Сообщение об ошибке.</param>
    /// <param name="eventArgs">Аргумент обработчика вызова.</param>
    /// <returns>True - разрешить отправку, иначе false.</returns>
    public static bool ValidateBeforeRework(Sungero.Workflow.IAssignment assignment, string errorMessage, Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      if (string.IsNullOrWhiteSpace(assignment.ActiveText))
      {
        eventArgs.AddError(errorMessage);
        return false;
      }
      
      return true;
    }
  }
}