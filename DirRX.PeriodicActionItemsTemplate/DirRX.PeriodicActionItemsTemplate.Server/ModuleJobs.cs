using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Metadata.Services;
using Sungero.Domain.Shared;

namespace DirRX.PeriodicActionItemsTemplate.Server
{
	public class ModuleJobs
	{
		
		#region Повторяющиеся поручения.
		
		/// <summary>
		/// Отправление повторяющихся поручений.
		/// </summary>
		public virtual void RepeatActionItemExecutionTasks()
		{
			var repeatSettings = RepeatSettings.GetAll(s => s.Status == PeriodicActionItemsTemplate.RepeatSetting.Status.Active);
			var createdTask = Sungero.RecordManagement.ActionItemExecutionTasks.GetAll(t => t.Started.HasValue && t.Started.Value.Date == Calendar.Today.Date);
			
			foreach (var setting in repeatSettings)
			{
				if (setting.StartedActionItemTask.Any(x => createdTask.Any(y => Sungero.RecordManagement.ActionItemExecutionTasks.Equals(x.ActionItemTask, y))))
				{
					Logger.DebugFormat("Action item sent.");
					continue;
				}
				
				if (setting.Type == null || setting.CreationDays == null)
					continue;
				
				Logger.Debug(string.Format("Setting with id = {0} processed. Type = {1}", setting.Id, setting.Type.Value.Value));
				var date = Calendar.Today;
				var deadlineDate = Calendar.Today.AddWorkingDays(setting.AssignedBy, setting.CreationDays.Value);
				
				#region Ежегодно.
				
				if (setting.Type == PeriodicActionItemsTemplate.RepeatSetting.Type.Year)
				{
					var beginningDate = setting.BeginningYear.Value;
					var endDate = setting.EndYear.HasValue ? setting.EndYear.Value : Calendar.SqlMaxValue;
					
					if (!Calendar.Between(deadlineDate, beginningDate, endDate))
					{
						Logger.DebugFormat("Date misses the period. Deadline = {0}. Settings: begin = {1}, end = {2}", deadlineDate.Year, beginningDate.Year, endDate.Year);
						continue;
					}
					
					var period = setting.RepeatValue.HasValue ? setting.RepeatValue.Value : 1;
					
					// Проверка соответствия года.
					if (!IsCorrectYear(period, beginningDate.Year, deadlineDate.Year))
					{
						Logger.DebugFormat("Incorrect year. Current = {0}. Settings: begin = {1}, period = {2}", deadlineDate.Year, beginningDate.Year, period);
						continue;
					}
					
					// Проверка соответствия месяца.
					var month = GetMonthValue(setting.YearTypeMonth.GetValueOrDefault());
					if (deadlineDate.Month != month)
					{
						Logger.DebugFormat("Incorrect month. Current = {0}. In setting = {1}", deadlineDate.Month, month);
						continue;
					}
					
					if (setting.YearTypeDay == PeriodicActionItemsTemplate.RepeatSetting.YearTypeDay.Date)
					{
						try
						{
							date = Calendar.GetDate(deadlineDate.Year, month, setting.YearTypeDayValue.GetValueOrDefault());
							if (!date.IsWorkingDay(setting.AssignedBy))
								date = date.PreviousWorkingDay(setting.AssignedBy);
							
							if (deadlineDate.Day == date.Day)
							{
								SendActionItem(setting, deadlineDate);
								
								if (setting.EndYear.HasValue && deadlineDate.AddYears(period).Year > setting.EndYear.Value.Year)
									SendNotice(setting);
							}
							else
								Logger.DebugFormat("Incorrect day. Current = {0}. Is setting (working day) = {1}", deadlineDate.Day, date.Day);
						}
						catch
						{
							Logger.ErrorFormat("Incorrect data for date. Year = {0}, month = {1}, day = {2}",
							                   deadlineDate.Year, month, setting.YearTypeDayValue.GetValueOrDefault());
						}
					}
					else
					{
						try
						{
							var beginningMonth = Calendar.GetDate(deadlineDate.Year, month, 1);
							date = GetDateTime(setting.YearTypeDayOfWeek.Value, setting.YearTypeDayOfWeekNumber.Value, beginningMonth);
							if (!date.IsWorkingDay(setting.AssignedBy))
								date = date.PreviousWorkingDay(setting.AssignedBy);
							
							if (date.Date == deadlineDate.Date)
							{
								SendActionItem(setting, deadlineDate);
								
								if (setting.EndYear.HasValue && deadlineDate.AddYears(period).Year > setting.EndYear.Value.Year)
									SendNotice(setting);
							}
							else
								Logger.DebugFormat("Incorrect day. Current = {0}. Settings: Day of week = {1}, week number = {2}. working day = {3}",
								                   deadlineDate.Day, setting.YearTypeDayOfWeek.Value.Value, setting.YearTypeDayOfWeekNumber.Value.Value, date.Day);
						}
						catch
						{
							Logger.ErrorFormat("Incorrect data for date. Year = {0}, month = {1}, day of week = {2}, week number = {3}",
							                   deadlineDate.Year, month, setting.YearTypeDayOfWeek.Value.Value, setting.YearTypeDayOfWeekNumber.Value.Value);
						}
					}
				}
				
				#endregion
				
				#region Ежемесячно.
				
				if (setting.Type == PeriodicActionItemsTemplate.RepeatSetting.Type.Month)
				{
					var beginningDate = setting.BeginningMonth.Value;
					var period = setting.RepeatValue.HasValue ? setting.RepeatValue.Value : 1;
					var endDate = setting.EndMonth.HasValue ? setting.EndMonth.Value.EndOfMonth() : Calendar.SqlMaxValue;
					
					if (!Calendar.Between(deadlineDate, beginningDate, endDate))
					{
						Logger.DebugFormat("Date misses the period. Deadline = {0}. Settings: begin = {1}, end = {2}", deadlineDate, beginningDate, endDate);
						continue;
					}
					
					// Проверка соответствия месяца. Если year = null пропускаем вычисления.
					if (!IsCorrectMonth(period, beginningDate, deadlineDate))
					{
						Logger.DebugFormat("Incorrect month. Current = {0}. Settings: begin = {1}, period = {2}", deadlineDate.Month, beginningDate, period);
						continue;
					}
					
					if (setting.MonthTypeDay == PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDay.Date)
					{
						try
						{
							var day = setting.MonthTypeDayValue.GetValueOrDefault();
							var dateString = string.Format("{0}/{1}/{2}", day.ToString(), deadlineDate.Month.ToString(), deadlineDate.Year.ToString());
							
							// Обработка несуществующего числа в месяце (например 30 число в феврале).
							while (!Calendar.TryParseDate(dateString, out date))
							{
								Logger.DebugFormat("Incorrect date. String = {0}", dateString);
								day--;
								dateString = string.Format("{0}/{1}/{2}", day.ToString(), deadlineDate.Month.ToString(), deadlineDate.Year.ToString());
							}
							
							if (!date.IsWorkingDay(setting.AssignedBy))
								date = date.PreviousWorkingDay(setting.AssignedBy);
							
							if (deadlineDate.Day == date.Day)
							{
								SendActionItem(setting, deadlineDate);
								
								var nextDeadline = deadlineDate.AddMonths(period);
								if (setting.EndMonth.HasValue && nextDeadline.EndOfMonth() > setting.EndMonth.Value)
									SendNotice(setting);
							}
							else
								Logger.DebugFormat("Incorrect day. Current = {0}. Is setting (working day) = {1}", deadlineDate.Day, date.Day);
						}
						catch
						{
							Logger.ErrorFormat("Incorrect data for date. Year = {0}, month = {1}, day = {2}",
							                   deadlineDate.Year, deadlineDate.Month, setting.MonthTypeDayValue.GetValueOrDefault());
						}
					}
					else
					{
						try
						{
							var beginningMonth = Calendar.GetDate(deadlineDate.Year, deadlineDate.Month, 1);
							date = GetDateTime(setting.MonthTypeDayOfWeek.Value, setting.MonthTypeDayOfWeekNumber.Value, beginningMonth);
							if (!date.IsWorkingDay(setting.AssignedBy))
								date = date.PreviousWorkingDay(setting.AssignedBy);
							
							if (date.Date == deadlineDate.Date)
							{
								SendActionItem(setting, deadlineDate);
								
								var nextDeadline = deadlineDate.AddMonths(period);
								if (setting.EndMonth.HasValue && nextDeadline.EndOfMonth() > setting.EndMonth.Value)
									SendNotice(setting);
							}
							else
								Logger.DebugFormat("Incorrect day. Current = {0}. Settings: Day of week = {1}, week number = {2}. working day = {3}",
								                   deadlineDate.Day, setting.MonthTypeDayOfWeek.Value.Value, setting.MonthTypeDayOfWeekNumber.Value.Value, date.Day);
						}
						catch
						{
							Logger.ErrorFormat("Incorrect data for date. Year = {0}, month = {1}, day of week = {2}, week number = {3}",
							                   deadlineDate.Year, deadlineDate.Month, setting.MonthTypeDayOfWeek.Value.Value, setting.MonthTypeDayOfWeekNumber.Value.Value);
						}
					}
				}
				
				#endregion
				
				#region Еженедельно.
				
				if (setting.Type == PeriodicActionItemsTemplate.RepeatSetting.Type.Week)
				{
					var beginningDate = setting.BeginningDate.Value;
					var endDate = setting.EndDate.HasValue ? setting.EndDate.Value.EndOfDay() : Calendar.SqlMaxValue;
					
					if (!Calendar.Between(deadlineDate, beginningDate, endDate))
					{
						Logger.DebugFormat("Date misses the period. Deadline = {0}. Settings: begin = {1}, end = {2}", deadlineDate, beginningDate, endDate);
						continue;
					}
					
					var daysOfWeek = new List<DayOfWeek>();
					
					if (setting.WeekTypeMonday.Value)
						daysOfWeek.Add(DayOfWeek.Monday);
					if (setting.WeekTypeTuesday.Value)
						daysOfWeek.Add(DayOfWeek.Tuesday);
					if (setting.WeekTypeWednesday.Value)
						daysOfWeek.Add(DayOfWeek.Wednesday);
					if (setting.WeekTypeThursday.Value)
						daysOfWeek.Add(DayOfWeek.Thursday);
					if (setting.WeekTypeFriday.Value)
						daysOfWeek.Add(DayOfWeek.Friday);
					
					// Вычисляем количество дней между неделями и вычитаем 6, чтобы учесть первую неделю периода.
					// Если количество дней целочисленно делится на период, то неделя в него попадает.
					TimeSpan timeSpan = deadlineDate.EndOfWeek() - beginningDate.BeginningOfWeek();
					var daysCount = timeSpan.Days - 6;
					var periodDays = 7 * (setting.RepeatValue.HasValue ? setting.RepeatValue.Value : 1);
					
					if (daysCount % periodDays == 0 && daysOfWeek.Contains(deadlineDate.DayOfWeek))
					{
						SendActionItem(setting, deadlineDate);
						
						if (setting.EndDate.HasValue && !daysOfWeek.Any(d => d > deadlineDate.DayOfWeek) &&
						    deadlineDate.EndOfWeek().AddDays(periodDays) > setting.EndDate.Value)
							SendNotice(setting);
					}
				}
				
				#endregion
				
				#region Ежедневно.
				
				if (setting.Type == PeriodicActionItemsTemplate.RepeatSetting.Type.Day)
				{
					var beginningDate = setting.BeginningDate.Value;
					var endDate = setting.EndDate.HasValue ? setting.EndDate.Value.EndOfDay() : Calendar.SqlMaxValue;
					
					if (!Calendar.Between(deadlineDate, beginningDate, endDate))
					{
						Logger.DebugFormat("Date misses the period. Deadline = {0}. Settings: begin = {1}, end = {2}", deadlineDate, beginningDate, endDate);
						continue;
					}
					
					var periodValue = setting.RepeatValue.HasValue ? setting.RepeatValue.Value : 1;
					if (periodValue == 1)
					{
						SendActionItem(setting, deadlineDate);
						
						if (setting.EndDate.HasValue && deadlineDate.AddWorkingDays(setting.AssignedBy, periodValue) > setting.EndDate.Value)
							SendNotice(setting);
					}
					else
					{
						// Прибавляем 1, чтобы учесть первый день в периоде.
						var daysCount = WorkingTime.GetDurationInWorkingDays(beginningDate, deadlineDate, setting.AssignedBy) + 1;
						
						if (daysCount % periodValue == 0)
						{
							SendActionItem(setting, deadlineDate);
							
							if (setting.EndDate.HasValue && deadlineDate.AddWorkingDays(setting.AssignedBy, periodValue) > setting.EndDate.Value)
								SendNotice(setting);
						}
					}
				}
				
				#endregion
			}
		}
		
