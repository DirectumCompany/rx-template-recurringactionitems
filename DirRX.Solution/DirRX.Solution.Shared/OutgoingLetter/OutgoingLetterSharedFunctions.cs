using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.OutgoingLetter;

namespace DirRX.Solution.Shared
{
  partial class OutgoingLetterFunctions
  {

    /// <summary>
    /// Показать/скрыть поле корреспондент с подписью "По списку рассылки".
    /// </summary>
    public void ShowCorresnpondentField()
    {
      _obj.State.Properties.CorrespondentManyAddressDirRX.IsVisible = _obj.IsManyAddressees.Value;
      _obj.State.Properties.CorrespondentBusinnesUnitDirRX.IsEnabled = !_obj.IsManyAddressees.Value;
      if (_obj.IsManyAddressees.GetValueOrDefault())
        _obj.CorrespondentBusinnesUnitDirRX = null;
    }
    
    /// <summary>
    /// Очистить лист рассылки и заполнить первого адресата из карточки.
    /// </summary>
    public void ClearAndFillFirstAddressee()
    {
      _obj.Addressees.Clear();
      if (_obj.Correspondent != null)
      {
        var newAddressee = _obj.Addressees.AddNew() as  DirRX.Solution.IOutgoingLetterAddressees;
        newAddressee.Correspondent = _obj.Correspondent;
        newAddressee.DepartmentDirRX = _obj.CorrespondentBusinnesUnitDirRX;
        newAddressee.Addressee = _obj.Addressee;
        newAddressee.DeliveryMethod = _obj.DeliveryMethod;
        newAddressee.Number = 1;
      }
    }

    /// <summary>
    /// Автозаполнение имени документа.
    /// </summary>
    public override void FillName()
    {
      if (_obj.DocumentKind != null && !_obj.DocumentKind.GenerateDocumentName.Value && _obj.Name == Sungero.Docflow.Resources.DocumentNameAutotext)
        _obj.Name = string.Empty;
      
      if (_obj.DocumentKind == null || !_obj.DocumentKind.GenerateDocumentName.Value)
        return;
      
      var name = string.Empty;
      
      /*Имя в форматах:
          <Вид документа> в <корреспондент> №<номер> от <дата> "<содержание>".        | Для организации
          <Вид документа> для <корреспондент> №<номер> от <дата> "<содержание>".      | Для персоны
          <Вид документа> по списку рассылки №<номер> от <дата> "<содержание>".       | Для нескольких адресатов
       */
      using (TenantInfo.Culture.SwitchTo())
      {
        if (_obj.IsManyAddressees == true && _obj.Addressees.Any())
          name += OutgoingLetters.Resources.CorrespondentToManyAddressees;
        else if (_obj.Correspondent != null && !Equals(_obj.Correspondent, Sungero.Parties.PublicFunctions.Counterparty.Remote.GetDistributionListCounterparty()))
          name += string.Format("{0}{1}", Sungero.Parties.People.Is(_obj.Correspondent) ? OutgoingLetters.Resources.CorrespondentToPerson : OutgoingLetters.Resources.CorrespondentToCompany,
                                _obj.Correspondent.DisplayValue);
        if (_obj.Addressee != null)
          name += string.Format(" {0}", CaseConverter.ConvertPersonFullNameToTargetDeclension(CaseConverter.SplitPersonFullName(_obj.Addressee.Name), DeclensionCase.Dative));
        
        if (!string.IsNullOrWhiteSpace(_obj.Subject))
          name += string.Format(" {0}", _obj.Subject);
        
        if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
          name += string.Format("{0}{1}", Sungero.Docflow.OfficialDocuments.Resources.Number, _obj.RegistrationNumber);
        
        if (_obj.RegistrationDate != null)
          name += string.Format("{0}{1}", Sungero.Docflow.OfficialDocuments.Resources.DateFrom, _obj.RegistrationDate.Value.ToString("d"));
        
        
      }
      
      if (string.IsNullOrWhiteSpace(name))
        name = Sungero.Docflow.Resources.DocumentNameAutotext;
      else if (_obj.DocumentKind != null)
        name = string.Format("{0}{1}", _obj.DocumentKind.ShortName, name);
      
      name = Sungero.Docflow.PublicFunctions.Module.TrimSpecialSymbols(name);
      _obj.Name = name.Length > 250 ? name.Substring(0, 250) : name;
    }

  }
}