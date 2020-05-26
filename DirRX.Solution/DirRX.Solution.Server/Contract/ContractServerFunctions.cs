using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Contract;
using Sungero.Contracts;
using Sungero.Domain.Shared;
using Sungero.Metadata;

namespace DirRX.Solution.Server
{
  partial class ContractFunctions
  {
    /// <summary>
    /// Зафиксировать факт передачи оригиналов на подписание.
    /// </summary>
    /// <param name="documents">Список договоров.</param>
    /// <returns>Список договоров, для которых не удалось зафиксировать факт передачи оригиналов на подписание.</returns>
    [Remote]
    public static List<Sungero.Contracts.IContractualDocument> SetStateOnSigning(List<Sungero.Contracts.IContractualDocument> documents)
    {
      var notProcessedDocs = new List<Sungero.Contracts.IContractualDocument>();
      // В выбранных документах будет зафиксирован факт передачи оригиналов на подписание (значение в поле Подписание оригиналов – На подписании).
      foreach (var doc in documents)
      {
        var lockInfo = Locks.GetLockInfo(doc);
        if (lockInfo.IsLockedByOther)
        {
          notProcessedDocs.Add(doc);
          Logger.DebugFormat(DirRX.Solution.Contracts.Resources.LockedContractErrorMessage, doc.Name, lockInfo.OwnerName, lockInfo.LockedMessage);
        }
        else
        {
          var contract = Contracts.As(doc);
          if (contract != null)
          {
            contract.OriginalSigning = DirRX.Solution.Contract.OriginalSigning.OnSigning;
            contract.Save();
          }
          
          var supAgreement = SupAgreements.As(doc);
          if (supAgreement != null)
          {
            supAgreement.OriginalSigning = DirRX.Solution.Contract.OriginalSigning.OnSigning;
            supAgreement.Save();
          }
        }
      }
      return notProcessedDocs;
    }
    
    public override List<Sungero.Docflow.IApprovalRuleBase> GetApprovalRules()
    {
      return Functions.ContractsApprovalRule.GetAvailableRulesByDocumentCustom(_obj)
        .OrderByDescending(r => r.Priority)
        .ToList();
    }
    
    #region Сводка по документу
    
    /// <summary>
    /// Получить текущую сумму по договору.
    /// </summary>
    /// <returns>Текущая сумма по договору.</returns>
    [Remote(IsPure = true)]
    public virtual StateView GetCurrentAmount()
    {
      var documentSummary = StateView.Create();
      
      var documentBlock = documentSummary.AddBlock();
      documentBlock.DockType = DockType.Bottom;
      var amount = Functions.Contract.GetContractTotalAmount(_obj);
      var documentAmount = string.Format(DirRX.Solution.Contracts.Resources.CurrentAmount, amount.ToString("N"));
      documentBlock.AddLabel(documentAmount);
      
      return documentSummary;
    }
    
