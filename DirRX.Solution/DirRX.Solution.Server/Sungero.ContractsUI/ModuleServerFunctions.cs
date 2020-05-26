using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Solution.Module.ContractsUI.Server
{
  partial class ModuleFunctions
  {

    /// <summary>
    /// Создание простой документ с указанным видом.
    /// </summary>
    /// <param name="docKindGuid">Guid вида документа.</param>
    /// <returns>Созданный документ.</returns>
    [Public, Remote]
    public static Sungero.Docflow.ISimpleDocument CreateSimpleDocumentWithDocKind(Guid docKindGuid)
    {
      var docKind = Sungero.Docflow.PublicFunctions.DocumentKind.GetNativeDocumentKind(docKindGuid);
      var doc = Sungero.Docflow.SimpleDocuments.Create();
      doc.DocumentKind = docKind;
      return doc;
    }
    
    /// <summary>
    /// Создание задачи с документом «Заявка на разработку типовой формы договора»
    /// </summary>
    [Public, Remote]
    public Sungero.Workflow.ISimpleTask CreateTaskTypeForm()
    {
      var role = Roles.GetAll().SingleOrDefault(n => n.Sid == ContractsCustom.PublicConstants.Module.RoleGuid.DPOEmployeRole);
      
      var doc = PublicFunctions.Module.Remote.CreateSimpleDocumentWithDocKind(DirRX.ContractsCustom.PublicConstants.Module.DocumentKindGuid.StandardContractFormGuid);
      doc.Name = DirRX.ContractsCustom.Resources.StandardContractForm;
      doc.AccessRights.Grant(role, DefaultAccessRightsTypes.Change);
      doc.Save();
      
      var newTask = Sungero.Workflow.SimpleTasks.Create();
      newTask.Subject = DirRX.Solution.Module.ContractsUI.Resources.TaskTypeFormSubject;
      
      var deadlineConstant = ContractsCustom.PublicFunctions.Module.Remote.GetContractConstant(DirRX.ContractsCustom.PublicConstants.Module.TermDevelopNewContractFormGuid.ToString());
      if (deadlineConstant != null && deadlineConstant.Unit == DirRX.ContractsCustom.ContractConstant.Unit.Day &&
         deadlineConstant.Period.HasValue)
        newTask.Deadline = Calendar.Now.AddWorkingDays(deadlineConstant.Period.Value);
      else
        Logger.DebugFormat("Не найдена или не заполнено значение константы: {0}", DirRX.ContractsCustom.PublicConstants.Module.TermDevelopNewContractFormName);
        
      var step = newTask.RouteSteps.AddNew();
      step.AssignmentType = Sungero.Workflow.SimpleTask.AssignmentType.Assignment;
      step.Performer = role;
      
      newTask.Attachments.Add(doc);
      newTask.Save();

      return newTask;
    }
    
    /// <summary>
    /// Создание заявки на оплату без договора.
    /// </summary>
    /// <returns>Заявка на оплату без договора.</returns>
    [Public, Remote]
    public DirRX.ContractsCustom.IMemoForPayment CreateMemoForPayment()
    {
      var doc = DirRX.ContractsCustom.MemoForPayments.Create();
      return doc;
    }
    
    /// <summary>
    /// Созданиедоговора.
    /// </summary>
    /// <returns>Договор.</returns>
    [Public, Remote]
    public Solution.IContract CreateContract()
    {
      var doc = Solution.Contracts.Create();
      return doc;
    }
  }
}