using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PeriodicActionItemsTemplate.RepeatSetting;

namespace DirRX.PeriodicActionItemsTemplate.Server
{
  partial class RepeatSettingFunctions
  {
    #region Скопировано из стандартной.
    
    /// <summary>
    /// Добавить получателей в группу исполнителей поручения, исключая дублирующиеся записи.
    /// </summary>
    /// <param name="recipient">Реципиент.</param>
    /// <returns>Если возникили ошибки/хинты, возвращает текст ошибки, иначе - пустая строка.</returns>
    [Public, Remote]
    public string SetRecipientsToAssignees(IRecipient recipient)
    {
      var error = string.Empty;
      var performers = new List<IRecipient> { recipient };
      var employees = Sungero.Company.PublicFunctions.Module.Remote.GetEmployeesFromRecipientsRemote(performers);
      if (employees.Count > Sungero.RecordManagement.PublicConstants.ActionItemExecutionTask.MaxCompoundGroup)
        return Sungero.RecordManagement.ActionItemExecutionTasks.Resources.BigGroupWarningFormat(Sungero.RecordManagement.PublicConstants.ActionItemExecutionTask.MaxCompoundGroup);
      
      var currentPerformers = _obj.ActionItemsParts.Select(x => x.Assignee);
      employees = employees.Except(currentPerformers).ToList();
      
      foreach (var employee in employees)
        _obj.ActionItemsParts.AddNew().Assignee = employee;
      
      return error;
    }
    
    #endregion
    
    /// <summary>
    /// Получить все действующие и неотправленные записи расписания отправки для графика.
    /// </summary>
    /// <returns>Объект запроса с действующими и неотправленными записями расписания.</returns>
    [Remote(IsPure = true)]
    public virtual IQueryable<IScheduleItem> GetActiveAwaitingScheduleItemsForSetting()
    {
      return ScheduleItems.GetAll(si => Equals(si.RepeatSetting, _obj) && si.ActionItemExecutionTask == null && si.Status == DirRX.PeriodicActionItemsTemplate.ScheduleItem.Status.Active);
    }
    
    /// <summary>
    /// Выдать права участникам периодического поручения.
    /// </summary>
    public virtual void GrantRightsToParticipants()
    {
      if (_obj.AccessRights.CanUpdate())
      {
        if (_obj.Supervisor != null)
          _obj.AccessRights.Grant(_obj.Supervisor, DefaultAccessRightsTypes.Change);
        
        if (_obj.AssignedBy != null)
          _obj.AccessRights.Grant(_obj.AssignedBy, DefaultAccessRightsTypes.Change);
        
        if (_obj.IsCompoundActionItem == true)
        {
          foreach (var row in _obj.ActionItemsParts)
          {
            _obj.AccessRights.Grant(row.Assignee, DefaultAccessRightsTypes.Read);
            if (row.Supervisor != null)
              _obj.AccessRights.Grant(row.Supervisor, DefaultAccessRightsTypes.Change);
          }
          
          foreach (var row in _obj.PartsCoAssignees)
            _obj.AccessRights.Grant(row.CoAssignee, DefaultAccessRightsTypes.Read);
        }
        else
        {
          if (_obj.Assignee != null)
            _obj.AccessRights.Grant(_obj.Assignee, DefaultAccessRightsTypes.Read);
          
          foreach (var row in _obj.CoAssignees)
            _obj.AccessRights.Grant(row.Assignee, DefaultAccessRightsTypes.Read);
        }
        
      }
    }
    
