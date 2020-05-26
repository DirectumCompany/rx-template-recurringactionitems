using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.DocumentRegister;


namespace DirRX.Solution.Shared
{
  partial class DocumentRegisterFunctions
  {
    /// <summary>
    /// Проверить регистрационный номер на валидность.
    /// </summary>
    /// <param name="registrationDate">Дата регистрации.</param>
    /// <param name="registrationNumber">Номер регистрации.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="leadDocNumber">Номер ведущего документа.</param>
    /// <returns>Пустая строка.</returns>
    /// <remarks>Умышленно идем на удаление проверки соответствию формату номера.</remarks>
    public override string CheckRegistrationNumberFormat(DateTime? registrationDate, string registrationNumber,
                                                        string departmentCode, string businessUnitCode, string caseFileIndex, string docKindCode, string counterpartyCode,
                                                        string leadDocNumber, bool searchCorrectingPostfix)
    {
      // Умышленно идем на удаление проверки соответствию формату номера.
      return string.Empty;
    }
    
    /// <summary>
    /// Получить пример номера журнала в соответствии с форматом.
    /// </summary>
    /// <returns>Пример номера журнала.</returns>
    public override string GetValueExample()
    {
      var registrationIndexExample = "1";
      var leadingDocNumberExample = "1";
      var departmentCodeExample = DocumentRegisters.Resources.NumberFormatDepartmentCode;
      var caseFileIndexExample = DocumentRegisters.Resources.NumberFormatCaseFile;
      var businessUnitCodeExample = DocumentRegisters.Resources.NumberFormatBUCode;
      var docKindCodeExample = DocumentRegisters.Resources.NumberFormatDocKindCode;
      var counterpartyCodeExample = DocumentRegisters.Resources.NumberFormatCounterpartyCode;
      var territoryCode = Constants.Docflow.DocumentRegister.TerritoryCodeLlkInt;
      
      return GenerateRegistrationNumberCustom(Calendar.UserNow, leadingDocNumberExample, registrationIndexExample, departmentCodeExample, businessUnitCodeExample, caseFileIndexExample, docKindCodeExample, counterpartyCodeExample, "0", territoryCode);
      
    }
    
