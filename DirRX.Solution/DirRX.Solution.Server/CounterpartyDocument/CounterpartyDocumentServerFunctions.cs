using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.CounterpartyDocument;

namespace DirRX.Solution.Server
{
  partial class CounterpartyDocumentFunctions
  {
    /// <summary>
    /// Создать документсведения о контрагенте.
    /// </summary>
    /// <returns>Документ.</returns>
    [Public, Remote]
    public static ICounterpartyDocument Create(string docKind)
    {
      var documentKind = Sungero.Docflow.DocumentKinds.GetAll().Where(d => d.Name.Trim().ToUpper() == docKind.Trim().ToUpper()).FirstOrDefault();
      if (documentKind == null)
        return null;
      
      var doc = CounterpartyDocuments.Create();
      // Для ситуации если в виде документа не настроено автоформирование имени, заполнить именем вида документа.
      doc.Name = docKind;
      doc.DocumentKind = documentKind;
      return doc;
    }
  }
}