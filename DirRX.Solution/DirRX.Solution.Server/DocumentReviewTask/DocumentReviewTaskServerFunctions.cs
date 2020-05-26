using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.DocumentReviewTask;

namespace DirRX.Solution.Server
{
	partial class DocumentReviewTaskFunctions
	{
		/// <summary>
		/// Добавить сотрудников в подписчики.
		/// </summary>
		/// <param name="employees">Список сотудников.</param>
		public void AddSubscribers(List<DirRX.Solution.IEmployee> employees)
		{
			foreach (var subscriber in employees)
			{
					var newSubscriber = _obj.SubcribersDirRX.AddNew();
					newSubscriber.Subcriber = subscriber;
			}
		}
		
		/// <summary>
		/// Получить текущих подписчиков.
		/// </summary>
		/// <returns>Список текущих подписчиков.</returns>
		[Remote]
		public List<DirRX.Solution.IEmployee> GetCurrentSubscribers()
		{
			return _obj.SubcribersDirRX.Select(x => x.Subcriber).ToList();
		}

		/// <summary>
		/// Выдать права подписчикам на задачу и вложения.
		/// </summary>
		[Public]
		public void SubscribersGrantAccessRights()
		{
			foreach (var employee in _obj.SubcribersDirRX)
			{
				_obj.AccessRights.Grant(employee.Subcriber, new[] {DefaultAccessRightsTypes.Read});
				foreach (var attachment in _obj.Attachments)
					attachment.AccessRights.Grant(employee.Subcriber, new[] {DefaultAccessRightsTypes.Read});
				
			}
		}

		/// <summary>
		/// Отправить уведомление подписчикам при старте задачи.
		/// </summary>
		public void SendNotificationToSubcribersOnStart()
		{
			var notice = Sungero.Workflow.SimpleTasks.CreateWithNotices(DirRX.Solution.DocumentReviewTasks.Resources.NotoficationForSubscribleSubjectFormat(_obj.DisplayValue),
			                                                            _obj.SubcribersDirRX.Select(x => x.Subcriber).ToArray());
			notice.Attachments.Add(_obj);
			
			try
			{
				notice.Start();
			}
			catch (Exception ex)
			{
				Logger.DebugFormat("Error occurred while sending the notification: {0}", ex.Message);
			}
		}
		
		
		/// <summary>
		/// Отправить уведомление подписчикам при нажатии кнопки на ленте.
		/// </summary>
		/// <returns>Резульать отпавки</returns>
		[Public,Remote]
		public bool SendNotificationToSubcribers(List<DirRX.Solution.IEmployee> employees)
		{
			var notice = Sungero.Workflow.SimpleTasks.CreateWithNotices(DirRX.Solution.DocumentReviewTasks.Resources.NotoficationForSubscribleSubjectFormat(_obj.DisplayValue),
			                                                            employees.ToArray());
			notice.Attachments.Add(_obj);
			try
			{
				AddSubscribers(employees);
				SubscribersGrantAccessRights();
				notice.Start();
				return true;
			}
			catch(Exception ex)
			{
				Logger.DebugFormat("Error occurred while sending the notification: {0}", ex.Message);
				return false;
			}
		}
		
		
	}
}