using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProcessSubstitutionModule.ProcessSubstitution;

namespace DirRX.ProcessSubstitutionModule
{
  partial class ProcessSubstitutionSubstitutionCollectionSubstitutePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> SubstitutionCollectionSubstituteFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (Roles.Administrators.RecipientLinks.Where(l => Users.Equals(l.Member, Users.Current)).Any())
        return query;
      
      var coworkers = DirRX.Solution.Employees.GetAll()
        .Where(c => Sungero.Company.Departments.Equals(DirRX.Solution.Employees.Current.Department, c.Department)).ToList();
      return query.Where(u => coworkers.Contains(DirRX.Solution.Employees.As(u)));
    }
  }

  partial class ProcessSubstitutionEmployeePropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> EmployeeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (!Users.Current.IncludedIn(Roles.Administrators))
      {
        var substitutableUsers = Substitutions.GetAll(s => Users.Equals(s.Substitute, Users.Current) &&
                                                      (!s.StartDate.HasValue || (s.StartDate.HasValue && s.StartDate.Value <= Calendar.Today)) &&
                                                      (!s.EndDate.HasValue || (s.EndDate.HasValue && s.EndDate.Value >= Calendar.Today)));
        
        query = query.Where(f => (Users.Equals(Users.Current, f) ||
                                  substitutableUsers.Any(r => Users.Equals(r.User, f))));
      }
      return query;
    }
  }

  partial class ProcessSubstitutionServerHandlers
  {

    public override void Deleting(Sungero.Domain.DeletingEventArgs e)
    {
      var connection = SubstituteConnections.GetAll(w => w.ProcessSubstitutionID == _obj.Id).FirstOrDefault();
      if (connection != null)
      {
        connection.NeedUpdateSubtitution = true;
        connection.Save();
      }
    }

    public override void Saving(Sungero.Domain.SavingEventArgs e)
    {
      if (_obj.State.Properties.Employee.IsChanged ||
          _obj.State.Properties.BeginDate.IsChanged ||
          _obj.State.Properties.EndDate.IsChanged ||
          _obj.State.Properties.Note.IsChanged ||
          _obj.State.Properties.SubstitutionCollection.IsChanged)
      {
        var connection = SubstituteConnections.GetAll(w => w.ProcessSubstitutionID == _obj.Id).FirstOrDefault();
        if (connection == null)
          connection = SubstituteConnections.Create();
        
        connection.NeedUpdateSubtitution = true;
        connection.ProcessSubstitutionID = _obj.Id;
        connection.Save();
      }
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      if (_obj.State.Properties.Employee.IsChanged ||
          _obj.State.Properties.BeginDate.IsChanged ||
          _obj.State.Properties.EndDate.IsChanged ||
          _obj.State.Properties.SubstitutionCollection.IsChanged||
          !_obj.Created.HasValue)
        _obj.Created = Calendar.Now;
      
      // Сгенерировать наименование.
      _obj.Name = string.Format("{0} {1} {2} {3}",
                                ProcessSubstitutions.Resources.DefaultSubstitutionName,
                                _obj.Employee,
                                _obj.BeginDate == null ? "" : string.Format("{0} {1}", ProcessSubstitutions.Resources.BeginDateAdditionalText, _obj.BeginDate.ToString()),
                                _obj.EndDate == null ? "" : string.Format("{0} {1}", ProcessSubstitutions.Resources.EndDateAdditionalText, _obj.EndDate.ToString())).Trim();
      // Проверить даты.
      if (_obj.BeginDate.HasValue && _obj.EndDate.HasValue && _obj.BeginDate.Value > _obj.EndDate.Value)
        e.AddError(string.Format(DirRX.ProcessSubstitutionModule.ProcessSubstitutions.Resources.DateError));
      
      // Проверить наличие замещений.
      var substitutionList = Functions.ProcessSubstitution.GetAllSubstitutions(_obj);
      if (!string.IsNullOrEmpty(substitutionList))
        e.AddError(string.Format(DirRX.ProcessSubstitutionModule.ProcessSubstitutions.Resources.SubstitutionErrorTemplate, _obj.Employee.ToString(), substitutionList));
      
      // Проверить настойку замещений.
      if (!_obj.SubstitutionCollection.Any())
      	e.AddError(DirRX.ProcessSubstitutionModule.ProcessSubstitutions.Resources.EmptySubstitutionError);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      _obj.Name = ProcessSubstitutions.Resources.DefaultSubstitutionName;
      _obj.Employee = DirRX.Solution.Employees.Current;
    }
  }

}