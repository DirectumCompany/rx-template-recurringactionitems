using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.AcquaintanceTask;

namespace DirRX.Solution.Shared
{
  partial class AcquaintanceTaskFunctions
  {
    #region Скопированно из стандартной.
    
    /// <summary>
    /// Сохранить номер версии и хеш документа в задаче.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="isMainDocument">Признак главного документа.</param>/// 
    public void StoreAcquaintanceTaskVersion(Sungero.Content.IElectronicDocument document, bool isMainDocument)
    {
      var lastVersion = document.LastVersion;
      var mainDocumentVersion = _obj.AcquaintanceVersions.AddNew();
      mainDocumentVersion.IsMainDocument = isMainDocument;
      mainDocumentVersion.DocumentId = document.Id;
      if (lastVersion != null)
      {
        mainDocumentVersion.Number = lastVersion.Number;
        mainDocumentVersion.Hash = lastVersion.Body.Hash;
      }
      else
      {
        mainDocumentVersion.Number = 0;
        mainDocumentVersion.Hash = null;
      }
    }
    
    #endregion
  }
}