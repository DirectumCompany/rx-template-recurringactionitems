using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;
using DirRX.ContractsCustom.ConfirmContractExecutedTask;

namespace DirRX.ContractsCustom.Server
{
  partial class ConfirmContractExecutedTaskRouteHandlers
  {

    public virtual void Script6Execute()
    {
      var document = _obj.AttachmentContractGroup.ContractualDocuments.SingleOrDefault();
      var contract = Solution.Contracts.As(document);
      if (contract != null)
        contract.LifeCycleState = DirRX.Solution.Contract.LifeCycleState.OpenSAP;
      var supAgreement = Solution.SupAgreements.As(document);
      if (supAgreement != null)
        supAgreement.LifeCycleState = DirRX.Solution.SupAgreement.LifeCycleState.OpenSAP;
      
    }

    public virtual void StartAssignment3(DirRX.ContractsCustom.IConfirmContractExecutedAssignment assignment, DirRX.ContractsCustom.Server.ConfirmContractExecutedAssignmentArguments e)
    {
      //Получить значение срока задачи из справочника
      var valueConstMaxdeadline = ContractConstants.GetAll(c => c.Sid == Constants.Module.ConfirmContractExecutedDeadlineGuid.ToString()).FirstOrDefault();
      
      if (valueConstMaxdeadline != null && valueConstMaxdeadline.Period.HasValue)
        assignment.Deadline = Calendar.Today.AddWorkingDays(valueConstMaxdeadline.Period.Value);
      // Тема.
      var document = _obj.AttachmentContractGroup.ContractualDocuments.SingleOrDefault();
      var subject = DirRX.ContractsCustom.ConfirmContractExecutedTasks.Resources.AssignmentSubjectFormat(document.Name);
      assignment.Subject = Solution.PublicFunctions.Module.SubstringStringPropertyText(subject, assignment.Info.Properties.Subject);
    }

    public virtual void StartBlock3(DirRX.ContractsCustom.Server.ConfirmContractExecutedAssignmentArguments e)
    {
      // Установим исполнителя.
      e.Block.Performers.Add(_obj.Employee);
    }

    public virtual bool Decision5Result()
    {
      var document = _obj.AttachmentContractGroup.ContractualDocuments.SingleOrDefault();
      return document.LifeCycleState != DirRX.Solution.Contract.LifeCycleState.Active;
    }

    public virtual bool Monitoring4Result()
    {
      return false;
    }

    public virtual void StartBlock4(Sungero.Workflow.Server.Route.MonitoringStartBlockEventArguments e)
    {
      //Получить Срок на повторную отправку задачи на подтверждение завершения договора.
      var remindPeriod = DirRX.ContractsCustom.PublicFunctions.ContractConstant.GetConstantPeriodSpan(Constants.Module.ContractExecutedRemindGuid);
      if (remindPeriod != TimeSpan.Zero)
      {
        e.Block.Period = remindPeriod;
        e.Block.RelativeDeadline = remindPeriod;
      }
    }
  }

}