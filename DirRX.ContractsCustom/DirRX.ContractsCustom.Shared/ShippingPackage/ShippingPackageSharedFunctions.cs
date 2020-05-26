using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ShippingPackage;
using System.Text;

namespace DirRX.ContractsCustom.Shared
{
  partial class ShippingPackageFunctions
  {

    /// <summary>
    /// Заполнение поля Содержание регистрационными номерами договорных документов.
    /// </summary>
    public void SetSubject()
    {
      var subjectText = new StringBuilder();
      var delimiter = string.Empty;
      foreach (var document in _obj.Documents)
      {
        // Если документ еще не зарегистрирован, то берется его содержание.
        var docData = string.IsNullOrEmpty(document.Document.RegistrationNumber) ? document.Document.Subject : document.Document.RegistrationNumber;
        subjectText.Append(delimiter + docData);
        if (subjectText.Length > 0)
          delimiter = Constants.ShippingPackage.SubjectTextDelimiter;
      }
      _obj.Subject = subjectText.ToString();
    }

  }
}