using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;
using DirRX.ContractsCustom.ControlReturnTask;

namespace DirRX.ContractsCustom.Server
{
  partial class ControlReturnTaskRouteHandlers
  {

    public virtual void StartAssignment3(DirRX.ContractsCustom.IControlReturnAssignment assignment, DirRX.ContractsCustom.Server.ControlReturnAssignmentArguments e)
    {
      //Получить значение срока задачи из справочника
      var valueConstMaxdeadline = ContractConstants.GetAll(c => c.Sid == Constants.Module.OriginalsControlTaskDeadlineConstantGuid.ToString()).FirstOrDefault();
      
      if (valueConstMaxdeadline.Period.HasValue)
        assignment.Deadline = Calendar.Today.AddWorkingDays(valueConstMaxdeadline.Period.Value);
    }

    public virtual void StartAssignment5(DirRX.ContractsCustom.ICreateCancelOfferAssignment assignment, DirRX.ContractsCustom.Server.CreateCancelOfferAssignmentArguments e)
    {
      var subject = string.Format(DirRX.ContractsCustom.ControlReturnTasks.Resources.CreateCancelOfferContract, _obj.AttachmentContractGroup.ContractualDocuments.SingleOrDefault().Name);
      assignment.Subject = Solution.PublicFunctions.Module.SubstringStringPropertyText(subject, assignment.Info.Properties.Subject);
      //Получить значение срока задачи из справочника
      var valueConstMaxdeadline = ContractConstants.GetAll(c => c.Sid == Constants.Module.OriginalsControlTaskDeadlineConstantGuid.ToString()).FirstOrDefault();
      
      if (valueConstMaxdeadline.Period.HasValue)
        assignment.Deadline = Calendar.Today.AddWorkingDays(valueConstMaxdeadline.Period.Value);
    }

    public virtual void StartBlock5(DirRX.ContractsCustom.Server.CreateCancelOfferAssignmentArguments e)
    {
      // Получение участников роли.
      var ECDResponsibleGroup = Roles.GetAll().Where(n => n.Sid == Constants.Module.RoleGuid.ECDCancelOfferRole).SingleOrDefault();
      if (ECDResponsibleGroup != null && ECDResponsibleGroup.RecipientLinks.Any())
      {
        var recepientRole = Users.As(ECDResponsibleGroup.RecipientLinks.FirstOrDefault().Member);
        e.Block.Performers.Add(recepientRole);
      }
    }

    public virtual void Script4Execute()
    {
      // Получить документ и изменить статус согласования с контрагентом и состояние.
      var contract = _obj.AttachmentContractGroup.ContractualDocuments.SingleOrDefault();
      contract.ExternalApprovalState = DirRX.Solution.Contract.ExternalApprovalState.Unsigned;
      contract.LifeCycleState = Sungero.Contracts.Contract.LifeCycleState.Obsolete;
      
      // Получить и прекратить задачу.
      var approvalTasks = DirRX.Solution.ApprovalTasks.GetAll(t => t.AttachmentDetails.Any(g => g.GroupId == DirRX.LocalActs.PublicConstants.Module.DocumentGroupApprovalTask && g.AttachmentId == contract.Id));

      if (approvalTasks.Any())
      {
        var approvalTask = approvalTasks.FirstOrDefault();
        approvalTask.AbortingReason = !string.IsNullOrWhiteSpace(_obj.ActiveText) ? _obj.ActiveText : string.Format(DirRX.ContractsCustom.ControlReturnTasks.Resources.ReturnExpired, _obj.PerformerTask);
        approvalTask.Abort();
      }
    }

    public virtual void StartBlock3(DirRX.ContractsCustom.Server.ControlReturnAssignmentArguments e)
    {
      // PerformerTask скрытое свойство задачи, которое заполняется из ФП ControlReturnJob при создании задачи.
      e.Block.Performers.Add(_obj.PerformerTask);
    }

  }
}