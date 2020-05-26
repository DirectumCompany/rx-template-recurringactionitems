using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.RecordCustom.RecordCustomRole;

namespace DirRX.RecordCustom.Shared
{
  partial class RecordCustomRoleFunctions
  {
    public override List<Sungero.Docflow.IDocumentKind> Filter(List<Sungero.Docflow.IDocumentKind> kinds)
    {
      var query = base.Filter(kinds);

      //Роли для служебных записок.
      if (_obj.Type == RecordCustom.RecordCustomRole.Type.MemoAddressee ||
          _obj.Type == RecordCustom.RecordCustomRole.Type.MemoAssignee ||
          _obj.Type == RecordCustom.RecordCustomRole.Type.AssigneeManager)
        query = query.Where(k => k.DocumentType.DocumentTypeGuid == "95af409b-83fe-4697-a805-5a86ceec33f5").ToList();
      return query;
    }
  }
}