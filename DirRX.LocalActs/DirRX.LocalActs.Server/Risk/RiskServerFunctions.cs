using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.Risk;

namespace DirRX.LocalActs.Server
{
  partial class RiskFunctions
  {

    /// <summary>
    /// Создание риска.
    /// </summary>
    /// <param name="approvalAssignment">Задание на согласование.</param>
    /// <returns>Риск.</returns>
    [Remote, Public]
    public static DirRX.LocalActs.IRisk CreateRisk(Solution.IApprovalAssignment approvalAssignment)
    {
      var risk = LocalActs.Risks.Create();
      risk.Level = approvalAssignment.RiskLevel;
      risk.Description = approvalAssignment.RiskDescription;
      risk.Author = DirRX.MailAdapterSolution.Employees.As(approvalAssignment.Performer);
      risk.AccessRights.Grant(approvalAssignment.Task.StartedBy, DefaultAccessRightsTypes.Read);
      if (Solution.ApprovalTasks.As(approvalAssignment.Task).Signatory != null)
        risk.AccessRights.Grant(Solution.ApprovalTasks.As(approvalAssignment.Task).Signatory, DefaultAccessRightsTypes.Read);
      var order = Solution.Orders.As(approvalAssignment.DocumentGroup.OfficialDocuments.FirstOrDefault());
      if (order != null && order.Supervisor != null && !risk.AccessRights.CanUpdate(order.Supervisor))
        risk.AccessRights.Grant(order.Supervisor, DefaultAccessRightsTypes.Change);
      
      // Выдать права куратору договорного документа
      var contract = Solution.Contracts.As(approvalAssignment.DocumentGroup.OfficialDocuments.FirstOrDefault());
      var supAgreement = Solution.SupAgreements.As(approvalAssignment.DocumentGroup.OfficialDocuments.FirstOrDefault());
      var memoForPayment = DirRX.ContractsCustom.MemoForPayments.As(approvalAssignment.DocumentGroup.OfficialDocuments.FirstOrDefault());
      
      Sungero.Company.IEmployee supervisor = null;
      
      if (contract != null)
        supervisor = contract.Supervisor;
      
      if (supAgreement != null)
        supervisor = supAgreement.Supervisor;
      
      if (memoForPayment != null)
        supervisor =  memoForPayment.Supervisor;
      
      if (supervisor != null && !risk.AccessRights.CanUpdate(supervisor))
        risk.AccessRights.Grant(supervisor, DefaultAccessRightsTypes.Read);
      
      // Выдать права всем вышестоящим руководителям согласующего, обозначившему риск
      LocalActs.PublicFunctions.Module.GrantAccesRightsForManagers(risk, risk.Author, DefaultAccessRightsTypes.Change);
      risk.AccessRights.Save();
      
      return risk;
    }
  }
}