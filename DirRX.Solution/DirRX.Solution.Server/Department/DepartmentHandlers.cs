using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Department;

namespace DirRX.Solution
{
	partial class DepartmentServerHandlers
	{

	  public override void Created(Sungero.Domain.CreatedEventArgs e)
	  {
	    base.Created(e);
	  }

		public override void Saving(Sungero.Domain.SavingEventArgs e)
		{
			Logger.Debug("Событие до сохранения Подразделения. Создание и удаление замещений.");
			base.Saving(e);
			Logger.Debug("Событие до сохранения Подразделения. Создание и удаление замещений успешно завершено.");
		}
	}

}