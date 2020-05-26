using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ShippingPackage;

namespace DirRX.ContractsCustom
{
  partial class ShippingPackageDocumentsSharedHandlers
  {

    public virtual void DocumentsDocumentChanged(DirRX.ContractsCustom.Shared.ShippingPackageDocumentsDocumentChangedEventArgs e)
    {
      if (e.NewValue != null && !Sungero.Contracts.ContractualDocuments.Equals(e.NewValue, e.OldValue))
      {
        Functions.ShippingPackage.SetSubject(ShippingPackages.As(_obj.RootEntity));
        // Комментарий заполняется автоматически комментарием для отправки из выбранного документа
        var contractDoc = DirRX.Solution.Contracts.As(e.NewValue);
        if (contractDoc != null)
        {
          _obj.Comment = contractDoc.SentNote;
          _obj.RefundRequired = contractDoc.Resending != true;
        }
        var supAgreement = DirRX.Solution.SupAgreements.As(e.NewValue);
        if (supAgreement != null)
        {
          _obj.Comment = supAgreement.SentNote;
          _obj.RefundRequired = supAgreement.Resending != true;
        }
        
      }
    }
  }

  partial class ShippingPackageDocumentsSharedCollectionHandlers
  {

    public virtual void DocumentsDeleted(Sungero.Domain.Shared.CollectionPropertyDeletedEventArgs e)
    {
      Functions.ShippingPackage.SetSubject(ShippingPackages.As(_obj));
    }

    public virtual void DocumentsAdded(Sungero.Domain.Shared.CollectionPropertyAddedEventArgs e)
    {
      _added.RefundRequired = false;
    }
  }

  partial class ShippingPackageSharedHandlers
  {

    public virtual void EmployeeChanged(DirRX.ContractsCustom.Shared.ShippingPackageEmployeeChangedEventArgs e)
    {
      // Телефон заполняется рабочим телефоном из карточки сотрудника, указанного в поле Наш контакт.
      if (e.NewValue != null && !Sungero.Company.Employees.Equals(e.NewValue, e.OldValue))
      {
        _obj.EmployeePhone = e.NewValue.Phone;
      }
    }

    public virtual void ContactChanged(DirRX.ContractsCustom.Shared.ShippingPackageContactChangedEventArgs e)
    {
      // Телефон контакта к/а  заполняется автоматически из карточки контактного лица контрагента.
      if (e.NewValue != null && !Sungero.Parties.Contacts.Equals(e.NewValue, e.OldValue))
      {
        _obj.ContactPhone = e.NewValue.Phone;
      }
    }

  }
}