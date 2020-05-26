using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace DirRX.IntegrationLLK.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      CreateDocumentKinds();
      GrantRightToEntitiesDefault();
      GrantRightsToDocumentsAndDatabooks();
    }

    /// <summary>
    /// Назначить права ролям по умолчанию.
    /// </summary>
    public static void GrantRightToEntitiesDefault()
    {
      InitializationLogger.Debug(DirRX.IntegrationLLK.Resources.GrantRightToDatabooks);
      // Всем пользователям на справочник Подразделения на Просмотр.
      var allUsers = Roles.AllUsers;
      DepartCompanieses.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      // Роли Ответствеенные за контрагентов Полный доступ.
      var counterpartiesResponsibleRole = IntegrationLLK.Functions.Module.GetCounterpartiesResponsiblesRole();
      if (counterpartiesResponsibleRole == null)
        return;
      DepartCompanieses.AccessRights.Grant(counterpartiesResponsibleRole, DefaultAccessRightsTypes.FullAccess);
      DepartCompanieses.AccessRights.Save();
    }
    
    /// <summary>
    /// Назначить права на типы документов и типы справочников.
    /// </summary>
    public static void GrantRightsToDocumentsAndDatabooks()
    {
      InitializationLogger.Debug("Init: Grant rights on integration documents and databooks");

      // Выдать права на справочник отсутствий сотрудников.
      IntegrationLLK.AbsenceOfEmployees.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Read);
      IntegrationLLK.AbsenceOfEmployees.AccessRights.Save();
    }
    
    /// <summary>
    /// Создать виды документов.
    /// </summary>
    public static void CreateDocumentKinds()
    {
      InitializationLogger.Debug("Init: Create document kinds.");
      var notNumerable = Sungero.Docflow.DocumentKind.NumberingType.NotNumerable;
      
      // Создать вид документа "Заявка на проверку контрагента".
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(DirRX.IntegrationLLK.Resources.KsssDocumentKind,
                                                                              DirRX.IntegrationLLK.Resources.KsssDocumentKind,
                                                                              notNumerable, Sungero.Docflow.DocumentType.DocumentFlow.Inner, true, false,
                                                                              Constants.Module.CounterpartyDocumentTypeGuid, null,
                                                                              Constants.Module.KsssDocumentKindGuid);
    }
  }
}
