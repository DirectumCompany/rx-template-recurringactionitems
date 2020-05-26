using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.StandardForm;

namespace DirRX.LocalActs.Server
{
  partial class StandardFormFunctions
  {
    /// <summary>
    /// Поиск стандартной формы по виду.
    /// </summary>
    [Public, Remote(IsPure=true)]
    public static List<IStandardForm> FindStandardForms(Sungero.Docflow.IDocumentKind documentKind)
    {
      return DirRX.LocalActs.StandardForms.GetAll(x => Sungero.Docflow.DocumentKinds.Equals(x.DocumentKind, documentKind)).ToList();
    }
  }
}