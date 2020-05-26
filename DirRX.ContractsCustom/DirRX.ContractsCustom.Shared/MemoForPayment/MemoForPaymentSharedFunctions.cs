using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.MemoForPayment;

namespace DirRX.ContractsCustom.Shared
{
  partial class MemoForPaymentFunctions
  {

    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool isRepeatRegister)
    {
      base.ChangeDocumentPropertiesAccess(isEnabled, isRepeatRegister);
      
      _obj.State.Properties.Name.IsEnabled = false;
      
      var isHighUrgency = _obj.IsHighUrgency == true;
      _obj.State.Properties.UrgencyReason.IsEnabled = isHighUrgency;
      _obj.State.Properties.UrgencyReason.IsRequired = isHighUrgency;
      
      
      var isDelay = _obj.PaymentCondition == PaymentCondition.Delay;
      _obj.State.Properties.DaysOfDelay.IsEnabled = isDelay;
      _obj.State.Properties.DaysOfDelay.IsRequired = isDelay;
    }
    
    /// <summary>
    /// Получить номер ведущего документа.
    /// </summary>
    /// <returns>Номер документа либо пустая строка.</returns>
    /// <remarks>Переопределяем для возможности вызова в коде.</remarks>
    public override string GetLeadDocumentNumber()
    {
      return base.GetLeadDocumentNumber();
    }
    
    public override void FillName()
    {
      var documentKind = _obj.DocumentKind;
      
      if (documentKind != null && !documentKind.GenerateDocumentName.Value && _obj.Name == Sungero.Docflow.Resources.DocumentNameAutotext)
        _obj.Name = string.Empty;
      
      if (documentKind == null || !documentKind.GenerateDocumentName.Value)
        return;
      
      var name = string.Empty;
      
      /* Имя в формате:
        <Вид документа> №<номер> от <дата> "<содержание>".
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += Sungero.Docflow.OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
        
        if (_obj.RegistrationDate != null)
          name += Sungero.Docflow.OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
        
        if (!string.IsNullOrWhiteSpace(_obj.Subject))
          name += " " + _obj.Subject;
      }
      
      if (string.IsNullOrWhiteSpace(name))
        name = Sungero.Docflow.Resources.DocumentNameAutotext;
      else if (documentKind != null)
        name = documentKind.ShortName + name;
      
      name = Sungero.Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Sungero.Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(name, _obj);
    }
    
    public override void UpdateLifeCycle(Nullable<Enumeration> registrationState, Nullable<Enumeration> approvalState, Nullable<Enumeration> counterpartyApprovalState)
    {
      base.UpdateLifeCycle(registrationState, approvalState, counterpartyApprovalState);
      
      if (approvalState == Sungero.Docflow.OfficialDocument.InternalApprovalState.Signed && _obj.LifeCycleState != Sungero.Docflow.OfficialDocument.LifeCycleState.Active)
        _obj.LifeCycleState = Sungero.Docflow.OfficialDocument.LifeCycleState.Active;
    }
    
    /// <summary>
    /// Определить куратора заявки.
    /// </summary>
    /// <returns>Куратор.</returns>
    public DirRX.Solution.IEmployee GetSupervisor()
    {
      if (_obj.CoExecutor != null)
        return DirRX.ActionItems.PublicFunctions.ActionItemsRole.Remote.GetInitCEOManager(_obj.CoExecutor);
      
      if (_obj.ResponsibleEmployee != null)
        return DirRX.ActionItems.PublicFunctions.ActionItemsRole.Remote.GetInitCEOManager(DirRX.Solution.Employees.As(_obj.ResponsibleEmployee));
      
      return null;
    }
  }
}