    /// <summary>
    /// Построить сводку по документу.
    /// </summary>
    /// <returns>Сводка по документу.</returns>
    [Remote(IsPure = true)]
    public override StateView GetDocumentSummary()
    {
      var documentSummary = StateView.Create();
      var documentBlock = documentSummary.AddBlock();
      
      #region Скопировано из стандартной
      
      // Краткое имя документа.
      var documentName = _obj.DocumentKind.Name;
      if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
        documentName += Sungero.Docflow.OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
      
      if (_obj.RegistrationDate != null)
        documentName += Sungero.Docflow.OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
      
      documentBlock.AddLabel(documentName);
      
      // Типовой/Не типовой.
      var isStadartLabel = _obj.IsStandard.Value ? ContractBases.Resources.isStandartContract : ContractBases.Resources.isNotStandartContract;
      documentBlock.AddLabel(string.Format("({0})", isStadartLabel));
      documentBlock.AddLineBreak();
      documentBlock.AddEmptyLine();
      
      #endregion
      
      // Контрагент.
      documentBlock.AddLabel(string.Format("{0}:", DirRX.Solution.Contracts.Resources.Counterparties));
      documentBlock.AddLineBreak();
      foreach (var cRow in _obj.Counterparties)
      {
        documentBlock.AddLabel(Hyperlinks.Get(cRow.Counterparty));
        if (cRow.Counterparty != null && cRow.Counterparty.Nonresident == true)
          documentBlock.AddLabel(string.Format("({0})", cRow.Counterparty.Info.Properties.Nonresident.LocalizedName).ToLower());
        
        if (cRow.Counterparty.CounterpartyStatus != null)
        {
          documentBlock.AddLineBreak();
          documentBlock.AddLabel(Contracts.Resources.CounterpartyStateFormat(cRow.Counterparty.CounterpartyStatus.Name));
        }
        documentBlock.AddLineBreak();
      }
      documentBlock.AddEmptyLine();
      
      #region Скопировано из стандартной
      
      // Содержание.
      documentBlock.AddLabel(string.Format("{0}: {1}", ContractBases.Resources.Subject, _obj.Subject));
      documentBlock.AddLineBreak();
      documentBlock.AddEmptyLine();
      
      #endregion
      
      var defaultCurrency = Sungero.Commons.PublicFunctions.Currency.Remote.GetDefaultCurrency();
      var currency = _obj.Currency == null ? defaultCurrency : _obj.Currency;
      var currencyUSD = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetCurrencyUSD();
      
      // Сумма согласуемого документа.
      var transactionAmount = _obj.TransactionAmount.Value;
      var transactionAmountText = string.Format("{0}: {1} {2}", _obj.Info.Properties.TransactionAmount.LocalizedName, transactionAmount.ToString("N"), currency.AlphaCode);
      documentBlock.AddLabel(transactionAmountText);
      documentBlock.AddLineBreak();
      
      // Сумма согласуемого документа в долларах США, если валюта документа не $.
      if (currency != currencyUSD)
      {
        var amountUSD = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetSummInUSD(transactionAmount, currency);
        var amountUSDText = string.Format("{0}: {1:F} {2}", DirRX.Solution.Contracts.Resources.DocumentSummaryAmountUSDName, amountUSD.ToString("N"), currencyUSD.AlphaCode);
        documentBlock.AddLabel(amountUSDText);
        documentBlock.AddLineBreak();
      }
      
      // Сумма договора превышает 1,5 млн. руб.
      if (_obj.IsFrameContract != true && _obj.IsTender != true)
      {
        var amount = Functions.Contract.GetContractTotalAmount(_obj);
        var totalAmount = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetSummInRUB(amount, currency);
        var maxAmount = ContractsCustom.PublicFunctions.Module.Remote.GetContractMaxAmountInRUB();
        if (maxAmount.HasValue && totalAmount > maxAmount.Value)
        {
          var maxAmountConstant =  ContractsCustom.PublicFunctions.Module.Remote.GetContractConstant(DirRX.ContractsCustom.PublicConstants.Module.ContractMaxAmountGuid.ToString());
          var maxAmountText = DirRX.Solution.Contracts.Resources.DocumentSummaryMaxAmountNameFormat(maxAmountConstant.Amount.Value, maxAmountConstant.Currency.AlphaCode);
          documentBlock.AddLabel(maxAmountText);
          documentBlock.AddLineBreak();
        }
      }
      
      // Срок действия договора.
      if (_obj.IsTermless != true)
      {
        var validity = "-";
        var validFrom = _obj.ValidFrom.HasValue ?
          string.Format("{0} {1} ", ContractBases.Resources.From, _obj.ValidFrom.Value.Date.ToShortDateString()) :
          string.Empty;
        
        var validTill = _obj.ValidTill.HasValue ?
          string.Format("{0} {1}", ContractBases.Resources.Till, _obj.ValidTill.Value.Date.ToShortDateString()) :
          string.Empty;
        
        var isAutomaticRenewal = _obj.IsAutomaticRenewal.Value &&  !string.IsNullOrEmpty(validTill) ?
          string.Format(", {0}", ContractBases.Resources.Renewal) :
          string.Empty;
        
        if (!string.IsNullOrEmpty(validFrom) || !string.IsNullOrEmpty(validTill))
          validity = string.Format("{0}{1}{2}", validFrom, validTill, isAutomaticRenewal);
        
        var validityText = string.Format("{0}:", ContractBases.Resources.Validity);
        documentBlock.AddLabel(validityText);
        documentBlock.AddLabel(validity);
      }
      else
      {
        var termlessText = DirRX.Solution.Contracts.Resources.DocumentSummaryIsTermlessName;
        documentBlock.AddLabel(termlessText);
      }
      documentBlock.AddLineBreak();
      
      // Добавить отметку "Требуется согласование с ПАО «ЛУКОЙЛ".
      if (_obj.LukoilApproving == DirRX.Solution.Contract.LukoilApproving.Required)
      {
        documentBlock.AddLabel(DirRX.ContractsCustom.ContractSettingses.Info.Properties.LukoilApproval.LocalizedName);
        documentBlock.AddLineBreak();
      }
      
      // Страны в перечне спорных территорий.
      var destinationCountriesNames = _obj.DestinationCountries.Where(c => c.DestinationCountry != null && c.DestinationCountry.IsIncludedInDisputedTerritories == true)
        .Select(c => c.DestinationCountry.Name);
      if (destinationCountriesNames.Count() > 0)
      {
        var countries = string.Join(", ", destinationCountriesNames);
        var countriesText = string.Format("{0}: {1}", DirRX.Solution.Contracts.Resources.DocumentSummaryCountriesName, countries);
        documentBlock.AddLabel(countriesText);
        documentBlock.AddLineBreak();
      }
      
      // Комментарий инициатора договора.
      documentBlock.AddEmptyLine();
      
      var docGuid = _obj.GetEntityMetadata().GetOriginal().NameGuid;
      var approvalTaskDocumentGroupGuid = Sungero.Docflow.Constants.Module.TaskMainGroup.ApprovalTask;
      var task = DirRX.Solution.ApprovalTasks.GetAll(t => t.AttachmentDetails.Any(att => att.AttachmentId == _obj.Id &&
                                                                                  att.EntityTypeGuid == docGuid &&
                                                                                  att.GroupId == approvalTaskDocumentGroupGuid ))
        .OrderByDescending(t => t.Started)
        .FirstOrDefault();
      var commentOfInitiator = "-";
      if (task != null && !string.IsNullOrEmpty(task.CommentOfInitiatorContract))
        commentOfInitiator = task.CommentOfInitiatorContract;
      var commentOfInitiatorText = string.Format("{0}:", DirRX.Solution.ApprovalTasks.Info.Properties.CommentOfInitiatorContract.LocalizedName);
      documentBlock.AddLabel(commentOfInitiatorText);
      documentBlock.AddLabel(commentOfInitiator);
      documentBlock.AddLineBreak();
      
      #region Скопировано из стандартной
      
      // Примечание.
      var note = string.IsNullOrEmpty(_obj.Note) ? "-" : _obj.Note;
      var noteText = string.Format("{0}:", ContractBases.Resources.Note);
      documentBlock.AddLabel(noteText);
      documentBlock.AddLabel(note);
      
      #endregion
      
      return documentSummary;
    }
    
