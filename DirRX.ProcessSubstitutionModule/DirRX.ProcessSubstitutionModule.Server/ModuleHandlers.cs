using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProcessSubstitutionModule.Server
{
  partial class ProcessSubstitutionFolderFolderHandlers
  {

    public virtual IQueryable<DirRX.Solution.IEmployee> ProcessSubstitutionFolderEmployeeFiltering(IQueryable<DirRX.Solution.IEmployee> query)
    {
      var processSubstitutionList = Functions.Module.GetSubstitutionList(Users.Current, null);
      var substitutionList = processSubstitutionList.Select(s => s.Employee).ToList();
      query = query.Where(s => substitutionList.Contains(s));
      
      return query;
    }

    public virtual IQueryable<Sungero.Workflow.IAssignmentBase> ProcessSubstitutionFolderDataQuery(IQueryable<Sungero.Workflow.IAssignmentBase> query)
    {
      
      // HACK предварительно очистить папку.
      query = query.Where(w => false);
      
      var filter = Structures.Module.ProcessSubstitutionFilter.Create(null, true, true, true, true, 180);
      
      if (_filter != null)
      {
        filter.Employee = _filter.Employee;
        filter.ShowAssignments = _filter.Assignments;
        filter.ShowNotices = _filter.Notice;
        if (_filter.Days180Flag)
          filter.DayCount = 180;
        if (_filter.Days90Flag)
          filter.DayCount = 90;
        if (_filter.Days30Flag)
          filter.DayCount = 30;
        
        filter.InWork = _filter.InWork; 
        filter.Completed = _filter.Completed;         
      }
      
      query = ProcessSubstitutionModule.Functions.Module.GetAssignmentsByAllSubstitutions(query, Users.Current, filter).AsQueryable();

      return query;
    }
  }

  partial class ProcessSubstitutionModuleHandlers
  {
  }
}