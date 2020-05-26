using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.SupAgreement;
using Sungero.Docflow;
using Sungero.Domain.Shared;
using Sungero.Metadata;

namespace DirRX.Solution.Server
{
  partial class SupAgreementFunctions
  {

    /// <summary>
    /// Получить доп. соглашения по договору.
    /// </summary>
    /// <param name="contract">Договор.</param>
    /// <returns>Доп. соглашения по договору исключая текущее.</returns>
    [Remote(IsPure = true)]
    public IQueryable<ISupAgreement> GetSupAgreement(IContract contract)
    {
      return Solution.SupAgreements.GetAll(d => (d.LeadingDocument.Equals(contract)) && (d.Id != _obj.Id)).OrderByDescending(d => d.Created);
    }
    
    
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
      double amount = Functions.SupAgreement.GetContractTotalAmount(_obj);
      var documentAmount = string.Format(DirRX.Solution.SupAgreements.Resources.CurrentAmount, amount.ToString("N"));
      
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
        documentName += OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
      
      if (_obj.RegistrationDate != null)
        documentName += OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
      
      documentBlock.AddLabel(documentName);
      
      // Типовое/Не типовое.
      var isStadartLabel = _obj.IsStandard.Value ? SupAgreements.Resources.IsStandartSupAgreement : SupAgreements.Resources.IsNotStandartSupAgreement;
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
      documentBlock.AddLabel(string.Format("{0}: {1}", _obj.Info.Properties.Subject.LocalizedName, _obj.Subject));
      documentBlock.AddLineBreak();
      documentBlock.AddEmptyLine();
      
      #endregion
      
      var defaultCurrency = Sungero.Commons.PublicFunctions.Currency.Remote.GetDefaultCurrency();
      var currency = _obj.Currency == null ? defaultCurrency : _obj.Currency;
      var currencyUSD = ContractsCustom.PublicFunctions.CurrencyRate.Remote.GetCurrencyUSD();
      var contract = Contracts.As(_obj.LeadingDocument);
      
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
      
      // Проверка суммы рамочного договора.
      if (contract.IsFrameContract == true)
      {
        var contractAmount = contract.TransactionAmount.Value;
        var currAmount = Functions.SupAgreement.GetContractTotalAmount(_obj);
        if (currAmount > contractAmount)
        {
          documentBlock.AddLabel(DirRX.Solution.SupAgreements.Resources.DocumentSummaryCheckCurrentAmountName);
          documentBlock.AddLineBreak();
        }
      }
      
      // Сумма договора превышает 1,5 млн. руб.
      if (contract.IsFrameContract != true && contract.IsTender != true)
      {
        var amount = Functions.SupAgreement.GetContractTotalAmount(_obj);
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
      
      #region Скопировано из стандартной
      
      // Срок действия.
      var validity = "-";
      var validFrom = _obj.ValidFrom.HasValue
        ? string.Format("{0} {1} ", Sungero.Contracts.ContractBases.Resources.From, _obj.ValidFrom.Value.ToShortDateString())
        : string.Empty;
      var validTill = _obj.ValidTill.HasValue
        ? string.Format("{0} {1}", Sungero.Contracts.ContractBases.Resources.Till, _obj.ValidTill.Value.ToShortDateString())
        : string.Empty;
      if (!string.IsNullOrEmpty(validFrom) || !string.IsNullOrEmpty(validTill))
        validity = string.Format("{0}{1}", validFrom, validTill);
      
      var validityText = string.Format("{0}: {1}", Sungero.Contracts.ContractBases.Resources.Validity, validity);
      documentBlock.AddLabel(validityText);
      documentBlock.AddLineBreak();
      #endregion
      
      // Проверка срока действия договора.
      if (contract.IsTermless != true && contract.ValidTill != null && contract.ValidTill.Value.CompareTo(Calendar.Now) < 0)
      {
        documentBlock.AddLabel(DirRX.Solution.SupAgreements.Resources.DocumentSummaryCheckContractValidName);
        documentBlock.AddLineBreak();
      }
      
      // Добавить отметку "Требуется получение согласования ПАО "Лукойл".
      if (_obj.LukoilApproving == DirRX.Solution.SupAgreement.LukoilApproving.Required)
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
      
      // Примечание.
      var note = !string.IsNullOrEmpty(_obj.Note) ? _obj.Note : "-";
      documentBlock.AddLabel(string.Format("{0}: {1}", _obj.Info.Properties.Note.LocalizedName, note));
      
      return documentSummary;
    }
    
    /// <summary>
    /// Общая сумма по договору в валюте договора.
    /// </summary>
    [Public, Remote(IsPure = true)]
    public double GetContractTotalAmount()
    {
      var contract = Contracts.As(_obj.LeadingDocument);
      // Сумма всех дополнительных соглашений в состоянии: Действующий, Недействующий.
      double amount = ContractsCustom.PublicFunctions.Module.Remote.GetSupAgreementsAmount(contract);
      // Добавим текущий документ если он в разработке.
      if (_obj.TransactionAmount.HasValue && _obj.LifeCycleState == LifeCycleState.Draft)
        amount += _obj.TransactionAmount.Value;
      // Если договор не рамочный, то добавим сумму договора.
      if (contract.IsFrameContract != true && contract.TransactionAmount.HasValue)
        amount += contract.TransactionAmount.Value;
      
      return amount;
    }

  }
}