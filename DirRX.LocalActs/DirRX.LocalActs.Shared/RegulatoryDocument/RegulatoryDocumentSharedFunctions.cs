using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.LocalActs.RegulatoryDocument;

namespace DirRX.LocalActs.Shared
{
  partial class RegulatoryDocumentFunctions
  {

    /// <summary>
    /// Вычислить редакцию и порядковый номер.
    /// </summary>
    public void CalculateEditionAndIndex()
    {
      bool isBPGroupHasValue = _obj.BPGroup != null;
      bool isPreviousEditionHasValue = _obj.PreviousEdition != null;

      if (isPreviousEditionHasValue)
      {
        _obj.Edition = _obj.PreviousEdition.Edition + 1;
        _obj.IndexNumber = _obj.PreviousEdition.IndexNumber;
      }
      else if (_obj.IndexNumber == null && isBPGroupHasValue)
        _obj.IndexNumber = Functions.RegulatoryDocument.Remote.GetLastBPGroupIndexNumber(_obj) + 1;
    }

    /// <summary>
    /// Изменить отображение панели регистрации.
    /// </summary>
    /// <param name="needShow">Признак отображения.</param>
    /// <param name="repeatRegister">Признак повторной регистрации\изменения реквизитов.</param>
    public override void ChangeRegistrationPaneVisibility(bool needShow, bool repeatRegister)
    {
      base.ChangeRegistrationPaneVisibility(needShow, repeatRegister);
      
      var notNumerable = _obj.DocumentKind != null && _obj.DocumentKind.NumberingType == Sungero.Docflow.DocumentKind.NumberingType.NotNumerable;
      var canRegister = _obj.AccessRights.CanRegister();
      var caseIsEnabled = notNumerable || !notNumerable && canRegister;
      // Может быть уже закрыто от редактирования, если документ зарегистрирован и в формате номера журнала присутствует индекс файла.
      caseIsEnabled = caseIsEnabled && _obj.State.Properties.CaseFile.IsEnabled;
      
      _obj.State.Properties.InternalApprovalState.IsVisible = needShow;
      _obj.State.Properties.ExecutionState.IsVisible = false;
      _obj.State.Properties.ControlExecutionState.IsVisible = false;
      _obj.State.Properties.CaseFile.IsEnabled = caseIsEnabled;
      _obj.State.Properties.PlacedToCaseFileDate.IsEnabled = caseIsEnabled;
      _obj.State.Properties.Tracking.IsEnabled = true;
    }
    
    /// <summary>
    /// Показ панели регистрации.
    /// </summary>
    /// <param name="conditions">Условие.</param>
    /// <returns>Результат проверки необходимости отображения.</returns>
    public override bool NeedShowRegistrationPane(bool conditions)
    {
      return base.NeedShowRegistrationPane(true);
    }
    
    /// <summary>
    /// Изменение состояния документа для ненумеруемых документов.
    /// </summary>
    public override void SetLifeCycleState()
    {
      _obj.LifeCycleState = LifeCycleState.Draft;
    }
    
    /// <summary>
    /// Установить доступность свойств.
    /// </summary>
    public void SetPropertiesAvailabilityAndVisibility()
    {
      bool isAdminOrResponsible = (Users.Current.IncludedIn(Roles.Administrators) ||
                                   Users.Current.IncludedIn(DirRX.LocalActs.PublicConstants.Module.RoleGuid.RegulatoryDocumentsUpdaterRoleGuid));
      bool isEnable = _obj.InternalApprovalState != InternalApprovalState.Signed;
      
      _obj.State.Properties.Name.IsEnabled = isEnable;
      _obj.State.Properties.DocumentKind.IsEnabled = isEnable;
      _obj.State.Properties.Subject.IsEnabled = isEnable;
      _obj.State.Properties.BusinessUnit.IsEnabled = isEnable;
      _obj.State.Properties.Department.IsEnabled = isEnable;
      _obj.State.Properties.OurSignatory.IsEnabled = isAdminOrResponsible;
      _obj.State.Properties.Code.IsEnabled = isAdminOrResponsible;
      _obj.State.Properties.Edition.IsEnabled = isAdminOrResponsible && isEnable;
      _obj.State.Properties.IndexNumber.IsVisible = isAdminOrResponsible;
      _obj.State.Properties.IndexNumber.IsEnabled = isEnable;
      _obj.State.Properties.BPGroup.IsEnabled = isEnable;
      _obj.State.Properties.PreviousEdition.IsEnabled = isEnable;
      _obj.State.Properties.SpreadsOn.IsEnabled = isEnable;
    }
    
  }
}