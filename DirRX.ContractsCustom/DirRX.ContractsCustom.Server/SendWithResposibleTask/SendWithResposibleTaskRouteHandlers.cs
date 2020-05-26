using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;
using DirRX.ContractsCustom.SendWithResposibleTask;

namespace DirRX.ContractsCustom.Server
{
  partial class SendWithResposibleTaskRouteHandlers
  {

    public virtual void Script9Execute()
    {
      var document = _obj.AttachmentContractGroup.ContractualDocuments.SingleOrDefault();
      // Отметим на вкладке выдача возврат документа.
      var tracking = document.Tracking.Where(l => (!l.ReturnDate.HasValue || Equals(l.ReturnResult, Sungero.Docflow.OfficialDocumentTracking.ReturnResult.AtControl)) &&
                                             Solution.Employees.Equals(l.DeliveredTo, _obj.Employee) &&
                                             l.DeliveryDate == _obj.DeliveryDate.Value &&
                                             l.Action == Sungero.Docflow.OfficialDocumentTracking.Action.Delivery).FirstOrDefault();

      if (tracking != null)
      {
        tracking.ReturnDate = Calendar.Now;
        tracking.ReturnResult = Sungero.Docflow.OfficialDocumentTracking.ReturnResult.Returned;
      }
      
      // Выполним задание на отправку оригинала
      var assigments = DirRX.Solution.ApprovalSendingAssignments.GetAll(a => a.Status == Sungero.Workflow.AssignmentBase.Status.InProcess &&
                                                                        DirRX.Solution.ApprovalStages.Is(a.Stage) &&
                                                                        DirRX.Solution.ApprovalStages.As(a.Stage).KindOfDocumentNeedSend == DirRX.Solution.ApprovalStage.KindOfDocumentNeedSend.Original).ToList()
        .Where(a => Sungero.Docflow.OfficialDocuments.Equals(document, a.DocumentGroup.OfficialDocuments.FirstOrDefault()));
      // Если есть задания, то выполним.
      if (assigments.Count() > 0)
      {
        foreach (var assigment in assigments)
        {
          // Выполним задание
          assigment.Complete(DirRX.Solution.ApprovalSendingAssignment.Result.Complete);
        }
      }
      else
      {
        Functions.Module.AddOriginalSendedToCounterpartyTracking(document, _obj.Employee, _obj.ReturnDate);
      }
      // Удалим статус на "Оригинал документа принят к отправке".
      ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(document,
                                                                            DirRX.ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalAcceptedForSendingGuid,
                                                                            DirRX.ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus);
      
      // Удалим статус на "Ожидает отправки контрагенту".
      ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(document,
                                                                            DirRX.ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalWaitingForSendingGuid,
                                                                            DirRX.ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus);
      
      // Изменим статус на "оригинал отправлен контрагенту".
      ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(document,
                                                                            DirRX.ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalSendedToCounterpartyGuid,
                                                                            DirRX.ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus,
                                                                            false);
    }

    public virtual void StartAssignment7(DirRX.ContractsCustom.ISendWithResposibleAssignment assignment, DirRX.ContractsCustom.Server.SendWithResposibleAssignmentArguments e)
    {
      //Получить значение срока задачи из справочника
      var valueConstMaxdeadline = ContractConstants.GetAll(c => c.Sid == Constants.Module.SendWithResposibleDeadlineGuid.ToString()).FirstOrDefault();
      
      if (valueConstMaxdeadline != null && valueConstMaxdeadline.Period.HasValue)
        assignment.Deadline = Calendar.Today.AddWorkingDays(valueConstMaxdeadline.Period.Value);
      // Тема.
      var document = _obj.AttachmentContractGroup.ContractualDocuments.SingleOrDefault();
      var subject = DirRX.ContractsCustom.SendWithResposibleTasks.Resources.AssignmentSubjectFormat(document.Name);
      assignment.Subject = Solution.PublicFunctions.Module.SubstringStringPropertyText(subject, assignment.Info.Properties.Subject);
    }

