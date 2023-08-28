using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PeriodicActionItemsTemplate.RepeatSetting;

namespace DirRX.PeriodicActionItemsTemplate.Client
{
  partial class RepeatSettingActionItemsPartsActions
  {

    public virtual bool CanFillPartCoAssignees(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return _obj.RepeatSetting.AccessRights.CanUpdate();
    }

    public virtual void FillPartCoAssignees(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      var selectedEmployees = Sungero.Company.PublicFunctions.Employee.Remote.GetEmployees()
        .Where(ca => ca.Status == Sungero.CoreEntities.DatabookEntry.Status.Active)
        .ShowSelectMany(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.ChooseCoAssigneesToAdd)
        .ToList();
      
      foreach (var coAssigneeRow in _obj.RepeatSetting.PartsCoAssignees.Where(pca => pca.PartGuid == _obj.PartGuid).ToList())
      {
        selectedEmployees.Add(coAssigneeRow.CoAssignee);
        _obj.RepeatSetting.PartsCoAssignees.Remove(coAssigneeRow);
      }
      
      foreach (var employee in selectedEmployees.Distinct())
      {
        var row = _obj.RepeatSetting.PartsCoAssignees.AddNew();
        row.CoAssignee = employee;
        row.PartGuid = _obj.PartGuid;
      }
      
      _obj.CoAssignees = string.Join("; ", _obj.RepeatSetting.PartsCoAssignees.Where(pca => pca.PartGuid == _obj.PartGuid).Select(x => x.CoAssignee.Person.ShortName));
    }

    public virtual bool CanClearCoAssignees(Sungero.Domain.Client.CanExecuteChildCollectionActionArgs e)
    {
      return _obj.RepeatSetting.AccessRights.CanUpdate() && _obj.RepeatSetting.PartsCoAssignees.Any(pca => pca.PartGuid == _obj.PartGuid);
    }

    public virtual void ClearCoAssignees(Sungero.Domain.Client.ExecuteChildCollectionActionArgs e)
    {
      
      var selectedEmployees = _obj.RepeatSetting.PartsCoAssignees
        .Where(pca => pca.PartGuid == _obj.PartGuid)
        .Select(pca => pca.CoAssignee)
        .ShowSelectMany(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.ChooseCoAssigneesForDelete)
        .ToList();
      
      foreach (var coAssigneeRow in _obj.RepeatSetting.PartsCoAssignees
               .Where(pca => pca.PartGuid == _obj.PartGuid && selectedEmployees.Contains(pca.CoAssignee))
               .ToList())
        _obj.RepeatSetting.PartsCoAssignees.Remove(coAssigneeRow);
      
      _obj.CoAssignees = string.Join("; ", _obj.RepeatSetting.PartsCoAssignees.Where(pca => pca.PartGuid == _obj.PartGuid).Select(x => x.CoAssignee.Person.ShortName));
    }
  }

  partial class RepeatSettingActions
  {
    public virtual void CreateSchedule(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      // ѕроверить, есть ли активные записи расписани€ отправки.
      var activeScheduleItems = Functions.RepeatSetting.Remote.GetActiveAwaitingScheduleItemsForSetting(_obj);
      
      // ѕо умолчанию true, чтобы не запускать проверку дублей, если действующий записей расписани€ этой настройки еще нет.
      bool? needClose = true;
      
      if (activeScheduleItems.Any())
      {
        needClose = Functions.RepeatSetting.ShowSelectActionForActiveScheduleItemsDialog();
        if (needClose == null)
          return;
        
        if (needClose == true)
        {
          var ids = activeScheduleItems.Select(si => si.Id);
          foreach (var id in ids)
          {
            var asyncHandler = AsyncHandlers.CloseScheduleItem.Create();
            asyncHandler.ScheduleItemId = id;
            asyncHandler.ExecuteAsync();
          }
        }
      }
      
      if (_obj.Type == DirRX.PeriodicActionItemsTemplate.RepeatSetting.Type.Arbitrary)
      {
        var scheduleItems = Functions.RepeatSetting.ShowArbitraryScheduleCreationDialog(_obj);
        foreach (var item in scheduleItems)
        {
          var scheduleItem = Functions.ScheduleItem.Remote.CreateNew();
          scheduleItem.RepeatSetting = _obj;
          scheduleItem.StartDate = item.StartDate;
          scheduleItem.Deadline = item.Deadline;
          scheduleItem.Save();
        }
        _obj.State.Controls.ScheduleStateView.Refresh();
        _obj.State.Pages.Schedule.Activate();
      }
      else
      {
        var asyncHandler = AsyncHandlers.CreateSchedule.Create();
        asyncHandler.RepeatSettingId = _obj.Id;
        asyncHandler.CheckDuplicates = needClose != true;
        asyncHandler.ExecuteAsync();
        Dialogs.NotifyMessage(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.SchedulingStarted);
      }
      
    }

    public virtual bool CanCreateSchedule(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted &&
        !_obj.State.IsChanged &&
        _obj.Status == Status.Active &&
        _obj.AccessRights.CanUpdate() &&
        ScheduleItems.AccessRights.CanCreate();
    }



    public virtual void AddPerformer(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var recipients = Sungero.Company.PublicFunctions.Module.GetAllActiveNoSystemGroups();
      var performer = recipients.ShowSelect();
      if (performer != null)
      {
        var error = DirRX.PeriodicActionItemsTemplate.PublicFunctions.RepeatSetting.Remote.SetRecipientsToAssignees(_obj, performer);
        if (error == Sungero.RecordManagement.ActionItemExecutionTasks.Resources.BigGroupWarningFormat(Sungero.RecordManagement.PublicConstants.ActionItemExecutionTask.MaxCompoundGroup))
          Dialogs.NotifyMessage(error);
      }
    }

    public virtual bool CanAddPerformer(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return _obj.IsCompoundActionItem == true;
    }

    public virtual void ChangeCompoundMode(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      if (_obj.IsCompoundActionItem == true)
      {
        if (_obj.ActionItemsParts.Count(a => a.Assignee != null) > 1 || _obj.ActionItemsParts.Any(a => a.Deadline != null || !string.IsNullOrEmpty(a.ActionItemPart)))
        {
          var dialog = Dialogs.CreateTaskDialog(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ChangeCompoundModeQuestion,
                                                Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ChangeCompoundModeDescription,
                                                MessageType.Question);
          dialog.Buttons.AddYesNo();
          dialog.Buttons.Default = DialogButtons.No;
          var yesResult = dialog.Show() == DialogButtons.Yes;
          if (yesResult)
            _obj.IsCompoundActionItem = false;
        }
        else
          _obj.IsCompoundActionItem = false;
      }
      else
        _obj.IsCompoundActionItem = true;
    }

    public virtual bool CanChangeCompoundMode(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

  }

}