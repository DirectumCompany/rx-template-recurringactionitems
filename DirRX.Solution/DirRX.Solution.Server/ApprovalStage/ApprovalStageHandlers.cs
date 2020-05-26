using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalStage;

namespace DirRX.Solution
{
  partial class ApprovalStageServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      // У задания на организацию подписания оригинала с признаком IsAssignmentOnSigningOriginals может быть только один исполнитель.
      if (_obj.StageType == StageType.SimpleAgr && _obj.IsAssignmentOnSigningOriginals == true)
      {
        var message = DirRX.Solution.ApprovalStages.Resources.CheckAssignmentOnSigningOriginalsRecipientsMessage;
        if ((_obj.Recipients.Any() && _obj.ApprovalRoles.Any()) ||
            _obj.Recipients.Count > 1 ||
            _obj.ApprovalRoles.Count > 1 ||
            _obj.ApprovalRoles.Any(r => r.ApprovalRole.Type == Sungero.Docflow.ApprovalRoleBase.Type.Approvers || 
                                   r.ApprovalRole.Type == DirRX.LocalActs.LocalActsRole.Type.Subscribers ||
                                   r.ApprovalRole.Type == DirRX.LocalActs.LocalActsRole.Type.CRegDocManagers ||
                                   r.ApprovalRole.Type == DirRX.LocalActs.LocalActsRole.Type.RegDocManagers ||
                                   r.ApprovalRole.Type == DirRX.LocalActs.LocalActsRole.Type.RiskManagers))
          e.AddError(message);
        else
        {
          // Если указана группа, то проверим сколько в ней участников.
          var recipients = new List<IRecipient>();
          // Сотрудники/группы.
          if (_obj.Recipients.Any())
            recipients.AddRange(_obj.Recipients
                                .Where(rec => rec.Recipient != null)
                                .Select(rec => rec.Recipient)
                                .ToList());
          var performers = Sungero.Docflow.PublicFunctions.Module.Remote.GetEmployeesFromRecipients(recipients);
          if (performers.Distinct().Count() > 1)
            e.AddError(message);
        }
      }
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      if (!_obj.State.IsCopied)
      {
        _obj.AllowSendToRecycling = false;
        _obj.IsRiskConfirmation = false;
        _obj.NeedCounterpartyApproval = false;
        _obj.CanFailOn = false;
        _obj.RequestInitiatorOn = false;
        _obj.AllowSelectSigner = false;
        _obj.IsAssignmentOnWaitingEndValidContractor = false;
        _obj.IsAssignmentOnSigningOriginals = false;
        _obj.ApprovalWithRiskOn = false;
        _obj.CanDenyApprovalOn = false;
        _obj.IsAssignmentAllDocsReceived = false;
        _obj.IsAssignmentCorporateApproval = false;
        _obj.IsSubjectTransactionConfirmation = false;
        _obj.IsAssignmentOnSigningScans = false;
        _obj.NeedBarcode = false;
      }
    }
  }

}