    #endregion
    
    /// <summary>
    /// Общая сумма по договору в валюте договора.
    /// </summary>
    /// <returns>Сумма.</returns>
    [Remote(IsPure = true)]
    public double GetContractTotalAmount()
    {
      double amount = ContractsCustom.PublicFunctions.Module.Remote.GetSupAgreementsAmount(_obj);
      if (_obj.IsFrameContract != true && _obj.TransactionAmount.HasValue)
        amount += _obj.TransactionAmount.Value;
      
      return amount;
    }
    
    /// <summary>
    /// Отменить факт передачи оригиналов на подписание.
    /// </summary>
    /// <param name="documents">Список договоров.</param>
    /// <returns>Список договоров, для которых не удалось отменить факт передачи оригиналов на подписание.</returns>
    [Remote]
    public static List<Sungero.Contracts.IContractualDocument> CancelStateOnSigning(List<Sungero.Contracts.IContractualDocument> documents)
    {
      var notProcessedDocs = new List<Sungero.Contracts.IContractualDocument>();
      // В выбранных документах будет отменен факт передачи оригиналов на подписание (значение в поле Подписание оригиналов – пусто).
      foreach (var doc in documents)
      {
        var lockInfo = Locks.GetLockInfo(doc);
        if (lockInfo.IsLockedByOther)
        {
          notProcessedDocs.Add(doc);
          Logger.DebugFormat(DirRX.Solution.Contracts.Resources.LockedContractErrorMessage, doc.Name, lockInfo.OwnerName, lockInfo.LockedMessage);
        }
        else
        {
          // Удаление статуса "Оригинал передан на подписание в Обществе".
          ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(doc,
                                                                                   ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalSendedBusinessUnitForSigningGuid,
                                                                                   ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus);
          
          var contract = Contracts.As(doc);
          if (contract != null)
          {
            contract.OriginalSigning = null;
            contract.Save();
          }
          
          var supAgreement = SupAgreements.As(doc);
          if (supAgreement != null)
          {
            supAgreement.OriginalSigning = null;
            supAgreement.Save();
          }
        }
      }
      return notProcessedDocs;
    }
    
    /// <summary>
    /// Отправить задачу на отправку документа с нарочным.
    /// </summary>
    /// <param name="doc">Договорной документ.</param>
    /// <param name="employee">Ответственный.</param>
    /// <param name="deliveryDate">Дата выдачи.</param>
    /// <param name="returnDate">Дата возврата.</param>
    /// <param name="comment">Комментарий.</param>
    [Remote]
    public static void StartSendDocWithResposibleTask(Sungero.Contracts.IContractualDocument doc,
                                                      Sungero.Company.IEmployee employee, DateTime deliveryDate, DateTime? returnDate, string comment)
    {
      // Запись в очередь на установку статуса договора "Оригинал документа помещен в пакет для отправки".
      var item = ContractsCustom.ContractQueueItems.Create();
      item.DocumentId = doc.Id;
      item.ContractStatusAction = ContractsCustom.PublicConstants.Module.StatusAction.AddAction;
      item.ContractStatusType = ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus;
      item.ContractStatusSid = ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalAcceptedForSendingGuid.ToString();
      item.Save();
      
      // Запустить агент установки статусов договоров.
      ContractsCustom.Jobs.SetContractStatusInPackagesJob.Enqueue();
      
      //Отправим задачу на отправку документа с нарочным.
      var task = DirRX.ContractsCustom.SendWithResposibleTasks.Create();
      task.Employee = Solution.Employees.As(employee);
      task.DeliveryDate = deliveryDate;
      task.ReturnDate = returnDate;
      task.Comment = comment;
      task.AttachmentContractGroup.ContractualDocuments.Add(doc);
      
      task.Save();
      task.Start();
      
    }
    
  }
}