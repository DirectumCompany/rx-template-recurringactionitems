using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ContractsRole;

namespace DirRX.ContractsCustom.Shared
{
	partial class ContractsRoleFunctions
	{
	
		/// <summary>
		/// Ограничение доступности роли по видам документов.
		/// </summary>
		/// <param name="kinds">Информация о типах документов.</param>
		/// <returns>Список допустимых видов документов.</returns>
		public override List<Sungero.Docflow.IDocumentKind> Filter(List<Sungero.Docflow.IDocumentKind> kinds)
		{
			var query = base.Filter(kinds);
			
			if (_obj.Type == DirRX.ContractsCustom.ContractsRole.Type.ContractResp ||
			    _obj.Type == DirRX.ContractsCustom.ContractsRole.Type.CoExecutor ||
			    _obj.Type == DirRX.ContractsCustom.ContractsRole.Type.Stage2Approvers ||
			    _obj.Type == DirRX.ContractsCustom.ContractsRole.Type.NonLukoil ||
			    _obj.Type == DirRX.ContractsCustom.ContractsRole.Type.NonStandart)
				query = query.Where(k => k.DocumentType.DocumentTypeGuid == DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.Contract ||
				                    k.DocumentType.DocumentTypeGuid == DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.SupAgreement).ToList();
			return query;
		}
		
	}
}