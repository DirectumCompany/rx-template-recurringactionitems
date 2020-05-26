using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PartiesControl.RevisionRequest;

namespace DirRX.PartiesControl.Shared
{
  partial class RevisionRequestFunctions
  {
    public override void SetRequiredProperties()
    {
      base.SetRequiredProperties();
      _obj.State.Properties.Subject.IsRequired = false;
    }
    
    /// <summary>
    /// Установить видимость и обязательность свойств.
    /// </summary>
    public void SetEnabledProperties()
    {
      var userIncludedInRole = Users.Current.IncludedIn(Constants.Module.ArchiveResponsibleRole);
      _obj.State.Properties.BindingDocuments.Properties.Received.IsEnabled = userIncludedInRole;
      _obj.State.Properties.BindingDocuments.Properties.ReceiveDate.IsEnabled = userIncludedInRole;
      _obj.State.Properties.CaseFile.IsEnabled = userIncludedInRole;
      _obj.State.Properties.PlacedToCaseFileDate.IsEnabled = userIncludedInRole;
      _obj.State.Properties.SecurityServiceDocuments.Properties.Received.IsEnabled = userIncludedInRole;
      _obj.State.Properties.SecurityServiceDocuments.Properties.ReceiveDate.IsEnabled = userIncludedInRole;
      _obj.State.Properties.SecurityComment.IsEnabled = Solution.Employees.Current != null && Solution.Employees.Current.IncludedIn(Constants.Module.SecurityServiceRole);
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
        <Вид документа> <Контрагент>".
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        
        if (_obj.Counterparty != null)
          name += string.Format(" {0}", _obj.Counterparty.Name);
      }
      
      if (string.IsNullOrWhiteSpace(name))
        name = Sungero.Docflow.Resources.DocumentNameAutotext;
      else if (documentKind != null)
        name = documentKind.ShortName + name;
      
      name = Sungero.Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
      
      _obj.Name = Sungero.Docflow.PublicFunctions.OfficialDocument.AddClosingQuote(name, _obj);
    }
    
    /// <summary>
    /// Изменить отображение панели регистрации.
    /// </summary>
    /// <param name="needShow">Признак отображения.</param>
    /// <param name="repeatRegister">Признак повторной регистрации\изменения реквизитов.</param>
    public override void ChangeRegistrationPaneVisibility(bool needShow, bool repeatRegister)
    {
      base.ChangeRegistrationPaneVisibility(needShow, repeatRegister);
      
      var properties = _obj.State.Properties;
      properties.CaseFile.IsEnabled = true;
      properties.CaseFile.IsVisible = true;

      properties.PlacedToCaseFileDate.IsEnabled = true;
      properties.PlacedToCaseFileDate.IsVisible = true;
    }
  }
}