using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PeriodicActionItemsTemplate.RepeatSetting;

namespace DirRX.PeriodicActionItemsTemplate.Client
{
  partial class RepeatSettingFunctions
  {
    /// <summary>
    /// �������� ������ ������ �������� ��� ������������ ������� ���������� ��� ������� �������.
    /// </summary>
    /// <returns>null - ���� ������ ������, false - �������� ������������, true - �������.</returns>
    public static bool? ShowSelectActionForActiveScheduleItemsDialog()
    {
      var dialog = Dialogs.CreateTaskDialog(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.SelectActionForActiveScheduleItemsDialogText,
                                            DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.SelectActionForActiveScheduleItemsDialogDescription,
                                            MessageType.Question);
      
      var keepActiveButton = dialog.Buttons.AddCustom(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.KeepActive);
      var closeButton = dialog.Buttons.AddCustom(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.Close);
      var cancelButton = dialog.Buttons.AddCancel();
      
      dialog.Buttons.Default = cancelButton;
      
      var resultButton = dialog.Show();
      
      if (resultButton == cancelButton)
        return null;
      
      if (resultButton == keepActiveButton)
        return false;
      
      if (resultButton == closeButton)
        return true;
      
      return null;
    }
    
    public virtual List<Structures.RepeatSetting.ArbitraryScheduleItem> ShowArbitraryScheduleCreationDialog()
    {
      var dialog = Dialogs.CreateInputDialog(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.ArbitraryScheduleCreationDialogText);
      
      var startDateControl = dialog.AddDate(ScheduleItems.Info.Properties.StartDate.LocalizedName, false);
      var deadlineControl = dialog.AddDate(ScheduleItems.Info.Properties.Deadline.LocalizedName, false);
      
      var nextScheduleButton = dialog.Buttons.AddCustom(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.Next);
      dialog.Buttons.AddOkCancel();
      
      var scheduleItems = new List<Structures.RepeatSetting.ArbitraryScheduleItem>();
      
      dialog.SetOnButtonClick(
        args =>
        {
          if (args.Button == nextScheduleButton || args.Button == DialogButtons.Ok)
          {
            // ������ ������ ��������� ������ ����������� ������������� IsRequired, �.�. ������������� ��������� ������������ ��� ����� ����� ������� � �������� �� ��� ��������� ����.
            var hasValidationError = false;
            if (!startDateControl.Value.HasValue)
            {
              args.AddError(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.RequiredPropertyIsEmptyFormat(ScheduleItems.Info.Properties.StartDate.LocalizedName), startDateControl);
              hasValidationError = true;
            }
            else if (startDateControl.Value < Calendar.Today)
            {
              args.AddError(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.StartDateCannotBeLessThanToday, startDateControl);
              hasValidationError = true;
            }
            if (_obj.HasIndefiniteDeadline != true && !deadlineControl.Value.HasValue)
            {
              args.AddError(DirRX.PeriodicActionItemsTemplate.RepeatSettings.Resources.RequiredPropertyIsEmptyFormat(ScheduleItems.Info.Properties.Deadline.LocalizedName), deadlineControl);
              hasValidationError = true;
            }
            
            if (hasValidationError)
              return;
            
            scheduleItems.Add(Structures.RepeatSetting.ArbitraryScheduleItem.Create(startDateControl.Value.Value, deadlineControl.Value));
            startDateControl.Value = null;
            deadlineControl.Value = null;
            args.CloseAfterExecute = args.Button == DialogButtons.Ok;
          }
        });
      
      var resultButton = dialog.Show();
      
      if (resultButton == DialogButtons.Ok)
        return scheduleItems;
      
      return new List<Structures.RepeatSetting.ArbitraryScheduleItem>();
    }
  }
}