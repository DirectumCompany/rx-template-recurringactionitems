using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.NoticeSetting;

namespace DirRX.ActionItems.Shared
{
  partial class NoticeSettingFunctions
  {
    /// <summary>
    /// Получить описание события.
    /// </summary>
    /// <param name="assignmentSubject">Название события.</param>
    /// <returns>Описание события.</returns>
    public static string GetNoticeDescriptionByEvent(string assignmentEvent)
    {
      switch (assignmentEvent)
      {
          case "StartEvent": return NoticeSettings.Resources.AssignmentNoticeStartsDescription;
          case "AbortEvent": return NoticeSettings.Resources.AssignmentNoticeAbortsDescription;
          case "RejectionEvent": return NoticeSettings.Resources.AssignmentNoticeRejectionDescription;
          case "AssigneeChangedEvent": return NoticeSettings.Resources.AssignmentNoticeAssigneeChangedDescription;
          case "DeadlineChangedEvent": return NoticeSettings.Resources.AssignmentNoticeDeadlineChangedDescription;
          case "ActionItemChangedEvent": return NoticeSettings.Resources.AssignmentNoticeActionItemChangedDescription;
          case "ReturnEvent": return NoticeSettings.Resources.AssignmentNoticeReturnDescription;
          case "OnControlEvent": return NoticeSettings.Resources.AssignmentNoticeOnControlDescription;
          case "AcceptEvent": return NoticeSettings.Resources.AssignmentNoticeControlerAcceptDescription;
          case "ReworkEvent": return NoticeSettings.Resources.AssignmentNoticeControlerReworkDescription;
          case "PerformEvent": return NoticeSettings.Resources.AssignmentNoticePerformDescription;
          case "EscalateEvent": return NoticeSettings.Resources.AssignmentNoticeEscalatedDescription;
          case "AddTimeEvent": return NoticeSettings.Resources.AssignmentNoticeAddTimeRequestDescription;
          case "TimeAcceptEvent": return NoticeSettings.Resources.AssignmentNoticeTimeAcceptDescription;
          case "TimeDeclineEvent": return NoticeSettings.Resources.AssignmentNoticeTimeDeclineDescription;
          case "EightyEvent": return NoticeSettings.Resources.AssignmentNoticeEightyPercentDescription;
          case "SixtyEvent": return NoticeSettings.Resources.AssignmentNoticeSixtyPercentDescription;
          case "FortyEvent": return NoticeSettings.Resources.AssignmentNoticeFortyPercentDescription;
          case "TwentyEvent": return NoticeSettings.Resources.AssignmentNoticeTwentyPercentDescription;
          case "DeadlineEvent": return NoticeSettings.Resources.AssignmentNoticeTodayExpiredDescription;
          case "ExpiredEvent": return NoticeSettings.Resources.AssignmentNoticeDeadlineExpiredDescription;
          default: return string.Empty;
      }
    }
    
