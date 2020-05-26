using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Company;

namespace DirRX.Solution.Client
{
  internal static class CompanyStoplistHistoryStaticActions
  {

    public static bool CanAddToStoplist(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return true;
    }

    public static void AddToStoplist(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var counterparty = DirRX.Solution.Companies.As(e.RootEntity);
      if (counterparty == null)
        return;
      
      var dialog = Dialogs.CreateInputDialog(Companies.Resources.IncludeReasonDialogTitle);
      var reasonInput = dialog.AddSelect(Companies.Resources.ReasonSelectTitle, true, PartiesControl.StoplistIncludeReasons.Null);
      var commentInput = dialog.AddMultilineString(Companies.Resources.CommentSelectTitle, true);
      
      var sendButton = dialog.Buttons.AddCustom(Companies.Resources.SendButtonTitle);
      var cancelButton = dialog.Buttons.AddCancel();
      dialog.Buttons.Default = sendButton;
      if (dialog.Show() == sendButton)
      {
        var stoplistReason = reasonInput.Value;
        var stoplistComment = commentInput.Value;
        var authorizedEmployeeRole = stoplistReason.Responsible;
        
        if (Solution.PublicFunctions.Module.Remote.IncludedInRoleWithSubsitute(authorizedEmployeeRole.Sid.Value,
                                                                               new List<Enumeration>() { DirRX.ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.Others }))
        {
          // Если текущий пользователь - уполномоченный сотрудник, то заполнить строку истории включения в стоп-лист.
          var stoplistHistory = counterparty.StoplistHistory;
          var stoplistRecord = stoplistHistory.Where(r => PartiesControl.CheckingReasons.Equals(r.Reason, reasonInput.Value) && !r.ExcludeDate.HasValue).FirstOrDefault();
          
          if (stoplistRecord != null)
          {
            Dialogs.ShowMessage(Companies.Resources.StoplistRecordTheSameReasonExists);
            AddToStoplist(e);
          }
          else
          {
            var isAlreadyIncluded = counterparty.StoplistHistory.Any(s => !s.ExcludeDate.HasValue);
            
            stoplistRecord = stoplistHistory.AddNew();
            stoplistRecord.Reason = stoplistReason;
            stoplistRecord.IncComment = stoplistComment;
            stoplistRecord.IncludeDate = Calendar.UserToday;
            stoplistRecord.IncUser = Employees.Current;

            var eventGUID = Functions.Company.Remote.CreateAndSendStoplistToCSB(stoplistRecord,
                                                                                Constants.Parties.Company.CSBStoplistAction.Include,
                                                                                Constants.Parties.Company.CSBStoplistStatus.Started,
                                                                                stoplistComment);
            if (!string.IsNullOrEmpty(eventGUID))
            {
              stoplistRecord.EventGUID = eventGUID;
              stoplistRecord.IsIncludeSended = true;
            }
            
            if (!isAlreadyIncluded)
            {
              counterparty.IsStoplistIncluded = true;
              var stoplistStatus = DirRX.PartiesControl.PublicFunctions.CounterpartyStatus.Remote.GetCounterpartyStatus(PartiesControl.PublicConstants.CounterpartyStatus.DefaultStatus.StopListSid);
              if (stoplistStatus != null && !PartiesControl.CounterpartyStatuses.Equals(stoplistStatus, counterparty.CounterpartyStatus))
                counterparty.CounterpartyStatus = stoplistStatus;
              
              counterparty.Save();

              Functions.Company.Remote.SendNoticeIncludeExcludeStoplist(counterparty, authorizedEmployeeRole, true);
            }
            else
              counterparty.Save();
          }
        }
        else
        {
          // Если текущий пользователь - не уполномоченный сотрудник, то отправить задачу уполномоченному сотруднику.
          var task = Functions.Company.Remote.GetIncludeExcludeStoplistTask(counterparty, authorizedEmployeeRole, stoplistComment, true);
          task.Show();
        }
      }
    }
  }

  partial class CompanyStoplistHistoryActions
  {

    public virtual bool CanRemoveFromStoplist(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return true;
    }

