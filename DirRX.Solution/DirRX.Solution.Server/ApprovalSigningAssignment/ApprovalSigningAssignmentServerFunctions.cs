using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalSigningAssignment;

namespace DirRX.Solution.Server
{
	partial class ApprovalSigningAssignmentFunctions
	{
		/// <summary>
		/// Получить список сотрудников имеющих право подписывать документ во вложении.
		/// </summary>
		/// <returns>Список сотрудников.</returns>
		public List<int> GetSignatureEmployees()
		{
			var doc = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
			
			if (doc != null)
				return Sungero.Docflow.PublicFunctions.OfficialDocument.Remote.GetEmployeeSignatories(doc);
			else
				return null;
		}

	}
}