    /// <summary>
    /// Записать в историю изменение участников периодического поручения.
    /// </summary>
    public virtual void WriteChangeParticipantsActionToHistory()
    {
      // Основная задача - зафиксировать в истории добавление/удаление уникальных участников. Т.е. в историю пададет запись, когда состав участников изменится.
      // Если поменяли местами исполнителя и соисполнителя, или переместили соисполнители между пунктами - то записи в историю не будет.
      
      var props = _obj.State.Properties;
      
      var currentParticipants = new List<Sungero.Company.IEmployee>();
      if (_obj.IsCompoundActionItem == true)
      {
        currentParticipants.AddRange(_obj.ActionItemsParts.Where(aip => aip.Assignee != null).Select(aip => aip.Assignee));
        currentParticipants.AddRange(_obj.PartsCoAssignees.Select(pca => pca.CoAssignee));
      }
      else
      {
        currentParticipants.Add(_obj.Assignee);
        currentParticipants.AddRange(_obj.CoAssignees.Select(ca => ca.Assignee));
      }
      currentParticipants = currentParticipants.Distinct().OrderBy(p => p.Id).ToList();
      
      var previousParticipants = new List<Sungero.Company.IEmployee>();
      // Changed содержит в себе и Added, а Added не нужен для поиска изначальных исполнителей.
      if (_obj.State.Properties.IsCompoundActionItem.OriginalValue == true)
      {        
        previousParticipants.AddRange(props.ActionItemsParts.Changed
                                      .Where(aip => aip.Assignee != null)
                                      .Where(aip => !props.ActionItemsParts.Added.Any(aaip => Equals(aaip, aip)))
                                      .Select(aip => aip.Assignee));
        
        previousParticipants.AddRange(props.ActionItemsParts.Deleted
                                      .Where(aip => aip.Assignee != null)
                                      .Select(aip => aip.Assignee));
        
        previousParticipants.AddRange(props.PartsCoAssignees.Changed
                                      .Where(ca => !props.PartsCoAssignees.Added.Any(aca => Equals(aca, ca)))
                                      .Select(ca => ca.CoAssignee));
        
        previousParticipants.AddRange(props.PartsCoAssignees.Deleted
                                      .Select(ca => ca.CoAssignee));
        
        // Ищем неизмененных исполнителей, чтобы их тоже учесть при подсчете изменений.
        previousParticipants.AddRange(_obj.ActionItemsParts
                                      .Where(aip => aip.Assignee != null)
                                      .Where(aip => !props.ActionItemsParts.Changed.Any(caip => Equals(caip, aip)))
                                      .Select(aip => aip.Assignee));
        
        previousParticipants.AddRange(_obj.PartsCoAssignees
                                      .Where(ca => !props.PartsCoAssignees.Changed.Any(aca => Equals(aca, ca)))
                                      .Select(ca => ca.CoAssignee));
      }
      else
      {
        previousParticipants.Add(props.Assignee.OriginalValue);
        
        previousParticipants.AddRange(props.CoAssignees.Changed
                                      .Where(ca => !props.CoAssignees.Added.Any(aca => Equals(aca, ca)))
                                      .Select(ca => ca.Assignee));
        
        previousParticipants.AddRange(props.CoAssignees.Deleted
                                      .Select(ca => ca.Assignee));
        
        previousParticipants.AddRange(_obj.CoAssignees
                                      .Where(ca => !props.CoAssignees.Changed.Any(aca => Equals(aca, ca)))
                                      .Select(ca => ca.Assignee));
      }

      previousParticipants = previousParticipants.Distinct().OrderBy(p => p.Id).ToList();
      
      if (!currentParticipants.SequenceEqual(previousParticipants))
      {
        var participantsChanged = new Enumeration(Constants.RepeatSetting.ChangeParticipants);
        var previousParticipantsText = string.Join(", ", previousParticipants.Where(p => !string.IsNullOrEmpty(p.Person?.ShortName)).Select(p => p.Person.ShortName));
        var currentParticipantsText = string.Join(", ", currentParticipants.Where(p => !string.IsNullOrEmpty(p.Person?.ShortName)).Select(p => p.Person.ShortName));
        _obj.History.Write(participantsChanged,
                           participantsChanged,
                           DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.ChangedParticipantsHistoryCommentFormat(previousParticipantsText, currentParticipantsText));
      }
    }
    
    /// <summary>
    /// Создать запись справочника по настройке периодичности поручения.
    /// </summary>
    /// <returns></returns>
    [Remote, Public]
    public static DirRX.PeriodicActionItemsTemplate.IRepeatSetting CreateRepeatSetting()
    {
      return RepeatSettings.Create();
    }
    
