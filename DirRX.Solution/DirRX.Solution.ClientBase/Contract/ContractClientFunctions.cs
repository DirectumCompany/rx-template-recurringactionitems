using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Contract;

namespace DirRX.Solution.Client
{
  partial class ContractFunctions
  {

    /// <summary>
    /// Проверка заполненности обязательных полей перед отправкой на согласование по регламенту.
    /// </summary>       
    public static bool CheckRequiredProperties(DirRX.Solution.IContract doc, Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Проверить заполненность обязательных полей.
      bool propertiesEmpty = false;
      // Вид документа.
      if (doc.DocumentKind == null)
      {
      	propertiesEmpty = true;
      	doc.State.Properties.DocumentKind.IsRequired = true;
      }
      // Категория.
      var hasAvailableCategories = Sungero.Docflow.DocumentGroupBases.GetAllCached(g => g.Status == Sungero.CoreEntities.DatabookEntry.Status.Active &&
                                                                           g.DocumentKinds.Any(d => Equals(d.DocumentKind, doc.DocumentKind))).Any();
      if (doc.DocumentGroup == null && doc.DocumentKind != null && hasAvailableCategories)
      {
      	propertiesEmpty = true;
        doc.State.Properties.DocumentGroup.IsRequired = true;
      }
      // Код вида договора ИСУ ЛЛК.
      if (doc.IMSCodeCollection.Count == 0 && doc.ContractFunctionality != ContractFunctionality.Mixed)
      {
      	propertiesEmpty = true;
      	doc.State.Properties.IMSCodeCollection.IsRequired = true;
      }
      // Контрагент.
      if (doc.Counterparty == null)
      {
      	propertiesEmpty = true;
      	doc.State.Properties.Counterparty.IsRequired = true;
      }
      // Способ доставки.
      var isManyCounterparties = doc.IsManyCounterparties.GetValueOrDefault();
      if (doc.DeliveryMethod == null && !isManyCounterparties)
      {
        propertiesEmpty = true;
      	doc.State.Properties.DeliveryMethod.IsRequired = true;
      }
      // Контакт.
      if (doc.Contact == null && doc.DeliveryMethod != null && doc.DeliveryMethod.IsRequireContactInContract == true)
      {
      	propertiesEmpty = true;
      	doc.State.Properties.Contact.IsRequired = true;
      }
      // Адрес отправки.
      if (doc.ShippingAddress == null && !isManyCounterparties)
      {
        propertiesEmpty = true;
      	doc.State.Properties.ShippingAddress.IsRequired = true;
      }
      // Причина смены способа доставки.
      if (doc.Counterparty != null && !isManyCounterparties && doc.ChangingShippingReason == null && !Solution.MailDeliveryMethods.Equals(doc.DeliveryMethod, doc.Counterparty.DeliveryMethod))
      {
      	propertiesEmpty = true;
      	doc.State.Properties.ChangingShippingReason.IsRequired = true;
      }
      // Подразделение.
      if (doc.Department == null)
      {
      	propertiesEmpty = true;
      	doc.State.Properties.Department.IsRequired = true;
      }
      // Куратор.
      if (doc.Supervisor == null)
      {
      	propertiesEmpty = true;
      	doc.State.Properties.Supervisor.IsRequired = true;
      }
      // Территория
      if (doc.Territory == null)
      {
      	propertiesEmpty = true;
      	doc.State.Properties.Territory.IsRequired = true;
      }
      // Напоминать об окончании за.
      if (doc.DaysToFinishWorks == null && doc.IsTermless != true)
      {
      	propertiesEmpty = true;
      	doc.State.Properties.DaysToFinishWorks.IsRequired = true;
      }      
      // Действует по.
      if (doc.ValidTill == null && doc.IsTermless != true && !doc.DocumentValidity.HasValue)
      {
        propertiesEmpty = true;
        doc.State.Properties.ValidTill.IsRequired = true;
      }
      else
      {
        doc.State.Properties.ValidTill.IsRequired = false;
      }
      // Срок действия договора.
      if (doc.DocumentValidity == null && doc.IsTermless != true && !doc.ValidTill.HasValue)
      {
        propertiesEmpty = true;
        doc.State.Properties.DocumentValidity.IsRequired = true;
      }
      else
      {
        doc.State.Properties.DocumentValidity.IsRequired = false;
      }
        
      // Страна назначения.
      if (doc.DestinationCountries.Count == 0 &&
          (doc.DocumentGroup == null || (doc.DocumentGroup != null && doc.DocumentGroup.DestinationCountry != DirRX.Solution.ContractCategory.DestinationCountry.NotRequired)))
      {
        propertiesEmpty = true;
        doc.State.Properties.DestinationCountries.IsRequired = true;
      }
      // Сумма согласуемого документа.
      if (doc.TransactionAmount == null)
      {
        propertiesEmpty = true;
        doc.State.Properties.TransactionAmount.IsRequired = true;
      }
      
      if (propertiesEmpty && e.FormType != Sungero.Domain.Shared.FormType.Collection)
      {
      	// Потрогать свойство, чтоб карточка перешла в режим изменения и при сохранении подстветились обязательные поля.
      	doc.Subject = doc.Subject;
      	doc.Save();
      }
      
      // Контакт в таблице Контрагенты.
      if (isManyCounterparties && doc.Counterparties.Any(c => c.Contact == null && c.DeliveryMethod.IsRequireContactInContract == true))
      {
        propertiesEmpty = true;
        if (e.FormType != Sungero.Domain.Shared.FormType.Collection)
          e.AddError(DirRX.Solution.Contracts.Resources.EmptyContactMessage);
      }
      
      return propertiesEmpty;
    }
    
