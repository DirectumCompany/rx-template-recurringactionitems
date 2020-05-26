using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.RegulatoryDocument;

namespace DirRX.LocalActs
{
  partial class RegulatoryDocumentServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      _obj.Edition = 1;
      
      if (CallContext.CalledFrom(DirRX.Solution.Orders.Info))
      {
        var orderID = CallContext.GetCallerEntityId(DirRX.Solution.Orders.Info);
        var order = DirRX.Solution.Orders.GetAll(o => o.Id == orderID).FirstOrDefault();
        if (order != null)
        {
          _obj.BPGroup = order.BPGroup;
          order.Relations.Add(LocalActs.PublicConstants.Module.RegulatoryOrderRelationName, _obj);
        }
      }
    }

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      if (_obj.SpreadsOn.Any(x => !(Sungero.Company.Departments.Is(x.Recepient) ||
                                    Sungero.CoreEntities.Roles.Is(x.Recepient) ||
                                    Solution.BusinessUnits.Is(x.Recepient)) ||
                             x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.Administrators ||
                             x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.Auditors ||
                             x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.ConfigurationManagers ||
                             x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.ServiceUsers ||
                             x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.SoloUsers ||
                             x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.DeliveryUsersSid ||
                             x.Recepient.Sid == Sungero.Projects.PublicConstants.Module.RoleGuid.ParentProjectTeam ||
                             x.Recepient.Sid == Sungero.Domain.Shared.SystemRoleSid.AllUsers))
      {
        e.AddError(RegulatoryDocuments.Resources.NeedDepartmentsOrRoles, _obj.Info.Actions.SpreadsOnClean);
      }

      Functions.RegulatoryDocument.CalculateEditionAndIndex(_obj);
    }
  }



  partial class RegulatoryDocumentCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      base.CreatingFrom(e);
      
      e.Without(_info.Properties.Edition);
      e.Without(_info.Properties.IndexNumber);
      e.Without(_info.Properties.Code);
      e.Without(_info.Properties.EndDate);
    }
  }

  partial class RegulatoryDocumentSpreadsOnRecepientPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> SpreadsOnRecepientFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(c => c.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
    }
  }

  partial class RegulatoryDocumentFilteringServerHandler<T>
  {

    public virtual IQueryable<Sungero.CoreEntities.IRecipient> SpreadsOnFiltering(IQueryable<Sungero.CoreEntities.IRecipient> query, Sungero.Domain.FilteringEventArgs e)
    {
      query = query.Where(c => c.Status == Sungero.CoreEntities.DatabookEntry.Status.Active &&
                          (Sungero.Company.Departments.Is(c) || Roles.Is(c)));

      return query;
    }

    public virtual IQueryable<Sungero.Docflow.IDocumentKind> KindFiltering(IQueryable<Sungero.Docflow.IDocumentKind> query, Sungero.Domain.FilteringEventArgs e)
    {
      return query.Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active &&
                         d.DocumentType.DocumentTypeGuid == Constants.Module.RegulatoryDocumentTypeGuid.ToString());
    }

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      if (_filter == null)
        return query;
      
      if (_filter.InWork || _filter.Active || _filter.Outdated)
        query = query.Where(r => _filter.InWork && r.LifeCycleState == Sungero.Docflow.OfficialDocument.LifeCycleState.Draft ||
                            _filter.Active && r.LifeCycleState == Sungero.Docflow.OfficialDocument.LifeCycleState.Active ||
                            _filter.Outdated && r.LifeCycleState == Sungero.Docflow.OfficialDocument.LifeCycleState.Obsolete);
      
      if (_filter.OurFirm != null)
        query = query.Where(r => DirRX.Solution.BusinessUnits.Equals(r.BusinessUnit, _filter.OurFirm));
      
      if (_filter.BPGroup != null)
        query = query.Where(r => BusinessProcessGroups.Equals(r.BPGroup, _filter.BPGroup));
      
      if (_filter.Kind != null)
        query = query.Where(r => Sungero.Docflow.DocumentKinds.Equals(r.DocumentKind, _filter.Kind));
      
      if (_filter.SpreadsOn != null)
      {
        // Получить главное подразделение.
        var highDepatments = Sungero.Company.Departments.GetAll(h => h.HeadOffice == null).ToList();

        // Отфильтровать по цепочке главных подразделений от указанной.
        var filterDepartment = Sungero.Company.Departments.As(_filter.SpreadsOn);
        if (filterDepartment != null)
        {
          var allDepartmentBranch = new List<Sungero.Company.IDepartment>() { filterDepartment };;
          while (filterDepartment.HeadOffice != null)
          {
            filterDepartment = filterDepartment.HeadOffice;
            allDepartmentBranch.Add(filterDepartment);
          }

          query = query.Where(r => r.SpreadsOn.Any(d => allDepartmentBranch.Contains(Sungero.Company.Departments.As(d.Recepient))));
        }
        
        // Отфильтровать по выбранной роли и для всей организации.
        var filterRole = Sungero.CoreEntities.Roles.As(_filter.SpreadsOn);
        if (filterRole != null)
          query = query.Where(r => r.SpreadsOn.Any(d => Sungero.CoreEntities.Roles.Equals(d.Recepient, filterRole) || highDepatments.Contains(Sungero.Company.Departments.As(d.Recepient))));
      }
      
      if (_filter.StartDateRangeFrom.HasValue)
        query = query.Where(r => r.StartDate.HasValue && r.StartDate.Value >= _filter.StartDateRangeFrom.Value);
      
      if (_filter.StartDateRangeTo.HasValue)
        query = query.Where(r => r.StartDate.HasValue && r.StartDate.Value <= _filter.StartDateRangeTo.Value);
      
      return query;
    }
  }

  partial class RegulatoryDocumentPreviousEditionPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> PreviousEditionFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(r => !RegulatoryDocuments.Equals(r, _obj));
    }
  }

}