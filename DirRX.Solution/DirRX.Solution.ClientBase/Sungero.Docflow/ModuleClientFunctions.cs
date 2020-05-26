using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Solution.Module.Docflow.Client
{
  partial class ModuleFunctions
  {
    /// <summary>
    /// Запустить отчет "Лист согласования".
    /// </summary>
    /// <param name="document">Документ.</param>
    public override void RunApprovalSheetReport(Sungero.Docflow.IOfficialDocument document)
    {
      var hasSignatures = Functions.Module.Remote.HasSignatureForApprovalSheetReport(document);
      if (!hasSignatures)
      {
        Dialogs.NotifyMessage(Sungero.Docflow.OfficialDocuments.Resources.DocumentIsNotSigned);
        return;
      }
      
      var report = DirRX.LocalActs.Reports.GetApprovalSheetReport();
      report.Document = document;
      report.Open();
    }
  }
}