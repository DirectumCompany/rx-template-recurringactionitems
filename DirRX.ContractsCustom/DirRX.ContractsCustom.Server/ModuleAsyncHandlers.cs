using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ContractsCustom.Server
{
  public class ModuleAsyncHandlers
  {

    public virtual void SendTaskToIMSResponsible(DirRX.ContractsCustom.Server.AsyncHandlerInvokeArgs.SendTaskToIMSResponsibleInvokeArgs args)
    {
      int documentId = args.DocumentId;
      var document =  Sungero.Contracts.ContractualDocuments.Get(documentId);
      
      var department = Solution.Departments.As(document.Department);
      var performer = Sungero.Company.Employees.Null;
      if (department != null)
        performer = (department.ResponsibleForSAP != null) ? department.ResponsibleForSAP : document.ResponsibleEmployee;
      else
        performer = document.ResponsibleEmployee;
      
      if (performer != null)
      {
        // Выдать права.
        if (!document.AccessRights.CanUpdate(performer))
        {
          document.AccessRights.Grant(performer, DefaultAccessRightsTypes.Change);
          document.AccessRights.Save();
        }
        
        var task = Sungero.Workflow.SimpleTasks.Create();
        task.Subject = DirRX.ContractsCustom.Resources.SendToIMSTaskSubjectFormat(document.Name);
        task.ActiveText = DirRX.ContractsCustom.Resources.SendToIMSTaskText;
        var deadlineConstant = ContractsCustom.PublicFunctions.Module.Remote.GetContractConstant(DirRX.ContractsCustom.PublicConstants.Module.SendToIMSConstantGuid.ToString());
        if (deadlineConstant != null && deadlineConstant.Unit == DirRX.ContractsCustom.ContractConstant.Unit.Day && deadlineConstant.Period.HasValue)
          task.Deadline = Calendar.Now.AddWorkingDays(deadlineConstant.Period.Value);
        else
          Logger.DebugFormat("Не найдена или не заполнено значение константы: {0}", DirRX.ContractsCustom.PublicConstants.Module.SendToIMSConstantName);
        
        task.Attachments.Add(document);
        var step = task.RouteSteps.AddNew();
        step.AssignmentType = Sungero.Workflow.SimpleTask.AssignmentType.Assignment;
        step.Performer = performer;
        task.NeedsReview = false;
        task.Start();
      }
    }

    public virtual void ConvertDocumentToPDFInsertBarcode(DirRX.ContractsCustom.Server.AsyncHandlerInvokeArgs.ConvertDocumentToPDFInsertBarcodeInvokeArgs args)
    {
      int documentId = args.DocumentId;
      var document =  Sungero.Contracts.ContractualDocuments.Get(documentId);
      Logger.DebugFormat("TryConvertToPDFInsertBarcode: start convert to PDF and insert barcode for document {0}", documentId);
      try
      {
        DirRX.ContractsCustom.PublicFunctions.Module.Remote.ConvertDocumentToPdfWithSBarcode(document);
        Logger.DebugFormat("TryConvertToPDFInsertBarcode: success  convert to PDF and insert barcode for document {0}", documentId);
      }
      catch (Exception ex)
      {
        Logger.DebugFormat("TryConvertToPDFInsertBarcode: cannot convert to PDF and insert barcode for document {0}. {1}", documentId, ex.Message);
        args.Retry = true;
      }
    }

  }
}