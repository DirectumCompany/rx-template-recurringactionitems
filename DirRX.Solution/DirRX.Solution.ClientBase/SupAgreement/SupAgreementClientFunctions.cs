using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.SupAgreement;

namespace DirRX.Solution.Client
{
  partial class SupAgreementFunctions
  {

    /// <summary>
    /// Проверка заполненности обязательных полей перед отправкой на согласование по регламенту.
    /// </summary>       
    public static bool CheckRequiredProperties(DirRX.Solution.ISupAgreement doc, Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // Проверить заполненность обязательных полей.
      bool propertiesEmpty = false;
      // Вид документа.
      if (doc.DocumentKind == null)
      {
      	propertiesEmpty = true;
      	doc.State.Properties.DocumentKind.IsRequired = true;
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
      if (doc.DaysToFinishWorks == null)
      {
      	propertiesEmpty = true;
      	doc.State.Properties.DaysToFinishWorks.IsRequired = true;
      }      
      // Действует по.
      if (doc.ValidTill == null && !doc.DocumentValidity.HasValue)
      {
      	propertiesEmpty = true;
        doc.State.Properties.ValidTill.IsRequired = true;
      }
      else
      {
        doc.State.Properties.DocumentValidity.IsRequired = false;
      }
      // Срок действия договора.
      if (doc.DocumentValidity == null && !doc.ValidTill.HasValue)
      {
        propertiesEmpty = true;
        doc.State.Properties.DocumentValidity.IsRequired = true;
      }
      else
      {
      	doc.State.Properties.ValidTill.IsRequired = false;
      }
      // Страна назначения.
      var leadingDocument = DirRX.Solution.Contracts.As(doc.LeadingDocument);
      if (doc.DestinationCountries.Count == 0 &&
          (leadingDocument.DocumentGroup == null || (leadingDocument.DocumentGroup != null && leadingDocument.DocumentGroup.DestinationCountry != DirRX.Solution.ContractCategory.DestinationCountry.NotRequired)))
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
  }
}