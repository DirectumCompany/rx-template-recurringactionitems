using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.LocalActs.Client
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Создание регламентирующих документов и приказов.
    /// </summary>
    public virtual void CreateDocument()
    {
      var document = Sungero.Docflow.InternalDocumentBases.CreateDocumentWithCreationDialog(Solution.Orders.Info,
                                                                                            DirRX.LocalActs.RegulatoryDocuments.Info,
                                                                                            DirRX.Solution.Memos.Info,
                                                                                            DirRX.Solution.IncomingLetters.Info,
                                                                                            DirRX.Solution.OutgoingLetters.Info);
      
    }

  }
}