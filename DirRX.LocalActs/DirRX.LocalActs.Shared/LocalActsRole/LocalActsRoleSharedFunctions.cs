using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.LocalActsRole;

namespace DirRX.LocalActs.Shared
{
  partial class LocalActsRoleFunctions
  {
    public override List<Sungero.Docflow.IDocumentKind> Filter(List<Sungero.Docflow.IDocumentKind> kinds)
    {
      var query = base.Filter(kinds);
      
      if (_obj.Type == DirRX.LocalActs.LocalActsRole.Type.RegDocManagers ||
          _obj.Type == DirRX.LocalActs.LocalActsRole.Type.CRegDocManagers)
        query = query.Where(k => k.DocumentType.DocumentTypeGuid == DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.Order).ToList();

      if (_obj.Type == DirRX.LocalActs.LocalActsRole.Type.Supervisor || _obj.Type == DirRX.LocalActs.LocalActsRole.Type.SprvisorManager || _obj.Type == DirRX.LocalActs.LocalActsRole.Type.RiskManagers)
        query = query.Where(k => (k.DocumentType.DocumentTypeGuid == DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.Order ||
                                  k.DocumentType.DocumentTypeGuid == DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.RevisionRequest ||
                                  k.DocumentType.DocumentTypeGuid == DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.Contract ||
                                  k.DocumentType.DocumentTypeGuid == DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.SupAgreement||
                                  k.DocumentType.DocumentTypeGuid == DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.MemoForPayment)).ToList();
      return query;
    }
  }
}