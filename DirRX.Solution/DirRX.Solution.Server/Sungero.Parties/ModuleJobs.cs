using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Solution.Module.Parties.Server
{
  partial class ModuleJobs
  {

    /// <summary>
    /// Выгрузка тел документов контрагентов из КССС.
    /// </summary>
    public virtual void CreateCounterpartyDocumentByReference()
    {
      var counterpartyDocuments = DirRX.Solution.CounterpartyDocuments.GetAll(d => !d.HasVersions).ToList().Where(d => !string.IsNullOrEmpty(d.BodyKSSSLink));
      foreach (var document in counterpartyDocuments)
      {
        if (string.IsNullOrEmpty(document.BodyExtension))
        {
          Logger.DebugFormat("Для документа с ИД={0} не указано расширения", document.Id);
          continue;
        }
        
        if (Locks.GetLockInfo(document).IsLocked)
        {
          Logger.DebugFormat("Документ с ИД={0} заблокирован", document.Id);
          continue;
        }
        
        try
        {
          var body = KSSSConnector.Client.Instance.GetCounterpartyDocumentBody(document.BodyKSSSLink);
          var documentBody = document.CreateVersionFrom(body, document.BodyExtension);
          document.Save();
        }
        catch (Exception ex)
        {
          Logger.DebugFormat("Обработка документа с ИД={0} завершилась ошибкой: {1}", document.Id, ex.Message);
        }
      }
    }

  }
}