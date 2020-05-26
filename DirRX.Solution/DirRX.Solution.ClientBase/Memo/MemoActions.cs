using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Memo;

namespace DirRX.Solution.Client
{
  partial class MemoCollectionActions
  {
    public override void Sign(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.Sign(e);
     
      foreach (var memo in _objs)
      {
      	if (memo.LastVersionApproved.Value == true)
      		memo.OurSignatory = Employees.Current;
      }
    }

    public override bool CanSign(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSign(e);
    }

  }


  partial class MemoActions
  {

    public override void CreateFromTemplate(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      FromTemplate(e);
    }

    public override bool CanCreateFromTemplate(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanCreateFromTemplate(e);
    }
    
    /// <summary>
    /// Создать версию документа из шаблона типовой формы.
    /// </summary>
    /// <param name="e">Аргументы.</param>
    public virtual void FromTemplate(Sungero.Domain.Client.ExecuteActionArgs e)
    {      
      var defaultStandardForm = Functions.Memo.Remote.GetStandartForm(_obj);
      var dialog = Dialogs.CreateInputDialog(Memos.Resources.CreateFromTemplate);
      var standardFormSelect = dialog.AddSelect(Memos.Resources.StandardForm, true, defaultStandardForm.Count == 1 ? defaultStandardForm.FirstOrDefault() : DirRX.LocalActs.StandardForms.Null)
        .Where(f => Sungero.Docflow.DocumentKinds.Equals(f.DocumentKind, _obj.DocumentKind) && f.Status == DirRX.LocalActs.StandardForm.Status.Active);
      dialog.Buttons.AddOkCancel();
      if (dialog.Show() == DialogButtons.Cancel)
        return;
      
      var template = standardFormSelect.Value.Template;
      using (var body = template.LastVersion.Body.Read())
      {
        var newVersion = _obj.CreateVersionFrom(body, template.AssociatedApplication.Extension);
        
        var exEntity = (Sungero.Domain.Shared.IExtendedEntity)_obj;
        exEntity.Params[Sungero.Content.Shared.ElectronicDocumentUtils.FromTemplateIdKey] = template.Id;

        _obj.Save();
        _obj.Edit();
      }
    }

  }

}