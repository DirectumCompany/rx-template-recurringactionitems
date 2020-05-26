using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ConfirmContractExecutedAssignment;

namespace DirRX.ContractsCustom.Client
{
  partial class ConfirmContractExecutedAssignmentActions
  {
    public virtual void Refuse(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var document = _obj.AttachmentContractGroup.ContractualDocuments.SingleOrDefault();
      // Требовать открытия карточки документа.
      if (!Functions.ConfirmContractExecutedAssignment.Remote.IsDocumentOpened(document, _obj.Created.Value))
        e.AddError(DirRX.ContractsCustom.ConfirmContractExecutedAssignments.Resources.IsDocumentOpenedErrorText);
      
      if (string.IsNullOrEmpty(_obj.ActiveText))
        e.AddError(DirRX.ContractsCustom.ConfirmContractExecutedAssignments.Resources.EmptyActiveTextErrorMessage);
    }

    public virtual bool CanRefuse(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

    public virtual void Complete(Sungero.Workflow.Client.ExecuteResultActionArgs e)
    {
      var document = _obj.AttachmentContractGroup.ContractualDocuments.SingleOrDefault();
      // Требовать открытия карточки документа.
      if (!Functions.ConfirmContractExecutedAssignment.Remote.IsDocumentOpened(document, _obj.Created.Value))
        e.AddError(DirRX.ContractsCustom.ConfirmContractExecutedAssignments.Resources.IsDocumentOpenedErrorText);
    }

    public virtual bool CanComplete(Sungero.Workflow.Client.CanExecuteResultActionArgs e)
    {
      return true;
    }

  }

}