		/// <summary>
		/// Получить дату соответствующую настройкам.
		/// </summary>
		/// <param name="week">День недели.</param>
		/// <param name="weekNumber">Порядковый номер недели.</param>
		/// <param name="date">Дата, от которой происходит отсчёт.</param>
		/// <returns>Дата.</returns>
		private DateTime GetDateTime(Sungero.Core.Enumeration dayOfWeekSetting, Sungero.Core.Enumeration dayOfWeekNumberSetting, DateTime date)
		{
			var dayOfWeek = DayOfWeek.Monday;
			
			if (dayOfWeekSetting == PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeek.Monday || dayOfWeekSetting == PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeek.Monday)
				dayOfWeek = DayOfWeek.Monday;
			if (dayOfWeekSetting == PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeek.Tuesday || dayOfWeekSetting ==PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeek.Tuesday)
				dayOfWeek = DayOfWeek.Tuesday;
			if (dayOfWeekSetting == PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeek.Wednesday || dayOfWeekSetting ==PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeek.Wednesday)
				dayOfWeek = DayOfWeek.Wednesday;
			if (dayOfWeekSetting == PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeek.Thursday || dayOfWeekSetting == PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeek.Thursday)
				dayOfWeek = DayOfWeek.Thursday;
			if (dayOfWeekSetting == PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeek.Friday || dayOfWeekSetting == PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeek.Friday)
				dayOfWeek = DayOfWeek.Friday;
			
			while (date.DayOfWeek != dayOfWeek)
				date = date.NextDay();
			
			var month = date.Month;
			if (dayOfWeekNumberSetting == PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeekNumber.Last || dayOfWeekNumberSetting == PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeekNumber.Last)
			{
				while (date.AddDays(7).Month == month)
					date = date.AddDays(7);
			}
			else
			{
				if (dayOfWeekNumberSetting == PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeekNumber.Second || dayOfWeekNumberSetting == PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeekNumber.Second)
					date = date.AddDays(7);
				if (dayOfWeekNumberSetting == PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeekNumber.Third || dayOfWeekNumberSetting == PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeekNumber.Third)
					date = date.AddDays(14);
				if (dayOfWeekNumberSetting == PeriodicActionItemsTemplate.RepeatSetting.YearTypeDayOfWeekNumber.Fourth || dayOfWeekNumberSetting == PeriodicActionItemsTemplate.RepeatSetting.MonthTypeDayOfWeekNumber.Fourth)
					date = date.AddDays(21);
			}
			
			return date;
		}
		
