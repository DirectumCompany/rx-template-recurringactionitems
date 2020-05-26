using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Solution.Module.RecordManagement.Server
{
  partial class OnResolutionProcessingFolderHandlers
  {

    public virtual bool IsOnResolutionProcessingVisible()
    {
    	return Sungero.Company.ManagersAssistants.GetAll().Any(x => Sungero.Company.Employees.Equals(Users.Current, x.Assistant) && x.Status == Sungero.Company.ManagersAssistant.Status.Active);
    }

    public virtual IQueryable<Sungero.RecordManagement.IReviewResolutionAssignment> OnResolutionProcessingDataQuery(IQueryable<Sungero.RecordManagement.IReviewResolutionAssignment> query)
    {      
      // Запрос непрочитанных без фильтра.
      if (_filter == null)
        return query;
      
      // Фильтр по статусу.
      if (_filter.InProcess)
        return query.Where(a => a.Status == Sungero.Workflow.AssignmentBase.Status.InProcess);
      
      // Фильтр по периоду.
      DateTime? periodBegin = null;
      if (_filter.Last30Days)
        periodBegin = Sungero.Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-30));
      else if (_filter.Last90Days)
        periodBegin = Sungero.Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-90));
      else if (_filter.Last180Days)
        periodBegin = Sungero.Docflow.PublicFunctions.Module.Remote.GetTenantDateTimeFromUserDay(Calendar.UserToday.AddDays(-180));
      
      if (periodBegin != null)
        query = query.Where(a => a.Created >= periodBegin);

      return query;
    }
  }
}