    public virtual void Script8Execute()
    {
      var document = _obj.AttachmentContractGroup.ContractualDocuments.SingleOrDefault();
      // Вернем старый способ доставки.
      document.DeliveryMethod = _obj.OldDeliveryMethod;
      // Заполним поле "Причина смены способа доставки".
      var contract = Solution.Contracts.As(document);
      var supAgreement = Solution.SupAgreements.As(document);
      if (contract != null)
        contract.ChangingShippingReason = DirRX.ContractsCustom.SendWithResposibleTasks.Resources.NotSendWithResposibleReasonTextFormat(_obj.Employee.Name);
      if (supAgreement != null)
        supAgreement.ChangingShippingReason = DirRX.ContractsCustom.SendWithResposibleTasks.Resources.NotSendWithResposibleReasonTextFormat(_obj.Employee.Name);

      // Отметим на вкладке выдача возврат документа.
      var tracking = document.Tracking.Where(l => (!l.ReturnDate.HasValue || Equals(l.ReturnResult, Sungero.Docflow.OfficialDocumentTracking.ReturnResult.AtControl)) &&
                                             Solution.Employees.Equals(l.DeliveredTo, _obj.Employee) &&
                                             l.DeliveryDate == _obj.DeliveryDate.Value &&
                                             l.Action == Sungero.Docflow.OfficialDocumentTracking.Action.Delivery).FirstOrDefault();

      if (tracking != null)
      {
        tracking.ReturnDate = Calendar.Now;
        tracking.ReturnResult = Sungero.Docflow.OfficialDocumentTracking.ReturnResult.Returned;
      }
      
      // Удалим статус на "Оригинал документа принят к отправке".
      ContractsCustom.PublicFunctions.Module.Remote.RemoveCustomContractStatus(document,
                                                                            DirRX.ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalAcceptedForSendingGuid,
                                                                            DirRX.ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus);
      // Изменим статус на "Ожидает отправки контрагенту"
      ContractsCustom.PublicFunctions.Module.Remote.SetCustomContractStatus(document,
                                                                            DirRX.ContractsCustom.PublicConstants.Module.ContractStatusGuid.OriginalWaitingForSendingGuid,
                                                                            DirRX.ContractsCustom.PublicConstants.Module.ContractStatusType.OriginalMoveStatus,
                                                                            false);
      
    }

    public virtual void StartBlock7(DirRX.ContractsCustom.Server.SendWithResposibleAssignmentArguments e)
    {
      // Установим исполнителя.
      e.Block.Performers.Add(_obj.Employee);
      // Заполним вкладку выдача.
      var document = _obj.AttachmentContractGroup.ContractualDocuments.SingleOrDefault();
      var issued = document.Tracking.AddNew();
      issued.DeliveryDate = _obj.DeliveryDate.Value;
      issued.ReturnDeadline = _obj.ReturnDate;
      issued.Note = _obj.Comment;
      issued.DeliveredTo = _obj.Employee;
      issued.IsOriginal = true;
      // TODO иначе не дает сохранить, т.к. при сохр OfficialDocument (строка 907) идет отправка задачи
      issued.ExternalLinkId = 0;

    }

    public virtual void Script6Execute()
    {
      var document = _obj.AttachmentContractGroup.ContractualDocuments.SingleOrDefault();
      // Запомним старый способ доставки на случай возврата документа.
      _obj.OldDeliveryMethod = Solution.MailDeliveryMethods.As(document.DeliveryMethod);
      // Установим способ доставки = Нарочно.
      var newDeliveryMethod = Sungero.Docflow.MailDeliveryMethods.GetAll(m => m.Sid == DirRX.ContractsCustom.PublicConstants.Module.WithRensposibleMailDeliveryMethod.ToString()).FirstOrDefault();
      document.DeliveryMethod = newDeliveryMethod;
      // Заполним поле "Причина смены способа доставки".
      var contract = Solution.Contracts.As(document);
      var supAgreement = Solution.SupAgreements.As(document);
      if (contract != null)
        contract.ChangingShippingReason = DirRX.ContractsCustom.SendWithResposibleTasks.Resources.ChangingShippingReasonTextFormat(_obj.Employee.Name);
      if (supAgreement != null)
        supAgreement.ChangingShippingReason = DirRX.ContractsCustom.SendWithResposibleTasks.Resources.ChangingShippingReasonTextFormat(_obj.Employee.Name);

    }

  }
}