    /// <summary>
    /// Генерировать префикс и постфикс регистрационного номера документа.
    /// </summary>
    /// <param name="date">Дата.</param>
    /// <param name="leadingDocumentNumber">Ведущий документ.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="counterpartyCodeIsMetasymbol">Признак того, что код контрагента нужен в виде метасимвола.</param>
    /// <param name="territory">Код территории договорного документа.</param>
    /// <returns>Сгенерированный регистрационный номер.</returns>
    public Sungero.Docflow.Structures.DocumentRegister.RegistrationNumberParts GenerateRegistrationNumberPrefixAndPostfixCustom(DateTime date, string leadingDocumentNumber, string departmentCode, string businessUnitCode, string caseFileIndex, string docKindCode, string counterpartyCode, bool counterpartyCodeIsMetasymbol, string territory)
    {
      var prefix = string.Empty;
      var postfix = string.Empty;
      var numberElement = string.Empty;
      var orderedNumberFormatItems = _obj.NumberFormatItems.OrderBy(f => f.Number);
      foreach (var element in orderedNumberFormatItems)
      {
        if (element.Element == Sungero.Docflow.DocumentRegisterNumberFormatItems.Element.Number)
        {
          prefix = numberElement;
          numberElement = string.Empty;
        }
        else if (element.Element == Sungero.Docflow.DocumentRegisterNumberFormatItems.Element.Log)
          numberElement += _obj.Index;
        else if (element.Element == Sungero.Docflow.DocumentRegisterNumberFormatItems.Element.RegistrPlace && _obj.RegistrationGroup != null)
          numberElement += _obj.RegistrationGroup.Index;
        else if (element.Element == Sungero.Docflow.DocumentRegisterNumberFormatItems.Element.Year2Place)
          numberElement += date.ToString("yy");
        else if (element.Element == Sungero.Docflow.DocumentRegisterNumberFormatItems.Element.Year4Place)
          numberElement += date.ToString("yyyy");
        else if (element.Element == Sungero.Docflow.DocumentRegisterNumberFormatItems.Element.Month)
          numberElement += date.ToString("MM");
        else if (element.Element == Sungero.Docflow.DocumentRegisterNumberFormatItems.Element.Quarter)
          numberElement += ToQuarterString(date);
        else if (element.Element == Sungero.Docflow.DocumentRegisterNumberFormatItems.Element.LeadingNumber)
          numberElement += leadingDocumentNumber;
        else if (element.Element == Sungero.Docflow.DocumentRegisterNumberFormatItems.Element.DepartmentCode)
          numberElement += departmentCode;
        else if (element.Element == Sungero.Docflow.DocumentRegisterNumberFormatItems.Element.BUCode)
          numberElement += businessUnitCode;
        else if (element.Element == Sungero.Docflow.DocumentRegisterNumberFormatItems.Element.CaseFile)
          numberElement += caseFileIndex;
        else if (element.Element == Sungero.Docflow.DocumentRegisterNumberFormatItems.Element.DocKindCode)
          numberElement += docKindCode;
        else if (element.Element == Sungero.Docflow.DocumentRegisterNumberFormatItems.Element.CPartyCode && !counterpartyCodeIsMetasymbol)
          numberElement += counterpartyCode;
        else if (element.Element == Sungero.Docflow.DocumentRegisterNumberFormatItems.Element.CPartyCode && counterpartyCodeIsMetasymbol)
          numberElement += DocumentRegisters.Resources.NumberFormatCounterpartyCode;
        else if (element.Element == Solution.DocumentRegisterNumberFormatItems.Element.Territory)
          numberElement += territory;
        
        // Не добавлять разделитель, для пустого кода контрагента.
        if (string.IsNullOrEmpty(counterpartyCode) || counterpartyCodeIsMetasymbol)
        {
          // Разделитель после пустого кода контрагента.
          if (element.Element == Sungero.Docflow.DocumentRegisterNumberFormatItems.Element.CPartyCode)
            continue;
          
          // Разделитель до кода контрагента, если код контрагента последний в номере.
          var nextElement = orderedNumberFormatItems.Where(f => f.Number > element.Number).FirstOrDefault();
          var lastElement = orderedNumberFormatItems.LastOrDefault();
          if (nextElement != null && nextElement.Element == Sungero.Docflow.DocumentRegisterNumberFormatItems.Element.CPartyCode &&
              lastElement != null && lastElement.Number == nextElement.Number)
            continue;
        }
        
        // Добавить разделитель.
        numberElement += element.Separator;
      }
      
      postfix = numberElement;
      return Sungero.Docflow.Structures.DocumentRegister.RegistrationNumberParts.Create(prefix, postfix);
    }
    
    /// <summary>
    /// Генерировать регистрационный номер для документа.
    /// </summary>
    /// <param name="date">Дата регистрации.</param>
    /// <param name="index">Номер.</param>
    /// <param name="leadingDocumentNumber">Номер ведущего документа.</param>
    /// <param name="departmentCode">Код подразделения.</param>
    /// <param name="businessUnitCode">Код нашей организации.</param>
    /// <param name="caseFileIndex">Индекс дела.</param>
    /// <param name="docKindCode">Код вида документа.</param>
    /// <param name="counterpartyCode">Код контрагента.</param>
    /// <param name="indexLeadingSymbol">Символ для заполнения ведущих значений индекса в номере.</param>
    /// <param name="territory">Код территории договорного документа.</param>
    /// <returns>Сгенерированный регистрационный номер.</returns>
    public string GenerateRegistrationNumberCustom(DateTime date, string index, string leadingDocumentNumber,
                                                     string departmentCode, string businessUnitCode, string caseFileIndex,
                                                     string docKindCode, string counterpartyCode, string indexLeadingSymbol, string territory)
    {
      // Сформировать регистрационный индекс.
      var registrationNumber = string.Empty;
      if (index.Length < _obj.NumberOfDigitsInNumber)
        registrationNumber = string.Concat(Enumerable.Repeat(indexLeadingSymbol, (_obj.NumberOfDigitsInNumber - index.Length) ?? 0));
      registrationNumber += index;
      
      // Сформировать регистрационный номер.
      var prefixAndPostfix = Functions.DocumentRegister.GenerateRegistrationNumberPrefixAndPostfixCustom(_obj, date, leadingDocumentNumber, departmentCode,
                                                                                                         businessUnitCode, caseFileIndex, docKindCode, counterpartyCode, false, territory);
      return string.Format("{0}{1}{2}", prefixAndPostfix.Prefix, registrationNumber, prefixAndPostfix.Postfix);
    }
    
  }
}