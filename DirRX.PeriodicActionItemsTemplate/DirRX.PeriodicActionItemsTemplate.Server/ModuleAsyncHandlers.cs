using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PeriodicActionItemsTemplate.Server
{
  public class ModuleAsyncHandlers
  {

    public virtual void StartPeriodicActionItem(DirRX.PeriodicActionItemsTemplate.Server.AsyncHandlerInvokeArgs.StartPeriodicActionItemInvokeArgs args)
    {
      var logger = Logger.WithLogger("StartPeriodicActionItem").WithProperty("ScheduleItemId", args.ScheduleItemId).WithProperty("RetryIteration", args.RetryIteration);
      
      logger.Debug("Start");
      
      var scheduleItem = ScheduleItems.GetAll(si => si.Id == args.ScheduleItemId).FirstOrDefault();
      
      if (scheduleItem == null || scheduleItem.Status == DirRX.PeriodicActionItemsTemplate.ScheduleItem.Status.Closed)
      {
        logger.Error("Schedule item is null or closed");
        return;
      }
      
      if (args.RetryIteration > 3)
      {
        // Отправляем уведомление администратору.
        var adminTask = Sungero.Workflow.SimpleTasks.CreateWithNotices(DirRX.PeriodicActionItemsTemplate.Resources.AdminErrorTaskSubject,
                                                                       Functions.Module.GetResponsibleForPeriodic());
        adminTask.Attachments.Add(scheduleItem);
        adminTask.ActiveText = DirRX.PeriodicActionItemsTemplate.Resources.AdminErrorTaskActiveTextFormat(args.ErrorMessage);
        adminTask.Start();
        return;
      }
      
      
      if (!Locks.TryLock(scheduleItem))
      {
        logger.Debug("Schedule Item with Id = {id} is locked. Sent to retry", scheduleItem.Id);
        args.Retry = true;
        return;
      }
      
      try
      {
        Functions.Module.StartActionItemFromScheduleItem(scheduleItem);
      }
      catch (Exception ex)
      {
        logger.Error(ex, "An error occured while creating Action Item from Schedule Item with Id = {id}", scheduleItem.Id);
        args.Retry = true;
        args.ErrorMessage = ex.Message;
      }
      finally
      {
        Locks.Unlock(scheduleItem);
      }
      
      logger.Debug("Finish");
    }

    public virtual void CreateSchedule(DirRX.PeriodicActionItemsTemplate.Server.AsyncHandlerInvokeArgs.CreateScheduleInvokeArgs args)
    {
      var logger = Logger.WithLogger("CreateSchedule").WithProperty("RepeatSettingId", args.RepeatSettingId);
      
      logger.Debug("Start");
      
      var repeatSetting = RepeatSettings.GetAll(rs => rs.Id == args.RepeatSettingId).FirstOrDefault();
      
      if (repeatSetting == null)
      {
        logger.Error("Record doesn't exists");
        return;
      }
      
      if (repeatSetting.Status == DirRX.PeriodicActionItemsTemplate.RepeatSetting.Status.Closed)
      {
        logger.Debug("Record already closed");
        return;
      }
      
      var maxCalendarDate = WorkingTime.GetAll().OrderByDescending(x => x.Year).First().Day.Where(d => !d.Kind.HasValue).OrderByDescending(d => d.Day).First().Day;
      
      var maxDate = repeatSetting.EndDate ?? repeatSetting.EndMonth ?? repeatSetting.EndYear;
      if (maxDate == null || maxDate > maxCalendarDate)
        maxDate = maxCalendarDate;
      
      try
      {
        if (repeatSetting.Type == DirRX.PeriodicActionItemsTemplate.RepeatSetting.Type.Day)
        {
          Functions.Module.CreateScheduleItemsForDailySchedule(repeatSetting, maxDate.Value.Date, args.CheckDuplicates);
        }
        
        if (repeatSetting.Type == DirRX.PeriodicActionItemsTemplate.RepeatSetting.Type.Week)
        {
          Functions.Module.CreateScheduleItemsForWeeklySchedule(repeatSetting, maxDate.Value.Date, args.CheckDuplicates);
        }
        
        if (repeatSetting.Type == DirRX.PeriodicActionItemsTemplate.RepeatSetting.Type.Month)
        {
          Functions.Module.CreateScheduleItemsForMonthlySchedule(repeatSetting, maxDate.Value.Date, args.CheckDuplicates);
        }
        
        if (repeatSetting.Type == DirRX.PeriodicActionItemsTemplate.RepeatSetting.Type.Year)
        {
          Functions.Module.CreateScheduleItemsForAnnualSchedule(repeatSetting, maxDate.Value.Date, args.CheckDuplicates);
        }
      }
      catch (Exception ex)
      {
        logger.Error(ex, "An error occured while creating schedule");
      }
      
      logger.Debug("Finish");
    }

    public virtual void CloseScheduleItem(DirRX.PeriodicActionItemsTemplate.Server.AsyncHandlerInvokeArgs.CloseScheduleItemInvokeArgs args)
    {
      var logger = Logger.WithLogger("CloseScheduleItem").WithProperty("ScheduleItemId", args.ScheduleItemId);
      
      logger.Debug("Start");
      
      var scheduleItem = ScheduleItems.GetAll(si => si.Id == args.ScheduleItemId).FirstOrDefault();
      
      if (scheduleItem == null)
      {
        logger.Error("Record doesn't exists");
        return;
      }
      
      if (!Locks.TryLock(scheduleItem))
      {
        logger.Error("Record is locked. Sent to retry");
        args.Retry = true;
        return;
      }
      
      try
      {
        scheduleItem.Status = PeriodicActionItemsTemplate.ScheduleItem.Status.Closed;
        scheduleItem.Save();
        logger.Debug("Record closed");
      }
      catch (Exception ex)
      {
        logger.Error(ex, "An error occured while closing record");
        return;
      }
      finally
      {
        Locks.Unlock(scheduleItem);
      }
      
      logger.Debug("Start");
    }

  }
}