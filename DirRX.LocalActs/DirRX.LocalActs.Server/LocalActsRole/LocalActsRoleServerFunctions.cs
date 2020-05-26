using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.LocalActsRole;
using DirRX.Solution;

namespace DirRX.LocalActs.Server
{
  partial class LocalActsRoleFunctions
  {

    /// <summary>
    /// Получить список исполнителей роли.
    /// </summary>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>Список исполнителей.</returns>
    [Remote(IsPure = true), Public]
    public List<Sungero.CoreEntities.IRecipient> GetRolePerformers(Sungero.Docflow.IApprovalTask task)
    {
      var result = new List<Sungero.CoreEntities.IRecipient>();
      var approvalTask = DirRX.Solution.ApprovalTasks.As(task);
      if (approvalTask != null && _obj.Type == DirRX.LocalActs.LocalActsRole.Type.Subscribers)
      {
        return approvalTask.Subscribers.Select(x => x.Subscriber).Cast<IRecipient>().ToList();
      }
      // Роли согласования "Руководители подразделений из регламентирующего документа" и "Руководители подразделений из отменяемого регл. документа".
      // Не включать в исполнителей ролей руководителей, в чьих подразделениях не указано Головное подразделение.
      if (approvalTask != null && (_obj.Type == DirRX.LocalActs.LocalActsRole.Type.RegDocManagers || _obj.Type == DirRX.LocalActs.LocalActsRole.Type.CRegDocManagers))
      {
        var document = approvalTask.DocumentGroup.OfficialDocuments.FirstOrDefault();
        // Согласуемый приказ в рамках задачи.
        var order = Solution.Orders.As(document);
        if (order != null)
        {
          var isRegDocManagers = _obj.Type == DirRX.LocalActs.LocalActsRole.Type.RegDocManagers;
          /* К исполнителям роли "Руководители подразделений из регламентирующего документа" будут добавлены руководители из регламентирующего документа
           * (указан в согласуемом приказе).
           * К исполнителям "Руководители подразделений из отменяемого регл. документа" будут добавлены руководители подразделений
           * из свойства "Распространяется на" всех регламентирующих документов, которые указаны в приказах, отменяющих согласуемый приказ (соединены связью "Отменяет").*/
          var approvingOrders = new List<DirRX.Solution.IOrder>();
          if (isRegDocManagers)
            approvingOrders.Add(order);
          else
            approvingOrders = order.Relations.GetRelatedFrom(DirRX.LocalActs.Constants.Module.RegulatoryNewEditionRelationName)
              .Where(x => DirRX.Solution.Orders.Is(x))
              .Select(x => DirRX.Solution.Orders.As(x))
              .ToList();
          
          foreach (var approvingOrder in approvingOrders)
          {
            if (approvingOrder.RegulatoryDocument != null)
            {
              foreach (var recepient in approvingOrder.RegulatoryDocument.SpreadsOn.Select(s => s.Recepient))
              {
                var businessUnit = Solution.BusinessUnits.As(recepient);
                if (businessUnit != null)
                  continue;
                
                var department = Sungero.Company.Departments.As(recepient);
                if (department != null)
                {
                  if (department.HeadOffice != null)
                  {
                    // Получить подразделение первого подчинения для выбранного и для всей организации.
                    var filterDepartment = department;
                    while (filterDepartment.HeadOffice.HeadOffice != null)
                      filterDepartment = filterDepartment.HeadOffice;

                    // Записать исполнителем руководителя подразделения первого подчинения.
                    if (filterDepartment.Manager != null)
                    {
                      var headManager = DirRX.Solution.Employees.As(filterDepartment.Manager).Manager;
                      if (headManager == null || ActionItems.PublicFunctions.ActionItemsRole.Remote.IsCEO(headManager))
                        headManager = Solution.Employees.As(filterDepartment.Manager);
                      result.Add(headManager);
                    }
                  }
                }
                else
                  result.Add(recepient);
              }
            }
          }
        }
      }
      
      // Руководители согласовавших с риском, находящиеся в прямом подчинении ГД.
      if (approvalTask != null && _obj.Type == DirRX.LocalActs.LocalActsRole.Type.RiskManagers)
      {
        var performers = DirRX.Solution.ApprovalAssignments.GetAll()
          .Where(a => Sungero.Workflow.Tasks.Equals(a.Task, approvalTask))
          .Where(a => a.ApprovedWithRisk == true)
          .Select(a => a.CompletedBy)
          .Distinct();
        
        foreach (var performer in performers)
        {
          var employe = Employees.As(performer);
          if (employe != null)
          {
            var manager = DirRX.ActionItems.PublicFunctions.ActionItemsRole.Remote.GetInitCEOManager(Employees.As(performer));
            if (manager != null)
            {
              if (!DirRX.ActionItems.PublicFunctions.ActionItemsRole.Remote.IsCEO(manager))
                result.Add(manager);
            }
          }
        }
      }

      return result;
    }
    
    /// <summary>
    /// Получить куратора по документу.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns></returns>
    private DirRX.Solution.IEmployee GetSupervisor(Sungero.Docflow.IOfficialDocument document)
    {
      var order = Solution.Orders.As(document);
      if (order != null && order.Supervisor != null)
        return order.Supervisor;
      
      var revisionRequest = PartiesControl.RevisionRequests.As(document);
      if (revisionRequest != null && revisionRequest.Supervisor != null)
        return revisionRequest.Supervisor;
      
      var contract = Contracts.As(document);
      if (contract != null)
        return contract.Supervisor;
      
      var supAgreement = SupAgreements.As(document);
      if (supAgreement != null)
        return supAgreement.Supervisor;
      
      var memo = ContractsCustom.MemoForPayments.As(document);
      if (memo != null)
        return memo.Supervisor;
      
      return null;
    }
    
    
    /// <summary>
    /// Получить сотрудника, исполнителя роли.
    /// </summary>
    /// <param name="task">Задача на согласование.</param>
    /// <returns>Сотрудник.</returns>
    public override Sungero.Company.IEmployee GetRolePerformer(Sungero.Docflow.IApprovalTask task)
    {
      if (_obj.Type == DirRX.LocalActs.LocalActsRole.Type.Supervisor)
      {
        var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
        if (document != null)
          return GetSupervisor(document);
      }
      
      if (task != null && _obj.Type == DirRX.LocalActs.LocalActsRole.Type.SprvisorManager)
      {
        var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
        if (document != null)
        {
          var supervisor = GetSupervisor(document);
          if (supervisor != null)
          {
            var manager = DirRX.ActionItems.PublicFunctions.ActionItemsRole.Remote.GetInitCEOManager(supervisor);
            if (manager != null)
            {
              if (DirRX.ActionItems.PublicFunctions.ActionItemsRole.Remote.IsCEO(manager))
                return supervisor;
              else
                return manager;
            }
          }
        }
      }

      return base.GetRolePerformer(task);
    }
  }
}