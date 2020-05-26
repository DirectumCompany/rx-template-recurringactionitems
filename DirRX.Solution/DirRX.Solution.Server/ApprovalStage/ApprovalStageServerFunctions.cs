using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalStage;

namespace DirRX.Solution.Server
{
	partial class ApprovalStageFunctions
	{
		/// <summary>
		/// Получить настройки этапа.
		/// </summary>
		/// <returns>Строка с перечнем настроек.</returns>
		public override string GetStageSettings()
		{
			var baseSettings = base.GetStageSettings();
			var settings = new List<string>();
			var resources = DirRX.Solution.ApprovalStages.Resources;
			
			
			// Согласование или задание.
			if (_obj.StageType == StageType.Approvers || _obj.StageType == StageType.SimpleAgr)			
			{
				// Разрешить отправку на переработку.
				if (_obj.AllowSendToRecycling == true)
					settings.Add(resources.AllowSendToRecycling);
				
				// Включить запрос инициатору.
				if (_obj.RequestInitiatorOn == true)
					settings.Add(resources.RequestInitiatorOn);
				
				// Включить возможность отказать.
				if (_obj.CanFailOn == true)
					settings.Add(resources.CanFailOn);
				
			}
			
			// Задание.
			if (_obj.StageType == StageType.SimpleAgr)
			{
				
				// Разрешить выбор подписанта.
				if (_obj.AllowSelectSigner == true)
					settings.Add(resources.AllowSelectSigner);
				
				// Задание на одобрение контрагента.
				if (_obj.NeedCounterpartyApproval == true)
					settings.Add(resources.NeedCounterpartyApproval);
				
				// Задание на подтверждение рисков.
				if (_obj.IsRiskConfirmation == true)
					settings.Add(resources.IsRiskConfirmation);
				
				// Задание на ожидание окончания проверки контрагента.
				if (_obj.IsAssignmentOnWaitingEndValidContractor == true)
					settings.Add(resources.IsAssignmentOnWaitingEndValidContractor);
				
				// Задание на организацию подписания оригиналов.
				if (_obj.IsAssignmentOnSigningOriginals == true)
					settings.Add(resources.IsAssignmentOnSigningOriginals);
				
			}
			
			// Согласование.
			if (_obj.StageType == StageType.Approvers)
			{
				// Включить согласование с рисками.
				if (_obj.ApprovalWithRiskOn == true)
					settings.Add(resources.ApprovalWithRiskOn);
				
				// Включить возможность отклонить согласование.
				if (_obj.CanDenyApprovalOn == true)
					settings.Add(resources.CanDenyApprovalOn);
			}
			
			// Контроль возврата.
			if (_obj.StageType == StageType.CheckReturn)
			{
				// Требуется возврат документа в виде.
				if (_obj.KindOfDocumentNeedReturn.HasValue)
					settings.Add(string.Format("{0}: {1}", resources.KindOfDocumentNeedReturn, _obj.KindOfDocumentNeedReturn.Value));
			}
			
			// Отправить контрагенту.
			if (_obj.StageType == StageType.Sending)
			{
				// Необходимо отправить документ в виде.
				if (_obj.KindOfDocumentNeedSend.HasValue)
					settings.Add(string.Format("{0}: {1}", resources.KindOfDocumentNeedSend, _obj.KindOfDocumentNeedSend.Value));
			}
						
			return string.Join(Environment.NewLine, baseSettings, settings);
		}
		
		public override List<IRecipient> GetStageRecipients(Sungero.Docflow.IApprovalTask task, List<IRecipient> additionalApprovers)
		{
			var recipients = base.GetStageRecipients(task, additionalApprovers);
			var roles = _obj.ApprovalRoles.Where(r => r.ApprovalRole.Type == DirRX.LocalActs.LocalActsRole.Type.Subscribers ||
			                                    r.ApprovalRole.Type == DirRX.LocalActs.LocalActsRole.Type.RegDocManagers ||
			                                    r.ApprovalRole.Type == DirRX.LocalActs.LocalActsRole.Type.CRegDocManagers ||
			                                    r.ApprovalRole.Type == DirRX.LocalActs.LocalActsRole.Type.RiskManagers)
				.Select(r => DirRX.LocalActs.LocalActsRoles.As(r.ApprovalRole)).Where(r => r != null);
			
			foreach (var role in roles)
				recipients.AddRange(DirRX.LocalActs.PublicFunctions.LocalActsRole.Remote.GetRolePerformers(role, task));
			
			var contractRoles = _obj.ApprovalRoles.Where(r => r.ApprovalRole.Type == DirRX.ContractsCustom.ContractsRole.Type.Stage2Approvers ||
			                                    r.ApprovalRole.Type == DirRX.ContractsCustom.ContractsRole.Type.NonLukoil ||
			                                    r.ApprovalRole.Type == DirRX.ContractsCustom.ContractsRole.Type.NonStandart)
				.Select(r => DirRX.ContractsCustom.ContractsRoles.As(r.ApprovalRole)).Where(r => r != null);			
			
			foreach (var contractRole in contractRoles)
				recipients.AddRange(DirRX.ContractsCustom.PublicFunctions.ContractsRole.Remote.GetRolePerformers(contractRole, task));
			
			return recipients.Distinct().ToList();
		}
	}
}