    /// <summary>
    /// Получить график отправки для поручения.
    /// </summary>
    /// <param name="actionItem">Поручение.</param>
    /// <returns>График отправки.</returns>
    /// <remarks>Сначала идет поиск графика, где поручение указано как поручение-основание.
    /// Если график не найден, то идет поиска графика, в рамках которого поручение было создано как периодическое.</remarks>
    [Remote(IsPure = true), Public]
    public static DirRX.PeriodicActionItemsTemplate.IRepeatSetting GetSettingByActionItem(Sungero.RecordManagement.IActionItemExecutionTask actionItem)
    {
      var settingAsMainActionItem = RepeatSettings.GetAll(rs => Equals(rs.MainActionItem, actionItem)).FirstOrDefault();
      if (settingAsMainActionItem != null)
        return settingAsMainActionItem;
      
      return ScheduleItems.GetAll(si => Equals(si.ActionItemExecutionTask, actionItem)).FirstOrDefault()?.RepeatSetting;
    }
    
    /// <summary>
    /// Получить график отправки для документа-основания.
    /// </summary>
    /// <param name="document">Документ-основание.</param>
    /// <returns>График отправки.</returns>
    [Remote(IsPure = true), Public]
    public static IQueryable<DirRX.PeriodicActionItemsTemplate.IRepeatSetting> GetSettingsByDocument(Sungero.Docflow.IOfficialDocument document)
    {
      return RepeatSettings.GetAll(rs => Equals(rs.MainDocument, document));
    }
    
    /// <summary>
    /// Сформировать тему для поручения.
    /// </summary>
    /// <returns>Тема поручения.</returns>
    [Remote]
    public string CreateSubject()
    {
      var subjectTemplate = _obj.IsCompoundActionItem == true ?
        Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject :
        Sungero.RecordManagement.ActionItemExecutionTasks.Resources.TaskSubject;
      
      return Functions.RepeatSetting.GetActionItemExecutionSubject(_obj, subjectTemplate);
    }
    
    #region Вкладка "График"
    