		#endregion
		
		#region Ежегодно.
		
		/// <summary>
		/// Получить численное представление месяца.
		/// </summary>
		/// <param name="month">Месяц из перечисления настроек.</param>
		/// <returns>Число от 1 до 12.</returns>
		private int GetMonthValue(Sungero.Core.Enumeration month)
		{
			if (month == PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.January)
				return 1;
			if (month == PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.February)
				return 2;
			if (month == PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.March)
				return 3;
			if (month == PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.April)
				return 4;
			if (month == PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.May)
				return 5;
			if (month == PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.June)
				return 6;
			if (month == PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.July)
				return 7;
			if (month == PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.August)
				return 8;
			if (month == PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.September)
				return 9;
			if (month == PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.October)
				return 10;
			if (month == PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.November)
				return 11;
			if (month == PeriodicActionItemsTemplate.RepeatSetting.YearTypeMonth.December)
				return 12;
			
			return 1;
		}
		
		/// <summary>
		/// Вычислить подходит ли год под период.
		/// </summary>
		/// <param name="period">Период.</param>
		/// <param name="beginningYear">Год от которого отсчитывается начало отправления поручений.</param>
		/// <param name="endYear">Год для вычисляемой даты.</param>
		/// <returns>True если вычисляемый год попадает в период.</returns>
		private bool IsCorrectYear(int period, int beginningYear, int endYear)
		{
			while (beginningYear <= endYear)
			{
				if (endYear == beginningYear)
					return true;
				
				beginningYear += period;
			}
			
			return false;
		}
		
