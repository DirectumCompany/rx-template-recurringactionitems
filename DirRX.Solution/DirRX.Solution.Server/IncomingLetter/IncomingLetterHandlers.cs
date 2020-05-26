using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.IncomingLetter;

namespace DirRX.Solution
{
  partial class IncomingLetterServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      if (!_obj.State.IsCopied)
      {
        _obj.Department = null;
        _obj.BusinessUnit = null;
      }
    }
  }

  partial class IncomingLetterCorrespondentDepDirRXDepartmentPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> CorrespondentDepDirRXDepartmentFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return _root.Correspondent != null ? query.Where(d => Sungero.Parties.Companies.Equals(d.Counterparty, _root.Correspondent)) : query;
    }
  }

  partial class IncomingLetterFilteringServerHandler<T>
  {

    public override IQueryable<Sungero.Docflow.IDocumentKind> DocumentKindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query, Sungero.Domain.FilteringEventArgs e)
    {
      query = base.DocumentKindFiltering(query, e);
      query = query.Where(x => x.DocumentType.DocumentTypeGuid == Sungero.RecordManagement.Server.IncomingLetter.ClassTypeGuid.ToString());
      return query;
    }

    public override IQueryable<Sungero.Company.IDepartment> DepartmentFiltering(IQueryable<Sungero.Company.IDepartment> query, Sungero.Domain.FilteringEventArgs e)
    {
      query = base.DepartmentFiltering(query, e);
      if (_filter.BusinessUnit != null)
        query = query.Where(d => BusinessUnits.Equals(d.BusinessUnit, _filter.BusinessUnit));
      
      return query;
    }

    public virtual IQueryable<DirRX.IntegrationLLK.IDepartCompanies> CorrespondentDepartmentFiltering(IQueryable<DirRX.IntegrationLLK.IDepartCompanies> query, Sungero.Domain.FilteringEventArgs e)
    {
      return _filter.Correspondent != null ? query.Where(d => Sungero.Parties.Companies.Equals(_filter.Correspondent, d.Counterparty)) : query;
    }

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      query = base.Filtering(query, e);
      
      if (_filter == null)
        return query;
      
      if (_filter.Correspondent != null)
        query = query.Where(d => Companies.Equals(d.Correspondent, _filter.Correspondent));
      
      if (_filter.CorrespondentDepartment != null)
        query = query.Where(d => d.CorrespondentDepDirRX.Select(x => x.Department).Contains(_filter.CorrespondentDepartment));
      
      if (_filter.BusinessUnit != null)
        query = query.Where(d => BusinessUnits.Equals(d.BusinessUnit, _filter.BusinessUnit));
      
      return query;
    }
  }

}