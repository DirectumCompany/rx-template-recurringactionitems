using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.RepeatSetting;

namespace DirRX.ActionItems
{
  partial class RepeatSettingActionItemsPartsClientHandlers
  {

    public virtual void ActionItemsPartsNumberValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      // Проверить число на положительность.
      if (e.NewValue < 1)
        e.AddError(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.NumberIsNotPositive);
    }

    public virtual void ActionItemsPartsActionItemPartValueInput(Sungero.Presentation.TextValueInputEventArgs e)
    {
      if (!string.IsNullOrEmpty(e.NewValue))
        e.NewValue = e.NewValue.Trim();
      
      var resolutionPoint = e.NewValue;
      var allowableResolutionLength = RepeatSettings.Info.Properties.ActionItem.Length;
      if (!string.IsNullOrEmpty(resolutionPoint) && resolutionPoint.Length > allowableResolutionLength)
        e.AddError(Sungero.RecordManagement.ActionItemExecutionTasks.Resources.AllowableLengthAssignmentsCharactersFormat(allowableResolutionLength));
    }
  }

  partial class RepeatSettingClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      if (CallContext.CalledFrom(DirRX.Solution.ActionItemExecutionTasks.Info))
      {
        var task = DirRX.Solution.PublicFunctions.ActionItemExecutionTask.Remote.GetActionItemExecutionTask(CallContext.GetCallerEntityId(DirRX.Solution.ActionItemExecutionTasks.Info));
        
        if (task != null)
        {
          _obj.State.Properties.ActionItem.IsEnabled = false;
          _obj.State.Properties.ActionItemsParts.IsEnabled = false;
          _obj.State.Properties.AssignedBy.IsEnabled = false;
          _obj.State.Properties.Assignee.IsEnabled = false;
          _obj.State.Properties.Category.IsEnabled = false;
          _obj.State.Properties.CoAssignees.IsEnabled = false;
          _obj.State.Properties.Initiator.IsEnabled = false;
          _obj.State.Properties.IsUnderControl.IsEnabled = false;
          _obj.State.Properties.Mark.IsEnabled = false;
          _obj.State.Properties.ReportDeadline.IsEnabled = false;
          _obj.State.Properties.Subscribers.IsEnabled = false;
          _obj.State.Properties.Supervisor.IsEnabled = false;
        }
      }
    }

    public virtual void MonthTypeDayValueValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && (e.NewValue <= 0 || e.NewValue > 31))
        e.AddError(RepeatSettings.Resources.IncorrectDayValueFormat(31));
    }

    public virtual void YearTypeDayValueValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue)
      {
        if ((_obj.YearTypeMonth == ActionItems.RepeatSetting.YearTypeMonth.January ||
             _obj.YearTypeMonth == ActionItems.RepeatSetting.YearTypeMonth.March ||
             _obj.YearTypeMonth == ActionItems.RepeatSetting.YearTypeMonth.May ||
             _obj.YearTypeMonth == ActionItems.RepeatSetting.YearTypeMonth.July ||
             _obj.YearTypeMonth == ActionItems.RepeatSetting.YearTypeMonth.August ||
             _obj.YearTypeMonth == ActionItems.RepeatSetting.YearTypeMonth.October ||
             _obj.YearTypeMonth == ActionItems.RepeatSetting.YearTypeMonth.December) &&
            (e.NewValue <= 0 || e.NewValue > 31))
          e.AddError(RepeatSettings.Resources.IncorrectDayValueFormat(31));
        
        if ((_obj.YearTypeMonth == ActionItems.RepeatSetting.YearTypeMonth.April ||
             _obj.YearTypeMonth == ActionItems.RepeatSetting.YearTypeMonth.June ||
             _obj.YearTypeMonth == ActionItems.RepeatSetting.YearTypeMonth.September ||
             _obj.YearTypeMonth == ActionItems.RepeatSetting.YearTypeMonth.November) &&
            (e.NewValue <= 0 || e.NewValue > 30))
          e.AddError(RepeatSettings.Resources.IncorrectDayValueFormat(30));
        
        if (_obj.YearTypeMonth == ActionItems.RepeatSetting.YearTypeMonth.February && (e.NewValue <= 0 || e.NewValue > 28))
          e.AddError(RepeatSettings.Resources.IncorrectDayValueFormat(28));
      }
    }

    public virtual void CreationDaysValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue < 0)
        e.AddError(RepeatSettings.Resources.IncorrectValue);
    }

    public virtual void RepeatValueValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
    {
      if (e.NewValue.HasValue && e.NewValue <= 0)
        e.AddError(RepeatSettings.Resources.IncorrectValue);
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      Functions.RepeatSetting.SetStateproperties(_obj);

      if (_obj.Type == DirRX.ActionItems.RepeatSetting.Type.Month && _obj.MonthTypeDayValue.HasValue && _obj.MonthTypeDayValue.Value > 28 && _obj.MonthTypeDayValue.Value <= 31)
        e.AddInformation(ActionItems.RepeatSettings.Resources.IncorrectDaysOfMonthFormat(_obj.MonthTypeDayValue.Value));
      
      #region Скопировано из стандартной.
      
      var isComponentResolution = _obj.IsCompoundActionItem ?? false;

      var properties = _obj.State.Properties;
      
      properties.ActionItemsParts.IsVisible = isComponentResolution;
      
      properties.Assignee.IsVisible = !isComponentResolution;
      properties.CoAssignees.IsVisible = !isComponentResolution;

      properties.Supervisor.IsEnabled = (_obj.IsUnderControl ?? false) && _obj.State.Properties.Assignee.IsEnabled;
      
      e.Title = (_obj.Subject == Sungero.Docflow.Resources.AutoformatTaskSubject) ? null : _obj.Subject;
      
      #endregion
    }

  }
}