		#endregion
		
		#region Ежемесячно.
		
		/// <summary>
		/// Вычислить подходит ли месяц под период.
		/// </summary>
		/// <param name="period">Период.</param>
		/// <param name="beginningYear">Дата от которой отсчитывается начало отправления поручений.</param>
		/// <param name="endYear">Вычисляемая дата.</param>
		/// <returns>True если вычисляемая дата попадает в период.</returns>
		private bool IsCorrectMonth(int period, DateTime beginningMonth, DateTime endMonth)
		{
			while (beginningMonth <= endMonth)
			{
				if (endMonth.Month == beginningMonth.Month && endMonth.Year == beginningMonth.Year)
					return true;
				
				beginningMonth = beginningMonth.AddMonths(period);
			}
			
			return false;
		}
		
		#endregion
		
		/// <summary>
		/// Отправить поручение.
		/// </summary>
		/// <param name="setting">Настройки.</param>
		/// <param name="deadline">Срок.</param>
		/// <returns></returns>
		private void SendActionItem(IRepeatSetting setting, DateTime deadline)
		{
			var task = Sungero.RecordManagement.ActionItemExecutionTasks.Create();
			task.AssignedBy = setting.AssignedBy;
			task.ActionItem = setting.ActionItem;
			task.Supervisor = setting.Supervisor;
			task.IsUnderControl = setting.IsUnderControl.GetValueOrDefault();
			
			if (setting.IsCompoundActionItem.GetValueOrDefault())
			{
				task.IsCompoundActionItem = true;
				
				foreach (var actionItemPartsSetting in setting.ActionItemsParts)
				{
					var actionItemParts = task.ActionItemParts.AddNew();
					actionItemParts.Assignee = actionItemPartsSetting.Assignee;
					actionItemParts.ActionItemPart = actionItemPartsSetting.ActionItemPart;
					actionItemParts.Deadline = deadline.Date;
				}
			}
			else
			{
				foreach (var coAssigneeSetting in setting.CoAssignees)
				{
					var coAssignee = task.CoAssignees.AddNew();
					coAssignee.Assignee = coAssigneeSetting.Assignee;
				}

				task.Assignee = setting.Assignee;
				task.Deadline = deadline.Date;		
			}
			
			var row = setting.StartedActionItemTask.AddNew();
			row.ActionItemTask = task;
			
			task.Start();
		}

		/// <summary>
		/// Отправить уведомление об актуализации расписания.
		/// </summary>
		/// <param name="setting">Настройка повторения поручений.</param>
		private void SendNotice(IRepeatSetting setting)
		{
			var notice = Sungero.Workflow.SimpleTasks.Create();
			notice.Subject = Resources.LastActionItemText;
			notice.NeedsReview = false;

			var routeStep = notice.RouteSteps.AddNew();
			routeStep.AssignmentType = Sungero.Workflow.SimpleTaskRouteSteps.AssignmentType.Notice;
			routeStep.Performer = setting.AssignedBy;
			routeStep.Deadline = null;
			
			notice.Attachments.Add(setting);
			
			if (notice.Subject.Length > Sungero.Workflow.Tasks.Info.Properties.Subject.Length)
				notice.Subject = notice.Subject.Substring(0, Sungero.Workflow.Tasks.Info.Properties.Subject.Length);
			
			notice.Start();
		}
	}
}