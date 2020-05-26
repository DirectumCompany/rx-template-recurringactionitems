using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.RecordCustom.RecordCustomRole;

namespace DirRX.RecordCustom.Server
{
  partial class RecordCustomRoleFunctions
  {
    public override Sungero.Company.IEmployee GetRolePerformer(Sungero.Docflow.IApprovalTask task)
    {
      if (_obj.Type == RecordCustom.RecordCustomRole.Type.InitCEOManager)
        return ActionItems.PublicFunctions.ActionItemsRole.Remote.GetInitCEOManager(DirRX.Solution.Employees.Get(task.Author.Id));
      
      if (_obj.Type == RecordCustom.RecordCustomRole.Type.MemoAddressee)
      {
        var memo = Solution.Memos.As(task.DocumentGroup.OfficialDocuments.FirstOrDefault());
        return memo != null ? memo.Addressee : null;
      }
      
      if (_obj.Type == RecordCustom.RecordCustomRole.Type.MemoAssignee)
      {
        var memo = Solution.Memos.As(task.DocumentGroup.OfficialDocuments.FirstOrDefault());
        return memo != null ? memo.PreparedBy : null;
      }
      
      
      if (_obj.Type == RecordCustom.RecordCustomRole.Type.AssigneeManager)
      {
        var memo = Solution.Memos.As(task.DocumentGroup.OfficialDocuments.FirstOrDefault());
        var preparedBy = memo != null ? DirRX.Solution.Employees.As(memo.PreparedBy) : null;
        
        if (preparedBy == null)
          return null;
        
        if (preparedBy.Manager != null)
          return preparedBy.Manager;
        else
          return preparedBy.Department != null ? preparedBy.Department.Manager : null;
      }
      
      if (_obj.Type == RecordCustom.RecordCustomRole.Type.ApproverPrvSt)
      {
        var assigment = DirRX.Solution.ApprovalAssignments.GetAll()
          .Where(a => Equals(a.Task, task) && (a.StageNumber == task.StageNumber.GetValueOrDefault() - 1))
          .OrderByDescending(a => a.Completed).FirstOrDefault();
        return assigment != null ? Sungero.Company.Employees.As(assigment.CompletedBy) : null;
      }
      
      return base.GetRolePerformer(task);
    }
  }
}