    /// <summary>
    /// Построить модель графика.
    /// </summary>
    /// <returns>Контрол состояния.</returns>
    [Remote(IsPure = true)]
    public virtual StateView GetScheduleStateView()
    {
      var stateView = StateView.Create();
      
      var sendedHeaderBlock = stateView.AddBlock();
      sendedHeaderBlock.ShowBorder = false;
      sendedHeaderBlock.DockType = DockType.Bottom;
      sendedHeaderBlock.AddLabel(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.Sent);
      
      var redStyle = StateBlockLabelStyle.Create();
      redStyle.Color = Colors.Common.Red;
      
      var sendedActionItemsBlock = AddParticipantsBlockAndLabel(stateView);
      
      
      foreach (var sendedScheduleItem in ScheduleItems.GetAll(si => Equals(si.RepeatSetting, _obj) && si.ActionItemExecutionTask != null).OrderBy(x => x.StartDate))
      {
        var actionItem = sendedScheduleItem.ActionItemExecutionTask;
        
        var actionItemBlock = sendedActionItemsBlock.AddChildBlock();
        actionItemBlock.Entity = actionItem;
        actionItemBlock.AssignIcon(StateBlockIconType.OfEntity, StateBlockIconSize.Small);
        
        actionItemBlock.AddLabel(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.SentDateFormat(actionItem.Started.Value.ToShortDateString()));
        if (actionItem.HasIndefiniteDeadline != true)
        {
          DateTime? deadline = null;
          if (actionItem.IsCompoundActionItem == true)
          {
            var minPartDeadline = actionItem.ActionItemParts.Where(p => p.Deadline.HasValue).OrderBy(p => p.Deadline).Select(p => p.Deadline).FirstOrDefault();
            if (minPartDeadline.HasValue)
              deadline = minPartDeadline > actionItem.FinalDeadline ? actionItem.FinalDeadline : minPartDeadline;
            else
              deadline = actionItem.FinalDeadline;
          }
          else
            deadline = actionItem.Deadline;
          
          if (deadline < Calendar.Today && actionItem.Status != Sungero.RecordManagement.ActionItemExecutionTask.Status.Completed)
            actionItemBlock.AddLabel(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.DeadlineFormat(deadline.Value.ToShortDateString()), redStyle);
          else
            actionItemBlock.AddLabel(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.DeadlineFormat(deadline.Value.ToShortDateString()));
        }
        
        actionItemBlock.AddContent().AddLabel(actionItem.Info.Properties.Status.GetLocalizedValue(actionItem.Status));
      }
      
      var awaitingHeaderBlock = stateView.AddBlock();
      awaitingHeaderBlock.ShowBorder = false;
      awaitingHeaderBlock.DockType = DockType.Bottom;
      awaitingHeaderBlock.AddLabel(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.PlannedForTheNextYear);
      
      var awaitingScheduleItemsBlock = AddParticipantsBlockAndLabel(stateView);
      
      var oneYearAhead = Calendar.Today.AddYears(1);
      foreach (var awaitingScheduleItem in ScheduleItems.GetAll(si => Equals(si.RepeatSetting, _obj) &&
                                                                si.ActionItemExecutionTask == null &&
                                                                si.Status == DirRX.PeriodicActionItemsTemplate.ScheduleItem.Status.Active &&
                                                                si.StartDate <= oneYearAhead).OrderBy(x => x.StartDate))
      {
        var scheduleItemBlock = awaitingScheduleItemsBlock.AddChildBlock();
        scheduleItemBlock.Entity = awaitingScheduleItem;
        scheduleItemBlock.AssignIcon(StateBlockIconType.OfEntity, StateBlockIconSize.Small);
        scheduleItemBlock.AddLabel(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.PlannedStartDateFormat(awaitingScheduleItem.StartDate.Value.ToShortDateString()));
        if (awaitingScheduleItem.HasIndefiniteDeadline != true)
          scheduleItemBlock.AddLabel(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.PlannedDeadlineFormat(awaitingScheduleItem.Deadline.Value.ToShortDateString()));
      }
      
      return stateView;
    }
    
    /// <summary>
    /// Добавить блок с именами выдавшего и всех исполнителей.
    /// </summary>
    /// <param name="stateView">Контрол состояния.</param>
    /// <returns>Блок с именами выдавшего и всех исполнителей.</returns>
    public virtual Sungero.Core.StateBlock AddParticipantsBlockAndLabel(StateView stateView)
    {
      var greenStyle = StateBlockLabelStyle.Create();
      greenStyle.Color = Colors.Common.Green;
      
      var participantsBlock = stateView.AddBlock();
      participantsBlock.ShowBorder = false;
      participantsBlock.NeedGroupChildren = true;
      participantsBlock.IsExpanded = true;
      
      participantsBlock.AddLabel(_obj.AssignedBy.Person.ShortName);
      participantsBlock.AddLabel(" --> ");
      if (_obj.IsCompoundActionItem == true)
      {
        var firstAssignee = true;
        foreach (var part in _obj.ActionItemsParts)
        {
          if (!firstAssignee)
            participantsBlock.AddLabel(",");
          
          participantsBlock.AddLabel(part.Assignee.Person.ShortName, greenStyle);
          foreach (var coAssigneeRow in _obj.PartsCoAssignees.Where(pca => pca.PartGuid == part.PartGuid))
          {
            participantsBlock.AddLabel(",");
            participantsBlock.AddLabel(coAssigneeRow.CoAssignee.Person.ShortName);
          }
          
          firstAssignee = false;
        }
      }
      else
      {
        participantsBlock.AddLabel(_obj.Assignee.Person.ShortName, greenStyle);
        foreach (var coAssigneeRow in _obj.CoAssignees)
        {
          participantsBlock.AddLabel(coAssigneeRow.Assignee.Person.ShortName);
        }
      }
      
      return participantsBlock;
    }
    
    #endregion
  }
}