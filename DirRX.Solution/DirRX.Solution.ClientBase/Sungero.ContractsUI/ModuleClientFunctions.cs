using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.PublicFunctions;

namespace DirRX.Solution.Module.ContractsUI.Client
{
  partial class ModuleFunctions
  {

    /// <summary>
    /// Создание заявки на оплату без договора.
    /// </summary>
    public virtual void CreateMemoForPayment()
    {
      var doc = PublicFunctions.Module.Remote.CreateMemoForPayment();
      doc.Show();
    }

    /// <summary>
    /// Создание документа «Заявка на разработку типовой формы договора»
    /// </summary>
    public virtual void CreateTypeForm()
    {
      var currentEmployee = DirRX.Solution.Employees.Current;
      // Проверка, находится ли сотрудник в прямом подчинении ГД.
      var isCEOManager = DirRX.Solution.PublicFunctions.Module.Remote.IsSubordinateCEOManager(currentEmployee);
      
      if (!isCEOManager && !Users.Current.IncludedIn(Roles.Administrators))
      {
        Dialogs.ShowMessage(Resources.DialogMessageText, MessageType.Warning);
        return;
      }
      var task = PublicFunctions.Module.Remote.CreateTaskTypeForm();
      task.Show();
    }

    /// <summary>
    /// Создание документа «Заявка на разработку новой формы договора».
    /// </summary>
    public virtual void CreateNewForm()
    {
      var doc = PublicFunctions.Module.Remote.CreateSimpleDocumentWithDocKind(DirRX.ContractsCustom.PublicConstants.Module.DocumentKindGuid.ApplicationNewContractFormGuid);
      doc.Show();
    }
    
    public override void CreateDocument()
    {
      var doc = PublicFunctions.Module.Remote.CreateContract();
      doc.Show();
    }

  }
}