using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalCheckingAssignment;

namespace DirRX.Solution.Server
{
  partial class ApprovalCheckingAssignmentFunctions
  {
    #region Cкопировано из стандартной разработки.
    /// <summary>
    /// Возвращает список подписывающих по правилу.
    /// </summary>
    /// <returns>Список тех, кто имеет право подписи.</returns>
    [Public]
    public static List<DirRX.Solution.Structures.Module.ISignatory> GetSignatories(Sungero.Docflow.IOfficialDocument document)
    {
      var signatories = new List<DirRX.Solution.Structures.Module.ISignatory>();
      
      if (document == null)
        return signatories;
      
      var settings = Sungero.Docflow.PublicFunctions.OfficialDocument.Remote.GetSignatureSettings(document);
      foreach (var setting in settings)
      {
        var priority = setting.Priority.Value;
        if (Groups.Is(setting.Recipient))
        {
          foreach (var employee in Groups.GetAllUsersInGroup(Groups.As(setting.Recipient)).Select(r => Employees.As(r)).Where(e => e != null))
          {
            var signature = DirRX.Solution.Structures.Module.Signatory.Create();
            signature.Employee = employee;
            signature.Priority = priority;
            signatories.Add(signature);
          }
        }
        else if (Employees.Is(setting.Recipient))
        {
          var signature = DirRX.Solution.Structures.Module.Signatory.Create();
          signature.Employee = Employees.As(setting.Recipient);
          signature.Priority = priority;
          signatories.Add(signature);
        }
      }
      
      signatories = signatories.Distinct().ToList();

      return signatories;
    }
    
    /// <summary>
    /// Построить сводку по документу.
    /// </summary>
    /// <returns>Сводка по документу.</returns>
    [Remote(IsPure = true)]
    public StateView GetDocumentSummary()
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      return Sungero.Docflow.PublicFunctions.Module.GetDocumentSummary(document);
    }
    #endregion
  }
}