    /// <summary>
    /// Установить доступность событий в настройках уведомлений.
    /// </summary>
    /// <param name="isSettingsResponsible">Признак доступности для ответственного.</param>
    public void SetAvailabilityNoticeSettingsEvents(bool isSettingsResponsible)
    {
      #region Блок признаков с событиями.
      
      _obj.State.Properties.IsAssignmentAborts.IsEnabled     = !_obj.IsAAbortsRequired.GetValueOrDefault()     || isSettingsResponsible;
      _obj.State.Properties.IsAssignmentAccept.IsEnabled     = !_obj.IsAAcceptRequired.GetValueOrDefault()     || isSettingsResponsible;
      _obj.State.Properties.IsAssignmentAddTime.IsEnabled    = !_obj.IsAAddTimeRequired.GetValueOrDefault()    || isSettingsResponsible;
      _obj.State.Properties.IsAssignmentDeadline.IsEnabled   = !_obj.IsADeadlineRequired.GetValueOrDefault()   || isSettingsResponsible;
      _obj.State.Properties.IsAssignmentDeclined.IsEnabled   = !_obj.IsADeclinedRequired.GetValueOrDefault()   || isSettingsResponsible;
      _obj.State.Properties.IsAssignmentEighty.IsEnabled     = !_obj.IsAEightyRequired.GetValueOrDefault()     || isSettingsResponsible;
      _obj.State.Properties.IsAssignmentEscalated.IsEnabled  = !_obj.IsAEscalatedRequired.GetValueOrDefault()  || isSettingsResponsible;
      _obj.State.Properties.IsAssignmentExpired.IsEnabled    = !_obj.IsAExpiredRequired.GetValueOrDefault()    || isSettingsResponsible;
      _obj.State.Properties.IsAssignmentForty.IsEnabled      = !_obj.IsAFortyRequired.GetValueOrDefault()      || isSettingsResponsible;
      _obj.State.Properties.IsAssignmentNewSubj.IsEnabled    = !_obj.IsANewSubjRequired.GetValueOrDefault()    || isSettingsResponsible;
      _obj.State.Properties.IsAssignmentOnControl.IsEnabled  = !_obj.IsAOnControlRequired.GetValueOrDefault()  || isSettingsResponsible;
      _obj.State.Properties.IsAssignmentPerform.IsEnabled    = !_obj.IsAPerformRequired.GetValueOrDefault()    || isSettingsResponsible;
      _obj.State.Properties.IsAssignmentRevision.IsEnabled   = !_obj.IsARevisionRequired.GetValueOrDefault()   || isSettingsResponsible;
      _obj.State.Properties.IsAssignmentRework.IsEnabled     = !_obj.IsAReworkRequired.GetValueOrDefault()     || isSettingsResponsible;
      _obj.State.Properties.IsAssignmentSixty.IsEnabled      = !_obj.IsASixtyRequired.GetValueOrDefault()      || isSettingsResponsible;
      _obj.State.Properties.IsAssignmentStarts.IsEnabled     = !_obj.IsAStartsRequired.GetValueOrDefault()     || isSettingsResponsible;
      _obj.State.Properties.IsAssignmentTimeAccept.IsEnabled = !_obj.IsATimeAcceptRequired.GetValueOrDefault() || isSettingsResponsible;
      _obj.State.Properties.IsAssignmentTwenty.IsEnabled     = !_obj.IsATwentyRequired.GetValueOrDefault()     || isSettingsResponsible;
      
      #endregion
      
      #region Блок приоритетов.
      
      _obj.State.Properties.AAbortsPriority.IsEnabled     = !_obj.IsAAbortsRequired.GetValueOrDefault()     || isSettingsResponsible;
      _obj.State.Properties.AAcceptPriority.IsEnabled     = !_obj.IsAAcceptRequired.GetValueOrDefault()     || isSettingsResponsible;
      _obj.State.Properties.AAddTimePriority.IsEnabled    = !_obj.IsAAddTimeRequired.GetValueOrDefault()    || isSettingsResponsible;
      _obj.State.Properties.ADeadlinePriority.IsEnabled   = !_obj.IsADeadlineRequired.GetValueOrDefault()   || isSettingsResponsible;
      _obj.State.Properties.ADeclinedPriority.IsEnabled   = !_obj.IsADeclinedRequired.GetValueOrDefault()   || isSettingsResponsible;
      _obj.State.Properties.AEightyPriority.IsEnabled     = !_obj.IsAEightyRequired.GetValueOrDefault()     || isSettingsResponsible;
      _obj.State.Properties.AEscalatedPriority.IsEnabled  = !_obj.IsAEscalatedRequired.GetValueOrDefault()  || isSettingsResponsible;
      _obj.State.Properties.AExpiredPriority.IsEnabled    = !_obj.IsAExpiredRequired.GetValueOrDefault()    || isSettingsResponsible;
      _obj.State.Properties.AFortyPriority.IsEnabled      = !_obj.IsAFortyRequired.GetValueOrDefault()      || isSettingsResponsible;
      _obj.State.Properties.ANewSubjPriority.IsEnabled    = !_obj.IsANewSubjRequired.GetValueOrDefault()    || isSettingsResponsible;
      _obj.State.Properties.AOnControlPriority.IsEnabled  = !_obj.IsAOnControlRequired.GetValueOrDefault()  || isSettingsResponsible;
      _obj.State.Properties.APerformPriority.IsEnabled    = !_obj.IsAPerformRequired.GetValueOrDefault()    || isSettingsResponsible;
      _obj.State.Properties.ARevisionPriority.IsEnabled   = !_obj.IsARevisionRequired.GetValueOrDefault()   || isSettingsResponsible;
      _obj.State.Properties.AReworkPriority.IsEnabled     = !_obj.IsAReworkRequired.GetValueOrDefault()     || isSettingsResponsible;
      _obj.State.Properties.ASixtyPriority.IsEnabled      = !_obj.IsASixtyRequired.GetValueOrDefault()      || isSettingsResponsible;
      _obj.State.Properties.AStartsPriority.IsEnabled     = !_obj.IsAStartsRequired.GetValueOrDefault()     || isSettingsResponsible;
      _obj.State.Properties.ATimeAcceptPriority.IsEnabled = !_obj.IsATimeAcceptRequired.GetValueOrDefault() || isSettingsResponsible;
      _obj.State.Properties.ATwentyPriority.IsEnabled     = !_obj.IsATwentyRequired.GetValueOrDefault()     || isSettingsResponsible;
      
      #endregion
    }
    
