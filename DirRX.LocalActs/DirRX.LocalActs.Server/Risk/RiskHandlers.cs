using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.Risk;

namespace DirRX.LocalActs
{
  partial class RiskServerHandlers
  {

    public override void BeforeSaveHistory(Sungero.Domain.HistoryEventArgs e)
    {
      base.BeforeSaveHistory(e);
      
      var stateProperties = _obj.State.Properties;
        
        // Изменение уровня.
        if (stateProperties.Level.IsChanged)
      {
        var operation = new Enumeration(Constants.Risk.LevelChange);
        var operationDetailed = operation;
        var comment = DirRX.LocalActs.Risks.Resources.LevelChangeHistoryTemplateFormat(_obj.Level.DisplayValue);
        e.Write(operation, operationDetailed, comment);
      }
      
      // Изменение описания.
      if (stateProperties.Description.IsChanged)
      {
        var operation = new Enumeration(Constants.Risk.DescriptionChange);
        var operationDetailed = operation;
        var comment = DirRX.LocalActs.Risks.Resources.DescriptionChangeHistoryTemplateFormat(_obj.Description);
        e.Write(operation, operationDetailed, comment);
      }
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var description = _obj.Description.Length > 200 ? _obj.Description.Substring(0, 200) : _obj.Description;
      var authorShortName = Sungero.Company.PublicFunctions.Employee.GetShortName(_obj.Author, DeclensionCase.Nominative, false);
      
      if (_obj.Status == LocalActs.RiskStatus.Status.Active)        
        _obj.Name = DirRX.LocalActs.Risks.Resources.RiskAutogenerateNameFormat(authorShortName, description);
      else
        _obj.Name = DirRX.LocalActs.Risks.Resources.RiskOutdatedAutogenerateNameFormat(authorShortName, description);    
      
    }
  }

}