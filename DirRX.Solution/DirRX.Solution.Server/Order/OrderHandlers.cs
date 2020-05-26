using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution;
using DirRX.Solution.Order;

namespace DirRX.Solution
{
  partial class OrderRegulatoryDocumentPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> RegulatoryDocumentFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      return query.Where(r => r.LifeCycleState == Sungero.Docflow.OfficialDocument.LifeCycleState.Draft &&
                         LocalActs.BusinessProcessGroups.Equals(r.BPGroup, _obj.BPGroup));
    }
  }

  partial class OrderFilteringServerHandler<T>
  {

    public override IQueryable<T> Filtering(IQueryable<T> query, Sungero.Domain.FilteringEventArgs e)
    {
      #region Из базовой
      if (_filter == null)
        return query;
      
      // При вызове во внутренних документах с панелью фильтрации, не выводим Не нумеруемые документы.
      query = query.Where(d => d.DocumentKind.NumberingType != Sungero.Docflow.DocumentKind.NumberingType.NotNumerable);
      
      // TODO Zamerov: явно убираем приложения.
      var guid = Sungero.Docflow.Server.Addendum.ClassTypeGuid.ToString();
      query = query.Where(d => d.DocumentKind.DocumentType.DocumentTypeGuid != guid);
      
      // Фильтр по нашей организации.
      if (_filter.BusinessUnitDirRX != null)
        query = query.Where(d => Equals(d.BusinessUnit, _filter.BusinessUnitDirRX));
      
      // Фильтр по интервалу времени
      var periodBegin = Calendar.UserToday.AddDays(-30);
      var periodEnd = Calendar.UserToday.EndOfDay();
      
      if (_filter.LastWeekDirRX)
        periodBegin = Calendar.UserToday.AddDays(-7);
      
      if (_filter.LastMonthDirRX)
        periodBegin = Calendar.UserToday.AddDays(-30);
      
      if (_filter.Last90DaysDirRX)
        periodBegin = Calendar.UserToday.AddDays(-90);
      
      if (_filter.ManualPeriodDirRX)
      {
        periodBegin = _filter.DateRangeDirRXFrom ?? Calendar.SqlMinValue;
        periodEnd = _filter.DateRangeDirRXTo ?? Calendar.SqlMaxValue;
      }
      
      query = query.Where(j => (j.RegistrationState == RegistrationState.Registered || j.RegistrationState == RegistrationState.Reserved) && j.RegistrationDate.Between(periodBegin, periodEnd) ||
                          j.RegistrationState == RegistrationState.NotRegistered && j.Created.Between(periodBegin, periodEnd));
      #endregion
      
      //Фильтр по Теме приказа.
      if (_filter.OrderSubject != null)
        query = query.Where(d => LocalActs.OrderSubjects.Equals(d.Theme, _filter.OrderSubject));
      
      //Фильтр по Типовой форме.
      if (_filter.StandardForm != null)
        query = query.Where(d => LocalActs.StandardForms.Equals(d.StandardForm, _filter.StandardForm));
      
      // Фильтр по состоянию.
      if (_filter.Active || _filter.Draft || _filter.Obsolete)
        query = query.Where(l => (_filter.Active && l.LifeCycleState == LifeCycleState.Active) ||
                            (_filter.Draft && l.LifeCycleState == LifeCycleState.Draft) ||
                            (_filter.Obsolete && l.LifeCycleState == LifeCycleState.Obsolete));
      return query;
    }
  }

  partial class OrderSupervisorPropertyFilteringServerHandler<T>
  {

    public virtual IQueryable<T> SupervisorFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.StandardForm != null && _obj.BPGroup != null)
      {
        if (_obj.StandardForm.IsBPOwner == true)
        {
          var roles = _obj.BPGroup.Owners.Select(t => t.Owner).ToList();
          var owners = Functions.Order.GetRoleEmployees(roles);
          query = query.Where(x => owners.Contains(x));
        }
        else
          return query.Where(x => Sungero.Company.Employees.Equals(x, _obj.Supervisor));
      }
      return query;
    }
  }

  partial class OrderStandardFormPropertyFilteringServerHandler<T>
  {
    public virtual IQueryable<T> StandardFormFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.Theme != null)
        query = query.Where(x => LocalActs.OrderSubjects.Equals(x.Subject, _obj.Theme));
      return query;
    }
  }

  partial class OrderServerHandlers
  {

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      base.AfterSave(e);

      bool isRegNumberChanged = false;
      if (e.Params.TryGetValue("ChangeRegistrationNumberFlag", out isRegNumberChanged))
        e.Params.Remove("ChangeRegistrationNumberFlag");

      bool isRegDateChanged = false;
      if (e.Params.TryGetValue("ChangeRegistrationDateFlag", out isRegDateChanged))
        e.Params.Remove("ChangeRegistrationDateFlag");
      
      var lastVersion = _obj.LastVersion;
      if (lastVersion == null)
        return;

      if (_obj.RegistrationState == Sungero.Docflow.OfficialDocument.RegistrationState.Registered && (isRegNumberChanged || isRegDateChanged))
        Functions.Module.SetRegInfoInPublicBody(_obj, _obj.RegistrationNumber, _obj.RegistrationDate.Value);
    }

    public override void BeforeSigning(Sungero.Domain.BeforeSigningEventArgs e)
    {
      if (e.Signature.SignatureType == SignatureType.Approval)
        Functions.Module.SetSignatureInfoInPublicBody(_obj, Users.Current);
      
      base.BeforeSigning(e);
    }
    

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      var regulatoryDocument = _obj.RegulatoryDocument;
      if (regulatoryDocument != null && regulatoryDocument.PreviousEdition != null)
      {
        var previousOrder = regulatoryDocument.PreviousEdition.Relations.GetRelatedFrom(LocalActs.PublicConstants.Module.RegulatoryOrderRelationName)
          .Where(d => DirRX.Solution.Orders.Is(d))
          .FirstOrDefault();
        
        if (previousOrder != null)
          _obj.Relations.AddFrom(LocalActs.PublicConstants.Module.RegulatoryNewEditionRelationName, previousOrder);
      }
      
      if (regulatoryDocument != null)
      {
        // Заполнить код регламентирующего документа.
        if (_obj.RegistrationState == Sungero.Docflow.OfficialDocument.RegistrationState.Registered)
        {
          string newCode = string.Format("{0}.{1}{2}.{3}-{4}",
                                         regulatoryDocument.BPGroup.Code,
                                         regulatoryDocument.DocumentKind.Code,
                                         regulatoryDocument.IndexNumber.HasValue ? regulatoryDocument.IndexNumber.Value.ToString() : string.Empty,
                                         regulatoryDocument.Edition.HasValue ? regulatoryDocument.Edition.Value.ToString() : string.Empty,
                                         _obj.RegistrationDate.HasValue ? _obj.RegistrationDate.Value.ToString("yyyy") : Calendar.UserToday.ToString("yyyy"));
          if (regulatoryDocument.Code != newCode)
          {
            regulatoryDocument.Code = newCode;
            regulatoryDocument.LifeCycleState = LifeCycleState.Active;
            regulatoryDocument.Save();
          }
        }
        
        if (_obj.RegistrationState == Sungero.Docflow.OfficialDocument.RegistrationState.NotRegistered && !string.IsNullOrEmpty(regulatoryDocument.Code))
        {
          regulatoryDocument.Code = string.Empty;
          regulatoryDocument.LifeCycleState = LifeCycleState.Draft;
          regulatoryDocument.Save();
        }
      }
      
      // Если дата отмены заполнена, занести ее в регламентирующий документ.
      var revokeDocuments = _obj.Relations.GetRelatedFrom(LocalActs.PublicConstants.Module.RegulatoryNewEditionRelationName).Where(d => DirRX.Solution.Orders.Is(d)).ToList();
      foreach (var revokeDocument in revokeDocuments)
      {
        var order = DirRX.Solution.Orders.As(revokeDocument);
        if (order.RegulatoryDocument != null && order.RevokeDate.HasValue)
        {
          var regulatoryRevokedDocument = order.RegulatoryDocument;
          regulatoryRevokedDocument.EndDate = order.RevokeDate.Value;
          regulatoryRevokedDocument.Save();
        }
      }
    }
    
    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      _obj.NeedTaxMonitoring = false;
      _obj.AllWordMarksRegistred = true;
    }
  }

  partial class OrderThemePropertyFilteringServerHandler<T>
  {
    public virtual IQueryable<T> ThemeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.BPGroup != null)
      {
        var subjects = Functions.Order.GetOrderSubjects(_obj.BPGroup);
        query = query.Where(x => subjects.Contains(x));
      }
      return query;
    }
  }
}