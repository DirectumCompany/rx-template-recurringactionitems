using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.OutgoingLetter;

namespace DirRX.Solution
{
  partial class OutgoingLetterAddresseesSharedHandlers
  {

    public virtual void AddresseesDepartmentDirRXChanged(DirRX.Solution.Shared.OutgoingLetterAddresseesDepartmentDirRXChangedEventArgs e)
    {
      if (e.OldValue != e.NewValue)
        _obj.Addressee = null;
    }
  }

  partial class OutgoingLetterSharedHandlers
  {

    public override void IsManyAddresseesChanged(Sungero.Domain.Shared.BooleanPropertyChangedEventArgs e)
    {
      if (_obj.IsManyAddressees == true)
      {
       Functions.OutgoingLetter.ClearAndFillFirstAddressee(_obj);
        
       _obj.Correspondent = DirRX.Solution.Companies.As(Sungero.Parties.PublicFunctions.Counterparty.Remote.GetDistributionListCounterparty());
        _obj.DistributionCorrespondent = _obj.Correspondent.Name;
        _obj.DeliveryMethod = null;
        _obj.Addressee = null;
        _obj.CorrespondentBusinnesUnitDirRX = null;
      }
      else if (_obj.IsManyAddressees == false)
      {
        var addressee = _obj.Addressees.OrderBy(a => a.Number).FirstOrDefault(a => a.Correspondent != null);
        var addresseeDirRX = addressee as  DirRX.Solution.IOutgoingLetterAddressees;
        if (addresseeDirRX != null)
        {
          _obj.Correspondent = addresseeDirRX.Correspondent;
          _obj.DeliveryMethod = addresseeDirRX.DeliveryMethod;
          _obj.CorrespondentBusinnesUnitDirRX = addresseeDirRX.DepartmentDirRX;
          _obj.Addressee = addresseeDirRX.Addressee;
        }
        else
        {
          _obj.Correspondent = null;
          _obj.DeliveryMethod = null;
          _obj.Addressee = null;
          _obj.CorrespondentBusinnesUnitDirRX = null;
        }
        
        Functions.OutgoingLetter.ClearAndFillFirstAddressee(_obj);
      }
      
    }

    public virtual void CorrespondentBusinnesUnitDirRXChanged(DirRX.Solution.Shared.OutgoingLetterCorrespondentBusinnesUnitDirRXChangedEventArgs e)
    {
    }

    public override void AddresseeChanged(Sungero.Docflow.Shared.OutgoingDocumentBaseAddresseeChangedEventArgs e)
    {
      base.AddresseeChanged(e);
      Functions.OutgoingLetter.FillName(_obj);
      if (e.NewValue != null && e.NewValue != e.OldValue)
      {
        _obj.CorrespondentBusinnesUnitDirRX = DirRX.Solution.Contacts.As(e.NewValue).Subdivision;
      }
    }
    
    public virtual void PageCountDirRXChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      int countPage;
      if (int.TryParse(e.NewValue, out countPage))
      {
        if (countPage < 0)
          _obj.PageCountDirRX = "0";
      }
    }

    public override void OurSignatoryChanged(Sungero.Docflow.Shared.OfficialDocumentOurSignatoryChangedEventArgs e)
    {
      base.OurSignatoryChanged(e);
      if (e.NewValue == null)
        _obj.ForWhomDirRX = null;
    }

    public override void RegistrationDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.RegistrationDateChanged(e);
      Functions.OutgoingLetter.FillName(_obj);
    }

    public override void RegistrationNumberChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      base.RegistrationNumberChanged(e);
      Functions.OutgoingLetter.FillName(_obj);
    }

    public override void CorrespondentChanged(Sungero.Docflow.Shared.OutgoingDocumentBaseCorrespondentChangedEventArgs e)
    {
      base.CorrespondentChanged(e);
      Functions.OutgoingLetter.FillName(_obj);
    }

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      base.SubjectChanged(e);
      Functions.OutgoingLetter.FillName(_obj);
    }

    public override void DocumentKindChanged(Sungero.Docflow.Shared.OfficialDocumentDocumentKindChangedEventArgs e)
    {
      base.DocumentKindChanged(e);
      Functions.OutgoingLetter.FillName(_obj);
    }

  }
}