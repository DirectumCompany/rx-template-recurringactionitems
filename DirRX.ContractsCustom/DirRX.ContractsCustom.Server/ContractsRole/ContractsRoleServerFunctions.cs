using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ContractsRole;

namespace DirRX.ContractsCustom.Server
{
	partial class ContractsRoleFunctions
	{
		/// <summary>
		/// Получить сотрудника, исполнителя роли.
		/// </summary>
		/// <param name="task">Задача на согласование.</param>
		/// <returns>Сотрудник.</returns>
		public override Sungero.Company.IEmployee GetRolePerformer(Sungero.Docflow.IApprovalTask task)
		{
			if (_obj.Type == DirRX.ContractsCustom.ContractsRole.Type.CoExecutor)
			{
				var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
				if (document == null)
					return null;
				
				if (Solution.Contracts.Is(document))
				{
					var contract = Solution.Contracts.As(document);
					if (contract != null)
						return contract.CoExecutor != null ? contract.CoExecutor : contract.ResponsibleEmployee;
				}
				
				if (Solution.SupAgreements.Is(document))
				{
					var supAgreement = Solution.SupAgreements.As(document);
					if (supAgreement != null)
						return supAgreement.CoExecutor != null ? supAgreement.CoExecutor : supAgreement.ResponsibleEmployee;
				}
				return null;
			}
			
			if (_obj.Type == DirRX.ContractsCustom.ContractsRole.Type.ContractResp)
			{
				var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
				if (document == null)
					return null;
				
				if (Solution.Contracts.Is(document))
				{
					var contract = Solution.Contracts.As(document);
					if (contract != null)
						return contract.ResponsibleEmployee;
				}
				
				if (Solution.SupAgreements.Is(document))
				{
					var supAgreement = Solution.SupAgreements.As(document);
					if (supAgreement != null)
						return supAgreement.ResponsibleEmployee;
				}
				
				if (ContractsCustom.MemoForPayments.Is(document))
				{
					var memoForPayment = ContractsCustom.MemoForPayments.As(document);
					if (memoForPayment != null)
						return memoForPayment.ResponsibleEmployee;
				}
				return null;
			}
			
			return base.GetRolePerformer(task);
		}
		
		/// <summary>
		/// Получить сотрудников, исполнителя роли.
		/// </summary>
		/// <param name="task">Задача на согласование.</param>
		/// <returns>Сотрудник.</returns>
		[Remote(IsPure = true), Public]
		public List<Sungero.CoreEntities.IRecipient> GetRolePerformers(Sungero.Docflow.IApprovalTask task)
		{
			var result = new List<Sungero.CoreEntities.IRecipient>();
			var document = task.DocumentGroup.OfficialDocuments.FirstOrDefault();
			// Проверить, что согласуется договор или доп. соглашение.
			var contract = DirRX.Solution.Contracts.As(document);
			var supAgreement = DirRX.Solution.SupAgreements.As(document);
			var leadingDocument = DirRX.Solution.Contracts.Null;
			if (supAgreement != null)
			  leadingDocument = DirRX.Solution.Contracts.As(supAgreement.LeadingDocument);
			
			var subCategory = contract != null ? contract.Subcategory : ContractSubcategories.Null;
			
			var setting = MatchingSettings.GetAll(x => x.Status == DirRX.ContractsCustom.MatchingSetting.Status.Active &&
			                                       DirRX.Solution.DocumentKinds.Equals(x.DocumentKind, document.DocumentKind) &&
			                                       DirRX.Solution.ContractCategories.Equals(x.DocumentGroup, document.DocumentGroup) &&
			                                       DirRX.ContractsCustom.ContractSubcategories.Equals(x.ContractSubcategory, subCategory) &&
			                                       ((_obj.Type == DirRX.ContractsCustom.ContractsRole.Type.Stage2Approvers && x.Stage2Matching.Any(m => m.Role != null)) ||
			                                        (_obj.Type == DirRX.ContractsCustom.ContractsRole.Type.NonLukoil && x.NonLukoilGroupCompany.Any(m => m.Role != null)) ||
			                                        (_obj.Type == DirRX.ContractsCustom.ContractsRole.Type.NonStandart && x.NonStandart.Any(m => m.Role != null)))).FirstOrDefault();
			
			if (setting == null)
			{
			  // Для доп. соглашений искать на основании ведущего документа.
			  if (contract == null)
			  {			  	
			  	
			  	subCategory = leadingDocument.Subcategory;
			  	setting = MatchingSettings.GetAll(x => x.Status == DirRX.ContractsCustom.MatchingSetting.Status.Active &&
			                                       DirRX.Solution.DocumentKinds.Equals(x.DocumentKind, leadingDocument.DocumentKind) &&
			                                       DirRX.Solution.ContractCategories.Equals(x.DocumentGroup, leadingDocument.DocumentGroup) &&
			                                       DirRX.ContractsCustom.ContractSubcategories.Equals(x.ContractSubcategory, subCategory) &&
			                                       ((_obj.Type == DirRX.ContractsCustom.ContractsRole.Type.Stage2Approvers && x.Stage2Matching.Any(m => m.Role != null)) ||
			                                        (_obj.Type == DirRX.ContractsCustom.ContractsRole.Type.NonLukoil && x.NonLukoilGroupCompany.Any(m => m.Role != null)) ||
			                                        (_obj.Type == DirRX.ContractsCustom.ContractsRole.Type.NonStandart && x.NonStandart.Any(m => m.Role != null)))).FirstOrDefault();
			  	if (setting == null)
			  	  return result;
			  }
			  else
			    return result;
			}
			
			var roles = new List<Sungero.CoreEntities.IRole>();
			if (_obj.Type == DirRX.ContractsCustom.ContractsRole.Type.Stage2Approvers)
				roles = setting.Stage2Matching.Where(m => m.Role != null).Select(p => p.Role).ToList();
			else if (_obj.Type == DirRX.ContractsCustom.ContractsRole.Type.NonLukoil)
				roles = setting.NonLukoilGroupCompany.Where(m => m.Role != null).Select(p => p.Role).ToList();
			else if (_obj.Type == DirRX.ContractsCustom.ContractsRole.Type.NonStandart && (contract != null ? contract.IsStandard != true : supAgreement.IsStandard != true))
			  roles = setting.NonStandart.Where(m => m.Role != null).Select(p => p.Role).ToList();

			foreach (var role in roles)
				result.AddRange(role.RecipientLinks.Select(x => x.Member));
			
			return result;
		}
	}
}