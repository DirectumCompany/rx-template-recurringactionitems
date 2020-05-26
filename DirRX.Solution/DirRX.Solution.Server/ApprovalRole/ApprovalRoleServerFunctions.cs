using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalRole;

namespace DirRX.Solution.Server
{
  partial class ApprovalRoleFunctions
  {
    public override Sungero.Company.IEmployee GetRolePerformer(Sungero.Docflow.IApprovalTask task)
    {
       var result = base.GetRolePerformer(task);
       if (_obj.Type == Sungero.Docflow.ApprovalRoleBase.Type.InitManager)
         result = DirRX.Solution.Employees.Get(task.Author.Id).Manager;
       return result;
    }
  }
}