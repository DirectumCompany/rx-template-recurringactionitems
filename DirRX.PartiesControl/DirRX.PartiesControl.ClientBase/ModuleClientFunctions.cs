using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PartiesControl.Client
{
  public class ModuleFunctions
  {

    /// <summary>
    /// Отладочная функция для удаления версий документов контрагентов.
    /// </summary>
    public static void ClearCounterpartyDoc()
    {
      var allDoc = Functions.Module.Remote.GetAllCounterpartyDoc();
      
      if (allDoc.Any())
      {
        Logger.Debug("Начало процесса удаления версий.");
        
        int blockLength = 0;
        int docCount = allDoc.Count();
        var docBlock = new List<DirRX.Solution.ICounterpartyDocument>();
        foreach (var doc in allDoc)
        {
          blockLength++;
          docBlock.Add(doc);
          if ((docBlock.Count() == 20) || (blockLength == docCount))
          {
            Logger.Debug("Начало обработки блока.");
            Functions.Module.Remote.ClearVerCounterpartyDoc(docBlock);
            docBlock.Clear();
            Logger.Debug("Конец обработки блока.");
          }
        }
        
        Logger.Debug("Завершение удаления версий.");
      }
      
    }

  }
}