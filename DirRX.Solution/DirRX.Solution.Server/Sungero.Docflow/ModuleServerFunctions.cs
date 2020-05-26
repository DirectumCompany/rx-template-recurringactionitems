using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Solution.Module.Docflow.Server
{
  partial class ModuleFunctions
  {
    #region Скопировано из стандартной разработки
    /// <summary>
    /// Проверить наличие согласующих или утверждающих подписей на документе.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если есть хоть одна подпись для отображения в отчете.</returns>
    [Remote(IsPure = true), Public]
    public static bool HasSignatureForApprovalSheetReport(Sungero.Docflow.IOfficialDocument document)
    {
      var setting = Sungero.Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(null);
      var showNotApproveSign = setting != null ? setting.ShowNotApproveSign == true : false;
      
      return Signatures.Get(document).Any(s => (showNotApproveSign || s.SignatureType != SignatureType.NotEndorsing) && s.IsExternal != true);
    }
    #endregion
    
    /// <summary>
    /// Выдать права на документ.
    /// </summary>
    /// <param name="documentId">ИД документа.</param>
    /// <param name="ruleId">ИД правила выдачи прав.</param>
    /// <param name="grantRightChildDocument">Выдавать права дочерним докментам.</param>
    /// <returns>True, если права были успешно выданы.</returns>
    public override bool GrantRightsToDocument(int documentId, int ruleId, bool grantRightChildDocument)
    {      
      var allRules = AccessRightsRules.GetAll(s => s.Status == Sungero.Docflow.AccessRightsRule.Status.Active).ToList();
      if (!allRules.Any())
      {
        Logger.DebugFormat("TryGrantRightsByRule: no rights for document {0}", documentId);
        return true;
      }

      var document = Sungero.Docflow.OfficialDocuments.GetAll(d => d.Id == documentId).FirstOrDefault();
      if (document == null)
      {
        Logger.DebugFormat("TryGrantRightsByRule: no document with id {0}", documentId);
        return true;
      }

      var rule = AccessRightsRules.GetAll(r => r.Id == ruleId).FirstOrDefault();
      if (rule == null && ruleId != 0)
      {
        Logger.DebugFormat("TryGrantRightsByRule: no rights with id {0}", ruleId);
        return true;
      }

      // Права на документ.
      var documentRules = GetAvailableRulesForDocument(document, allRules);
      
      if (rule != null)
      {
        if (documentRules.Contains(rule) || grantRightChildDocument == false)
          documentRules = new List<IAccessRightsRule>() { rule };
        else
          return true;
      }

      foreach (var documentRule in documentRules)
      {
        if (!TryGrantRightsToDocument(document, documentRule))
          return false;

        // Права на дочерние документы от ведущего.
        if (documentRule.GrantRightsOnLeadingDocument == true && grantRightChildDocument == true)
        {
          var relatedDocuments = document.Relations.GetRelated(LocalActs.PublicConstants.Module.RegulatoryOrderRelationName);

          foreach (var relatedDocument in relatedDocuments)
          {
            var addenda = Sungero.Docflow.OfficialDocuments.As(relatedDocument);
            if (addenda == null)
              continue;

            Sungero.Docflow.PublicFunctions.Module.CreateGrantAccessRightsToDocumentAsyncHandler(addenda.Id, documentRule.Id, false);
            Logger.DebugFormat("TryGrantRightsByRule: create addenda document queue for document {0}, rule {1}", addenda.Id, documentRule.Id);
          }        
        }
      }

      return true;
    }
    
    /// <summary>
    /// Получить из списка правил подходящие для документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="rules">Правила.</param>
    /// <returns>Подходящие правила.</returns>
    public static List<IAccessRightsRule> GetAvailableRulesForDocument(Sungero.Docflow.IOfficialDocument document, List<IAccessRightsRule> rules)
    {      
      var documentGroup = Sungero.Docflow.PublicFunctions.OfficialDocument.GetDocumentGroup(document);
      
      rules = rules
        .Where(s => s.Status == Sungero.Docflow.AccessRightsRule.Status.Active)
        .Where(s => !s.DocumentKinds.Any() || s.DocumentKinds.Any(k => Equals(k.DocumentKind, document.DocumentKind)))
        .Where(s => !s.BusinessUnits.Any() || s.BusinessUnits.Any(u => Equals(u.BusinessUnit, document.BusinessUnit)))
        .Where(s => !s.Departments.Any() || s.Departments.Any(k => Equals(k.Department, document.Department)))
        .Where(s => !s.DocumentGroups.Any() || s.DocumentGroups.Any(k => Equals(k.DocumentGroup, documentGroup)))
        .ToList();
      
      // Обработка признака "Подписан".
      if (document.InternalApprovalState != Sungero.Docflow.OfficialDocument.InternalApprovalState.Signed)
        rules = rules.Where(s => !s.IsSigned.GetValueOrDefault()).ToList();
      
      // Обработка признака "Зарегистрирован".
      if (document.RegistrationState != Sungero.Docflow.OfficialDocument.RegistrationState.Registered)
        rules = rules.Where(s => !s.IsRegistered.GetValueOrDefault()).ToList();
      Logger.DebugFormat("Count = {0}", rules.Count);
      return rules;
    }
  }
}