using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.RegulatoryDocument;

namespace DirRX.LocalActs.Server
{
  partial class RegulatoryDocumentFunctions
  {

    /// <summary>
    /// Получить регламентирующий документ по id.
    /// </summary>
    /// <param name="id">Id.</param>
    /// <returns>Регламентирующий документ.</returns>
    [Remote(IsPure = true), Public]
    public static IRegulatoryDocument GetRegulatoryDocument(int id)
    {
      return RegulatoryDocuments.GetAll(r => r.Id == id).FirstOrDefault();
    }

    /// <summary>
    /// Получить максимальный порядковый номер по группе бизнес-процессов.
    /// </summary>
    /// <returns>Максимальный порядковый номер</returns>
    [Remote(IsPure = true)]
    public int GetLastBPGroupIndexNumber()
    {
      int? maxNumber = RegulatoryDocuments
        .GetAll(r => !r.Equals(_obj) &&
                BusinessProcessGroups.Equals(r.BPGroup, _obj.BPGroup) &&
                Sungero.Docflow.DocumentKinds.Equals(r.DocumentKind, _obj.DocumentKind))
        .Select(n => n.IndexNumber)
        .Max();
      
      return maxNumber.HasValue ? maxNumber.Value : 0;
    }

  }
}