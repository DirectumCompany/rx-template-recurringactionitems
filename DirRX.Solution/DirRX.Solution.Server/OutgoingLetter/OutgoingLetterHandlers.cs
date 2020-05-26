using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.OutgoingLetter;

namespace DirRX.Solution
{


  partial class OutgoingLetterAddresseesAddresseePropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> AddresseesAddresseeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = base.AddresseesAddresseeFiltering(query, e);
      if (_obj.DepartmentDirRX != null)
      	return query.ToList().Where(x => DirRX.IntegrationLLK.DepartCompanieses.Equals(DirRX.Solution.Contacts.As(x).Subdivision, _obj.DepartmentDirRX) || DirRX.Solution.Contacts.As(x).Subdivision == null).AsQueryable();
      else
      	return query;
    }
  }

  partial class OutgoingLetterAddresseePropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> AddresseeFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      query = base.AddresseeFiltering(query, e);
      if (_obj.CorrespondentBusinnesUnitDirRX != null)
      	return query.ToList().Where(x => DirRX.IntegrationLLK.DepartCompanieses.Equals(DirRX.Solution.Contacts.As(x).Subdivision, _obj.CorrespondentBusinnesUnitDirRX) || DirRX.Solution.Contacts.As(x).Subdivision == null).AsQueryable();
      else
      	return query;
    }
  }


	partial class OutgoingLetterAddresseesDepartmentDirRXPropertyFilteringServerHandler<T>
	{

		public virtual IQueryable<T> AddresseesDepartmentDirRXFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
		{
			return query.ToList().Where(x => Sungero.Parties.CompanyBases.Equals(x.Counterparty, _obj.Correspondent)).AsQueryable();
		}
	}

	partial class OutgoingLetterForWhomDirRXPropertyFilteringServerHandler<T>
	{

		public virtual IQueryable<T> ForWhomDirRXFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
		{
			var signaturies = Functions.OutgoingLetter.GetSignatories(_obj).Select(x => x.EmployeeId).Distinct().ToList();
			return query.Where(x => signaturies.Contains(x.Id));
		}
	}
	partial class OutgoingLetterServerHandlers
	{

		public override void Created(Sungero.Domain.CreatedEventArgs e)
		{
			base.Created(e);
			Functions.OutgoingLetter.FillName(_obj);
		}
	}

	partial class OutgoingLetterCorrespondentBusinnesUnitDirRXPropertyFilteringServerHandler<T>
	{

		public virtual IQueryable<T> CorrespondentBusinnesUnitDirRXFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
		{
			return query.ToList().Where(x => Sungero.Parties.CompanyBases.Equals(x.Counterparty, _obj.Correspondent)).AsQueryable();
		}
	}

}