    public virtual void RemoveFromStoplist(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      if (_obj.ExcludeDate.HasValue)
      {
        Dialogs.ShowMessage(Companies.Resources.StoplistRecordAllreadyExclude);
        return;
      }
      
      var dialog = Dialogs.CreateInputDialog(Companies.Resources.ExcludeReasonDialogTitle);
      var reasonInput = dialog.AddSelect(Companies.Resources.ReasonSelectTitle, true, _obj.Reason);
      var commentInput = dialog.AddMultilineString(Companies.Resources.CommentSelectTitle, true);
      
      var sendButton = dialog.Buttons.AddCustom(Companies.Resources.SendButtonTitle);
      var cancelButton = dialog.Buttons.AddCancel();
      dialog.Buttons.Default = sendButton;
      if (dialog.Show() == sendButton)
      {
        var counterparty = _obj.Company;
        var stoplistReason = reasonInput.Value;
        var stoplistComment = commentInput.Value;
        var authorizedEmployeeRole = stoplistReason.Responsible;
        
        if (Solution.PublicFunctions.Module.Remote.IncludedInRoleWithSubsitute(authorizedEmployeeRole.Sid.Value,
                                                                               new List<Enumeration>() { DirRX.ProcessSubstitutionModule.ProcessSubstitutionSubstitutionCollection.Process.Others }))
        {
          // Если текущий пользователь - уполномоченный сотрудник, то заполнить строку истории исключения из стоп-листа.
          _obj.ExcComment = commentInput.Value;
          _obj.ExcludeDate = Calendar.UserToday;
          _obj.ExcUser = Employees.Current;
          
          if (!counterparty.StoplistHistory.Any(s => !s.ExcludeDate.HasValue))
          {
            counterparty.IsStoplistIncluded = false;
            
            if (counterparty.CheckingResult != null && counterparty.CheckingResult.CounterpartyStatus != null)
              counterparty.CounterpartyStatus = counterparty.CheckingResult.CounterpartyStatus;
            else
            {
              var checkRequiredStatus = DirRX.PartiesControl.PublicFunctions.CounterpartyStatus.Remote.GetCounterpartyStatus(PartiesControl.PublicConstants.CounterpartyStatus.DefaultStatus.CheckingRequiredSid);
              if (checkRequiredStatus != null)
                counterparty.CounterpartyStatus = checkRequiredStatus;
            }

            var eventGUID = Functions.Company.Remote.CreateAndSendStoplistToCSB(_obj,
                                                                                Constants.Parties.Company.CSBStoplistAction.Exclude,
                                                                                Constants.Parties.Company.CSBStoplistStatus.Ended,
                                                                                stoplistComment);
            if (!string.IsNullOrEmpty(eventGUID))
              _obj.IsExcludeSended = true;
            
            _obj.Company.Save();
            
            DirRX.Solution.Functions.Company.Remote.SendNoticeIncludeExcludeStoplist(counterparty, authorizedEmployeeRole, false);
          }
          else
          {
            var eventGUID = Functions.Company.Remote.CreateAndSendStoplistToCSB(_obj,
                                                                                Constants.Parties.Company.CSBStoplistAction.Include,
                                                                                Constants.Parties.Company.CSBStoplistStatus.Ended,
                                                                                stoplistComment);
            if (!string.IsNullOrEmpty(eventGUID))
              _obj.IsExcludeSended = true;
            
            _obj.Company.Save();
          }
        }
        else
        {
          // Если текущий пользователь - не уполномоченный сотрудник, то отправить задачу уполномоченному сотруднику.
          var task =  DirRX.Solution.Functions.Company.Remote.GetIncludeExcludeStoplistTask(counterparty, authorizedEmployeeRole, stoplistComment, false);
          task.Show();
        }
      }
    }
  }


  partial class CompanyActions
  {

    public virtual void UnapprovedCounterpartyContracts(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      Functions.Company.Remote.GetUnapprovedCounterpartyContracts(_obj).Show();
    }

    public virtual bool CanUnapprovedCounterpartyContracts(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ImportFromKSSSByCode(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      _obj.Save();
      if (_obj.KSSSContragentId == null)
      {
        e.AddError(DirRX.Solution.Companies.Resources.CodeKSSSNotFound);
        return;
      }

      var result = Functions.Company.Remote.CreateRequest(_obj, Constants.Parties.Company.KSSSParams.CSCDIDFieldName, _obj.KSSSContragentId.Value.ToString());
      
      if (string.IsNullOrEmpty(result))
        e.AddInformation(DirRX.Solution.Companies.Resources.RequestSentSuccessfully);
      else
        e.AddError(result);
    }

    public virtual bool CanImportFromKSSSByCode(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public virtual void ExportToKSSS(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      _obj.Save();
      
      var result = Functions.Company.Remote.CreateRequest(_obj);
      
      if (string.IsNullOrEmpty(result))
        e.AddInformation(DirRX.Solution.Companies.Resources.RequestSentSuccessfully);
      else
        e.AddError(result);
    }

    public virtual bool CanExportToKSSS(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Users.Current.IncludedIn(PartiesControl.PublicConstants.Module.KsssResponsibleRole);
    }

    public virtual void AddressList(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      DirRX.PartiesControl.PublicFunctions.ShippingAddress.Remote.GetShippingAdresses(_obj).Show();
    }

    public virtual bool CanAddressList(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void CheckingList(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      PublicFunctions.Company.Remote.OpenRevisionRequests(_obj).Show();
    }

    public virtual bool CanCheckingList(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted;
    }

    public virtual void SearchInKSSSByINN(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (!e.Validate())
        return;
      
      var result = Functions.Company.Remote.CreateRequest(_obj, Constants.Parties.Company.KSSSParams.INNFieldName, _obj.TIN);
      
      if (string.IsNullOrEmpty(result))
        e.AddInformation(DirRX.Solution.Companies.Resources.RequestSentSuccessfully);
      else
        e.AddError(result);
    }

    public virtual bool CanSearchInKSSSByINN(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && !string.IsNullOrEmpty(_obj.TIN);
    }

  }

}