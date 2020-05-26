using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.IncomingLetter;

namespace DirRX.Solution
{
  partial class IncomingLetterSharedHandlers
  {

    public virtual void AddresseeDirRXChanged(DirRX.Solution.Shared.IncomingLetterAddresseeDirRXChangedEventArgs e)
    {
      if (e.NewValue != null && e.NewValue != e.OldValue)
      {
        _obj.BusinessUnit = DirRX.Solution.BusinessUnits.As(_obj.AddresseeDirRX.BusinessUnit);
        _obj.Department = DirRX.Solution.Departments.As(_obj.AddresseeDirRX.Department);
      }
    }

    public override void RegistrationDateChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      base.RegistrationDateChanged(e);
      Functions.IncomingLetter.FillName(_obj);
    }

    public override void RegistrationNumberChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      base.RegistrationNumberChanged(e);
      Functions.IncomingLetter.FillName(_obj);
    }

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      base.SubjectChanged(e);
      Functions.IncomingLetter.FillName(_obj);
    }

    public override void SignedByChanged(Sungero.RecordManagement.Shared.IncomingLetterSignedByChangedEventArgs e)
    {
      base.SignedByChanged(e);
      Functions.IncomingLetter.FillName(_obj);
      
      if (e.OldValue != e.NewValue && e.NewValue != null)
      {
        _obj.CorrespondentDepDirRX.Clear();
        if (DirRX.Solution.Contacts.As(e.NewValue).Subdivision != null)
        	_obj.CorrespondentDepDirRX.AddNew().Department = DirRX.Solution.Contacts.As(e.NewValue).Subdivision;
      }
      if (e.OldValue != e.NewValue && e.NewValue == null)
        _obj.CorrespondentDepDirRX.Clear();
    }

    public override void DocumentKindChanged(Sungero.Docflow.Shared.OfficialDocumentDocumentKindChangedEventArgs e)
    {
      base.DocumentKindChanged(e);
      Functions.IncomingLetter.FillName(_obj);
    }

    public override void CorrespondentChanged(Sungero.Docflow.Shared.IncomingDocumentBaseCorrespondentChangedEventArgs e)
    {
      base.CorrespondentChanged(e);
      
      Functions.IncomingLetter.FillName(_obj);
    }

  }
}