    [Public]
    public static void ExecuteSignedAction(List<Sungero.Contracts.IContractualDocument> objs)
    {
      var notUpdatedContracts = DirRX.Solution.Functions.Module.Remote.ChangeDocSigningOriginalState(objs, true);
      if (notUpdatedContracts.Count == 0)
        Dialogs.NotifyMessage(DirRX.Solution.Contracts.Resources.SelectedContractsSetStateSignedSuccessfully);
      else
        DirRX.Solution.Functions.Module.ShowActionResultDialog(objs.Count(),
                                                               notUpdatedContracts,
                                                               DirRX.Solution.Contracts.Resources.SelectedContractsSetStateSignedPartially);
    }

    [Public]
    public static void ExecuteOriginalSignedAction(List<Sungero.Contracts.IContractualDocument> objs)
    {
      // Если документ один и это многосторонний договор/ДС, то покажем предупреждающий диалог.
      if (objs.Count == 1)
      {
        var document = objs.FirstOrDefault();
        var contract = Solution.Contracts.As(document);
        var supAgreement = Solution.SupAgreements.As(document);
        if ((contract != null && contract.IsManyCounterparties == true)||
            (supAgreement != null && supAgreement.IsManyCounterparties == true))
        {
          var dialog = Dialogs.CreateTaskDialog(DirRX.Solution.Contracts.Resources.MultiContractOriginalSignedDialogText, string.Empty, MessageType.Information, DirRX.Solution.Contracts.Resources.MultiContractOriginalSignedDialogTitle);
          var signButton = dialog.Buttons.AddCustom(DirRX.Solution.Contracts.Resources.MultiContractOriginalSignedDialogSignButtonTitle);
          dialog.Buttons.AddCancel();
          var result = dialog.Show();
          
          if (result == DialogButtons.Cancel)
            return;
        }
      }
      var notUpdatedContracts = DirRX.Solution.Functions.Module.Remote.ChangeDocCounterpartySigningOriginalState(objs);
      if (notUpdatedContracts.Count == 0)
        Dialogs.NotifyMessage(DirRX.Solution.Contracts.Resources.SelectedContractsSetStateCounterpartySignedSuccessfully);
      else
        DirRX.Solution.Functions.Module.ShowActionResultDialog(objs.Count(),
                                                               notUpdatedContracts,
                                                               DirRX.Solution.Contracts.Resources.SelectedContractsSetStateCounterpartySignedPartially);
    }
    
    [Public]
    public static void ExecuteCopySignedAction(List<Sungero.Contracts.IContractualDocument> objs)
    {
      var lockedContracts = Solution.Functions.Module.Remote.ChangeDocsSigningCopyState(objs, true);
      if (lockedContracts.Count == 0)
        Dialogs.NotifyMessage(DirRX.Solution.Contracts.Resources.SelectedContractsChangeStateSignedCopySuccessfully);
      else
        DirRX.Solution.Functions.Module.ShowActionResultDialog(objs.Count(),
                                                               lockedContracts.ToList<Sungero.Contracts.IContractualDocument>(),
                                                               DirRX.Solution.Contracts.Resources.SelectedContractsChangeStateSignedCopyPartially);
    }
    
    [Public]
    public static void ExecuteSendDocWithResposibleAction(Sungero.Contracts.IContractualDocument doc)
    {
      var deadline = Functions.Module.Remote.GetDeadlineConstantValue();
      
      var dialog = Dialogs.CreateInputDialog(Sungero.Docflow.Resources.DeliverDocumentDialog, doc.Name);
      dialog.HelpCode = Constants.Module.DeliverHelpCode;
      var employee = dialog.AddSelect(Sungero.Docflow.Resources.DeliverDocumentEmployee, true, doc.ResponsibleEmployee);
      var deliveryDate = dialog.AddDate(Sungero.Docflow.Resources.DeliverDocumentDeliveryDate, true, Calendar.UserToday);
      var returnDate = dialog.AddDate(Sungero.Docflow.Resources.DeliverDocumentSheduledReturnDate, false, Calendar.UserToday.AddWorkingDays(deadline));
      var comment = dialog.AddMultilineString(Sungero.Docflow.Resources.DeliverDocumentComment, false);
      dialog.SetOnRefresh(d =>
                          {
                            if (returnDate.Value.HasValue && deliveryDate.Value.HasValue && returnDate.Value.Value < deliveryDate.Value.Value)
                              d.AddError(Sungero.Docflow.Resources.ReturnDocumentDeliveryAndReturnDate, returnDate);
                          });
      if (dialog.Show() == DialogButtons.Ok)
      {
        Functions.Contract.Remote.StartSendDocWithResposibleTask(doc, employee.Value, deliveryDate.Value.Value, returnDate.Value, comment.Value);
        Dialogs.NotifyMessage(Sungero.Docflow.Resources.DeliverDocumentNotify);
      }
    }
  }
}