using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ContractsApprovalRule;

namespace DirRX.Solution
{
  partial class ContractsApprovalRuleServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      //base.BeforeSave(e);
    }

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      _obj.Priority = 0;
      if (_obj.BusinessUnits.Any())
        _obj.Priority += 512;
      if (_obj.DocumentKinds.Any())
        _obj.Priority += 256;
      if (_obj.Departments.Any())
        _obj.Priority += 128;
      if (_obj.DocumentGroups.Any())
        _obj.Priority += 64;
      if (_obj.ContractFunctionality != null)
        _obj.Priority += 32;
      if (_obj.IsStandard != null)
        _obj.Priority += 16;
      if (_obj.ConditionSubcategory.Any())
        _obj.Priority += 4;
      if (_obj.IsHighUrgency != null)
        _obj.Priority += 2;
      if (_obj.TenderStep != null)
        _obj.Priority += 1;
    }
    
  }

}