    /// <summary>
    /// Заполнить карточку настройки.
    /// </summary>
    public void FillSetting()
    {
      FillSetting(null);
    }
    
    /// <summary>
    /// Заполнить карточку настройки.
    /// </summary>
    /// <param name="setting">Настройка, из которой берутся значения.</param>
    public void FillSetting(INoticeSetting setting)
    {
      #region Блок признаков с событиями.
      _obj.IsAssignmentAborts     = setting != null ? setting.IsAssignmentAborts.GetValueOrDefault() : false;
      _obj.IsAssignmentAccept   	= setting != null ? setting.IsAssignmentAccept.GetValueOrDefault() : false;
      _obj.IsAssignmentAddTime    = setting != null ? setting.IsAssignmentAddTime.GetValueOrDefault() : false;
      _obj.IsAssignmentDeadline   = setting != null ? setting.IsAssignmentDeadline.GetValueOrDefault() : false;
      _obj.IsAssignmentDeclined   = setting != null ? setting.IsAssignmentDeclined.GetValueOrDefault() : false;
      _obj.IsAssignmentEighty     = setting != null ? setting.IsAssignmentEighty.GetValueOrDefault() : false;
      _obj.IsAssignmentEscalated  = setting != null ? setting.IsAssignmentEscalated.GetValueOrDefault() : false;
      _obj.IsAssignmentExpired    = setting != null ? setting.IsAssignmentExpired.GetValueOrDefault() : false;
      _obj.IsAssignmentForty      = setting != null ? setting.IsAssignmentForty.GetValueOrDefault() : false;
      _obj.IsAssignmentNewSubj    = setting != null ? setting.IsAssignmentNewSubj.GetValueOrDefault() : false;
      _obj.IsAssignmentOnControl  = setting != null ? setting.IsAssignmentOnControl.GetValueOrDefault() : false;
      _obj.IsAssignmentPerform    = setting != null ? setting.IsAssignmentPerform.GetValueOrDefault() : false;
      _obj.IsAssignmentRevision   = setting != null ? setting.IsAssignmentRevision.GetValueOrDefault() : false;
      _obj.IsAssignmentRework     = setting != null ? setting.IsAssignmentRework.GetValueOrDefault() : false;
      _obj.IsAssignmentSixty      = setting != null ? setting.IsAssignmentSixty.GetValueOrDefault() : false;
      _obj.IsAssignmentStarts     = setting != null ? setting.IsAssignmentStarts.GetValueOrDefault() : false;
      _obj.IsAssignmentTimeAccept = setting != null ? setting.IsAssignmentTimeAccept.GetValueOrDefault() : false;
      _obj.IsAssignmentTwenty     = setting != null ? setting.IsAssignmentTwenty.GetValueOrDefault() : false;
      #endregion
      
      #region Блок кнопок "Неотключаемый".
      _obj.IsAAbortsRequired     = setting != null ? setting.IsAAbortsRequired.GetValueOrDefault() : false;
      _obj.IsAAcceptRequired     = setting != null ? setting.IsAAcceptRequired.GetValueOrDefault() : false;
      _obj.IsAAddTimeRequired    = setting != null ? setting.IsAAddTimeRequired.GetValueOrDefault() : false;
      _obj.IsADeadlineRequired   = setting != null ? setting.IsADeadlineRequired.GetValueOrDefault() : false;
      _obj.IsADeclinedRequired   = setting != null ? setting.IsADeclinedRequired.GetValueOrDefault() : false;
      _obj.IsAEightyRequired     = setting != null ? setting.IsAEightyRequired.GetValueOrDefault() : false;
      _obj.IsAEscalatedRequired  = setting != null ? setting.IsAEscalatedRequired.GetValueOrDefault() : false;
      _obj.IsAExpiredRequired    = setting != null ? setting.IsAExpiredRequired.GetValueOrDefault() : false;
      _obj.IsAFortyRequired      = setting != null ? setting.IsAFortyRequired.GetValueOrDefault() : false;
      _obj.IsANewSubjRequired    = setting != null ? setting.IsANewSubjRequired.GetValueOrDefault() : false;
      _obj.IsAOnControlRequired  = setting != null ? setting.IsAOnControlRequired.GetValueOrDefault() : false;
      _obj.IsAPerformRequired    = setting != null ? setting.IsAPerformRequired.GetValueOrDefault() : false;
      _obj.IsARevisionRequired   = setting != null ? setting.IsARevisionRequired.GetValueOrDefault() : false;
      _obj.IsAReworkRequired     = setting != null ? setting.IsAReworkRequired.GetValueOrDefault() : false;
      _obj.IsASixtyRequired      = setting != null ? setting.IsASixtyRequired.GetValueOrDefault() : false;
      _obj.IsAStartsRequired     = setting != null ? setting.IsAStartsRequired.GetValueOrDefault() : false;
      _obj.IsATimeAcceptRequired = setting != null ? setting.IsATimeAcceptRequired.GetValueOrDefault() : false;
      _obj.IsATwentyRequired     = setting != null ? setting.IsATwentyRequired.GetValueOrDefault() : false;
      #endregion
      
      #region Блок приоритетов.
      if (setting != null)
      {
        foreach (IPriority priority in setting.AAbortsPriority.Select(p => p.Priority).ToList())
          _obj.AAbortsPriority.AddNew().Priority = priority;
        
        foreach (IPriority priority in setting.AAcceptPriority.Select(p => p.Priority).ToList())
          _obj.AAcceptPriority.AddNew().Priority = priority;
        
        foreach (IPriority priority in setting.AAddTimePriority.Select(p => p.Priority).ToList())
          _obj.AAddTimePriority.AddNew().Priority = priority;
        
        foreach (IPriority priority in setting.ADeadlinePriority.Select(p => p.Priority).ToList())
          _obj.ADeadlinePriority.AddNew().Priority = priority;
        
        foreach (IPriority priority in setting.ADeclinedPriority.Select(p => p.Priority).ToList())
          _obj.ADeclinedPriority.AddNew().Priority = priority;
        
        foreach (IPriority priority in setting.AEightyPriority.Select(p => p.Priority).ToList())
          _obj.AEightyPriority.AddNew().Priority = priority;
        
        foreach (IPriority priority in setting.AEscalatedPriority.Select(p => p.Priority).ToList())
          _obj.AEscalatedPriority.AddNew().Priority = priority;
        
        foreach (IPriority priority in setting.AExpiredPriority.Select(p => p.Priority).ToList())
          _obj.AExpiredPriority.AddNew().Priority = priority;
        
        foreach (IPriority priority in setting.AFortyPriority.Select(p => p.Priority).ToList())
          _obj.AFortyPriority.AddNew().Priority = priority;
        
        foreach (IPriority priority in setting.ANewSubjPriority.Select(p => p.Priority).ToList())
          _obj.ANewSubjPriority.AddNew().Priority = priority;
        
        foreach (IPriority priority in setting.AOnControlPriority.Select(p => p.Priority).ToList())
          _obj.AOnControlPriority.AddNew().Priority = priority;
        
        foreach (IPriority priority in setting.APerformPriority.Select(p => p.Priority).ToList())
          _obj.APerformPriority.AddNew().Priority = priority;
        
        foreach (IPriority priority in setting.ARevisionPriority.Select(p => p.Priority).ToList())
          _obj.ARevisionPriority.AddNew().Priority = priority;
        
        foreach (IPriority priority in setting.AReworkPriority.Select(p => p.Priority).ToList())
          _obj.AReworkPriority.AddNew().Priority = priority;
        
        foreach (IPriority priority in setting.ASixtyPriority.Select(p => p.Priority).ToList())
          _obj.ASixtyPriority.AddNew().Priority = priority;
        
        foreach (IPriority priority in setting.AStartsPriority.Select(p => p.Priority).ToList())
          _obj.AStartsPriority.AddNew().Priority = priority;
        
        foreach (IPriority priority in setting.ATimeAcceptPriority.Select(p => p.Priority).ToList())
          _obj.ATimeAcceptPriority.AddNew().Priority = priority;
        
        foreach (IPriority priority in setting.ATwentyPriority.Select(p => p.Priority).ToList())
          _obj.ATwentyPriority.AddNew().Priority = priority;
      }
      #endregion
    }
  }
}