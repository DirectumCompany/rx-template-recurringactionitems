using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.DocumentRegister;

namespace DirRX.Solution.Server
{
  partial class DocumentRegisterFunctions
  {
    /// <summary>
    /// Получить следующий регистрационный номер.
    /// </summary>
    /// <param name="date">Дата регистрации.</param>
    /// <param name="leadDocumentId">ID ведущего документа.</param>
    /// <param name="document">Документ.</param>
    /// <param name="leadingDocumentNumber">Номер ведущего документа.</param>
    /// <param name="departmentId">ИД подразделения.</param>
    /// <param name="businessUnitId">ID НОР.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="indexLeadingSymbol">Ведущий символ индекса.</param>
    /// <returns>Регистрационный номер.</returns>
    [Remote(IsPure = true)]
    public override string GetNextNumber(DateTime date, int leadDocumentId, Sungero.Docflow.IOfficialDocument document, string leadingDocumentNumber,
                                         int departmentId, int businessUnitId, string caseFileIndex, string docKindCode, string indexLeadingSymbol)
    {
      var index = base.GetNextIndex(date, leadDocumentId, departmentId, businessUnitId, document).ToString();
      
      var departmentCode = string.Empty;
      if (departmentId != 0)
      {
        var department = Departments.Get(departmentId);
        if (department != null)
          departmentCode = department.Code ?? string.Empty;
      }
      
      var businessUnitCode = string.Empty;
      if (businessUnitId != 0)
      {
        var businessUnit = BusinessUnits.Get(businessUnitId);
        if (businessUnit != null)
          businessUnitCode = businessUnit.Code ?? string.Empty;
      }
      
      var counterpartyCode = Sungero.Docflow.PublicFunctions.OfficialDocument.GetCounterpartyCode(document);
      
      //если документ договорной, то получим территорию
      var territory = Functions.DocumentRegister.GetDocumentTerritory(document);
      
      var number = Functions.DocumentRegister.GenerateRegistrationNumberCustom(_obj, date, index, leadingDocumentNumber,
                                                                               departmentCode, businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, indexLeadingSymbol, territory);
      return number;
    }
    
    /// <summary>
    /// Получить и запомнить префикс и постфикс регистрационного номера.
    /// </summary>
    /// <param name="document"> Документ.</param>
    /// <param name="e"> Аргументы события документа "До сохранения"</param>
    /// <param name="leadingDocumentNumber"> Номер ведущего документа. Получается функцией GetLeadDocumentNumber() документа.</param>
    /// <returns>Пустая строка. Если длина автогенерируемого номера превышает размер свойства, то возвращается сообщение об ошибке.</returns>
    /// <description>Используется в событии документа "До сохранения" после вызова базового события.</description>
    [Public]
    public static string UpdateDocumentPrefixAndPostfix(Sungero.Docflow.IOfficialDocument document, Sungero.Domain.Shared.BaseEventArgs e, string leadingDocumentNumber)
    {
      // Получить и запомнить префикс и постфикс регистрационного номера.
      if (string.IsNullOrEmpty(document.RegistrationNumber) && document.RegistrationDate != null && document.DocumentRegister != null)
      {
        // Почистим предыдущие значения.
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.RegistrationNumberPrefix, string.Empty);
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.RegistrationNumberPostfix, string.Empty);
        // Данные для валидации рег. номера.
        var depCode = document.Department != null ? document.Department.Code : string.Empty;
        var bunitCode = document.BusinessUnit != null ? document.BusinessUnit.Code : string.Empty;
        var caseIndex = document.CaseFile != null ? document.CaseFile.Index : string.Empty;
        var kindCode = document.DocumentKind != null ? document.DocumentKind.Code : string.Empty;
        var counterpartyCode = Sungero.Docflow.PublicFunctions.OfficialDocument.GetCounterpartyCode(document);
        var documentRegister = DirRX.Solution.DocumentRegisters.As(document.DocumentRegister);
        var territory = Functions.DocumentRegister.GetDocumentTerritory(document);
        var prefixAndPostfix = Functions.DocumentRegister.GenerateRegistrationNumberPrefixAndPostfixCustom(documentRegister, document.RegistrationDate.Value, leadingDocumentNumber,
                                                                                                           depCode, bunitCode, caseIndex, kindCode, counterpartyCode, false, territory);
        // проверить длину автогенерируемого номера.
        if (document.DocumentRegister.NumberOfDigitsInNumber + prefixAndPostfix.Prefix.Length + prefixAndPostfix.Postfix.Length > document.Info.Properties.RegistrationNumber.Length)
        {
          var errorMessage = string.Format(Sungero.Docflow.Resources.PropertyLengthError, document.Info.Properties.RegistrationNumber.LocalizedName, document.Info.Properties.RegistrationNumber.Length);
          errorMessage = string.Format("{0} {1}", errorMessage, Sungero.Parties.Resources.ContactAdministrator);
          return errorMessage;
        }
        
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.RegistrationNumberPrefix, prefixAndPostfix.Prefix);
        e.Params.AddOrUpdate(Sungero.Docflow.Constants.OfficialDocument.RegistrationNumberPostfix, prefixAndPostfix.Postfix);
      }
      return string.Empty;
    }
    
    /// <summary>
    /// Код территории договорного документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Код территории договорного документа. Если документ не договорной, то вернется пустая строка.</returns>
    public static string GetDocumentTerritory(Sungero.Docflow.IOfficialDocument document)
    {
      //если документ договорной, то получим территорию
      var territory = new Enumeration();
      var contractDoc = DirRX.Solution.Contracts.As(document);
      if (contractDoc != null)
        territory = contractDoc.Territory.Value;
      var supAgreement = DirRX.Solution.SupAgreements.As(document);
      if (supAgreement != null)
        territory = supAgreement.Territory.Value;
      var memoForPayment = DirRX.ContractsCustom.MemoForPayments.As(document);
      if (memoForPayment != null)
        territory = memoForPayment.Territory.Value;
      
      var territoryCode = string.Empty;
      
      if (territory == DirRX.Solution.SupAgreement.Territory.LlkIntTyumen)
        territoryCode = Constants.Docflow.DocumentRegister.TerritoryCodeLlkIntTyumen;
      
      if (territory == DirRX.Solution.SupAgreement.Territory.TppPerm)
        territoryCode = Constants.Docflow.DocumentRegister.TerritoryCodeTppPerm;
      
      if (territory == DirRX.Solution.SupAgreement.Territory.TppVolgograd)
        territoryCode = Constants.Docflow.DocumentRegister.TerritoryCodeTppVolgograd;
      
      if (territory == DirRX.Solution.SupAgreement.Territory.LlkInt)
        territoryCode = Constants.Docflow.DocumentRegister.TerritoryCodeLlkInt;
      
      return territoryCode;
    }

  }
}