using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ContractSettings;

namespace DirRX.ContractsCustom.Shared
{
  partial class ContractSettingsFunctions
  {
    
    /// <summary>
    /// Задание доступности и видимости свойств.
    /// </summary>
    public void SetStateProperties()
    {
      var enableAnalysisRequiredProperies = _obj.IsAnalysisRequired == true;
      _obj.State.Properties.ContractTermAnalysisRequired.IsEnabled = enableAnalysisRequiredProperies;
      _obj.State.Properties.TransactionAmountAnalysisRequired.IsEnabled = enableAnalysisRequiredProperies;
      _obj.State.Properties.CurrencyAnalysisRequired.IsEnabled = enableAnalysisRequiredProperies;
      _obj.State.Properties.ContractTermAnalysisRequired.IsRequired = enableAnalysisRequiredProperies;
      _obj.State.Properties.TransactionAmountAnalysisRequired.IsRequired = enableAnalysisRequiredProperies;
      _obj.State.Properties.CurrencyAnalysisRequired.IsRequired = enableAnalysisRequiredProperies;
      
      /*var enableLukoilApprovalProperties = _obj.LukoilApproval == true;
      _obj.State.Properties.ContractTerm.IsEnabled = enableLukoilApprovalProperties;
      _obj.State.Properties.TransactionAmount.IsEnabled = enableLukoilApprovalProperties;
      _obj.State.Properties.Currency.IsEnabled = enableLukoilApprovalProperties;*/
    }
  }
}