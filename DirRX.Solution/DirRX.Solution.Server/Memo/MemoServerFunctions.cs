using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Memo;
using Sungero.Docflow;

namespace DirRX.Solution.Server
{
  partial class MemoFunctions
  {
    
    /// <summary>
    /// Получить электронную подпись подписанта для простановки отметки.
    /// </summary>
    /// <param name="versionId">Номер версии.</param>
    /// <returns>Электронная подпись.</returns>
    [Public]
    public override Sungero.Domain.Shared.ISignature GetSignatureForMark(int versionId)
    {
      var version = _obj.Versions.FirstOrDefault(x => x.Id == versionId);
      if (version == null)
        return null;
      
      // Только согласующие подписи сотрудника из поля "Подписал".
      var versionSignatures = Signatures.Get(version)
        .Where(s => s.IsExternal != true && (s.SignatureType == SignatureType.Endorsing || s.SignatureType == SignatureType.Approval) && Equals(s.Signatory, _obj.OurSignatory))
        .ToList();
      if (!versionSignatures.Any())
        return null;
      
      // Квалифицирофанная ЭП приоритетнее простой.
      return versionSignatures
        .OrderBy(s => s.SignCertificate == null)
        .ThenBy(s => s.SignatureType)
        .ThenByDescending(s => s.SigningDate)
        .FirstOrDefault();
    }

    /// <summary>
    /// Преобразовать документ в PDF с наложением отметки об ЭП.
    /// </summary>
    /// <returns>Результат преобразования.</returns>
    public Sungero.Docflow.Structures.OfficialDocument.СonversionToPdfResult MemoConvertToPdfWithSignatureMark()
    {
      var versionId = _obj.LastVersion.Id;
      var info = this.ValidateDocumentBeforeConvertion(versionId);
      if (info.HasErrors || info.IsOnConvertion)
        return info;
      
      // В очереди хранить ИД документа и версии, а не ссылки на них, чтобы не возникали проблемы с блокировками.
      var queueItem = Sungero.Docflow.DocumentConvertToPdfQueueItems.Create();
      queueItem.DocumentId = _obj.Id;
      queueItem.VersionId = _obj.LastVersion.Id;
      queueItem.ErrorType = Sungero.Docflow.DocumentConvertToPdfQueueItem.ErrorType.NoError;
      queueItem.Save();
      
      Sungero.Docflow.Jobs.ConvertDocumentToPdf.Enqueue();
      
      info.HasErrors = false;
      
      return info;
    }

    /// <summary>
    /// Проверить документ до преобразования в PDF.
    /// </summary>
    /// <param name="versionId">Id версии документа.</param>
    /// <returns>Результат проверки перед преобразованием документа.</returns>
    public override Sungero.Docflow.Structures.OfficialDocument.СonversionToPdfResult ValidateDocumentBeforeConvertion(int versionId)
    {
      #region Скопировано из стандартной
      var info = Sungero.Docflow.Structures.OfficialDocument.СonversionToPdfResult.Create();
      info.HasErrors = true;
      
      // Документ МКДО.
      if (Sungero.Exchange.ExchangeDocumentInfos.GetAll().Any(x => Equals(x.Document, _obj)))
      {
        info.ErrorTitle = Sungero.Docflow.OfficialDocuments.Resources.IsExchangeServiceDocumentTitle;
        info.ErrorMessage = Sungero.Docflow.OfficialDocuments.Resources.IsExchangeServiceDocument;
        return info;
      }
      
      // Проверить наличие версии.
      var version = _obj.Versions.FirstOrDefault(x => x.Id == versionId);
      if (version == null)
      {
        info.ErrorTitle = Sungero.Docflow.OfficialDocuments.Resources.ConvertionErrorTitleBase;
        info.ErrorMessage = Sungero.Docflow.OfficialDocuments.Resources.NoVersionError;
        return info;
      }
      
      // Требуемая версия подписана утверждающей или согласующей подписью сотрудником, указанным в поле "Подписал".
      if (!Signatures.Get(version)
          .Any(s => s.IsExternal != true && (s.SignatureType == SignatureType.Endorsing || s.SignatureType == SignatureType.Approval) && Equals(s.Signatory, _obj.OurSignatory)))
      {
        info.ErrorTitle = DirRX.Solution.Memos.Resources.LastVersionNotEndorsed;
        info.ErrorMessage = DirRX.Solution.Memos.Resources.LastVersionNotEndorsedError;
        return info;
      }
      
      // Формат не поддерживается.
      var versionExtension = version.BodyAssociatedApplication.Extension.ToLower();
      var versionExtensionIsSupported = Sungero.AsposeExtensions.Converter.CheckIfExtensionIsSupported(versionExtension);
      if (!versionExtensionIsSupported)
      {
        info.ErrorTitle = Sungero.Docflow.OfficialDocuments.Resources.ConvertionErrorTitleBase;
        info.ErrorMessage = Sungero.Docflow.OfficialDocuments.Resources.ExtensionNotSupportedFormat(versionExtension);
        return info;
      }
      
      // Версия документа повреждена.
      var versionQueueItems = Sungero.Docflow.DocumentConvertToPdfQueueItems.GetAll()
        .Where(x => x.DocumentId == _obj.Id && x.VersionId == versionId);
      var documentBodyNotConvertible = versionQueueItems.Any(x => x.ErrorType == Sungero.Docflow.DocumentConvertToPdfQueueItem.ErrorType.Convertion);
      if (documentBodyNotConvertible)
      {
        info.ErrorTitle = Sungero.Docflow.OfficialDocuments.Resources.ConvertionErrorTitleBase;
        info.ErrorMessage = Sungero.Docflow.Resources.DocumentBodyNeedsRepair;
        return info;
      }
      
      // Проблемы с блокировками при преобразовании.
      if (versionQueueItems.Any(x => x.ErrorType == Sungero.Docflow.DocumentConvertToPdfQueueItem.ErrorType.Locks))
      {
        Sungero.Docflow.Jobs.ConvertDocumentToPdf.Enqueue();
        info.HasLockError = true;
        info.IsOnConvertion = true;
        info.HasErrors = false;
        return info;
      }
      
      // Документ уже в очереди на конвертацию.
      if (versionQueueItems.Any(x => x.ErrorType == Sungero.Docflow.DocumentConvertToPdfQueueItem.ErrorType.NoError))
      {
        info.IsOnConvertion = true;
        info.HasErrors = false;
        return info;
      }
      
      // Валидация подписи.
      var signature = DirRX.Solution.Functions.Memo.GetSignatureForMark(_obj, versionId);
      var separator = ". ";
      var validationError = Sungero.Docflow.PublicFunctions.Module.GetSignatureValidationErrorsAsString(signature, separator);
      if (!string.IsNullOrEmpty(validationError))
      {
        info.ErrorTitle = Sungero.Docflow.OfficialDocuments.Resources.SignatureNotValidErrorTitle;
        info.ErrorMessage = string.Format(Sungero.Docflow.OfficialDocuments.Resources.SignatureNotValidError, validationError);
        return info;
      }
      
      info.HasErrors = false;
      return info;
      #endregion
    }

    /// <summary>
    /// Получить адресата из Типовой формы по виду документа.
    /// </summary>
    [Remote]
    public List<Sungero.Company.IEmployee> GetAddressee()
    {
      var employees = new List<Sungero.Company.IEmployee>();
      var stdForm = DirRX.LocalActs.PublicFunctions.StandardForm.Remote.FindStandardForms(_obj.DocumentKind).Where(x => x.Addressee != null);
      var firstForm = stdForm.FirstOrDefault();
      if (firstForm != null && !stdForm.Any(x => !LocalActs.StandardForms.Equals(x, firstForm)))
      {
        var recipients = new List<IRecipient>();
        recipients.Add(stdForm.FirstOrDefault().Addressee);
        employees = Sungero.Docflow.PublicFunctions.Module.Remote.GetEmployeesFromRecipients(recipients);
      }
      return employees;
    }
    
    /// <summary>
    /// Получение Типовой формы по виду документа.
    /// </summary>
    [Remote]
    public List<DirRX.LocalActs.IStandardForm> GetStandartForm()
    {
      return DirRX.LocalActs.StandardForms.GetAll(x => Sungero.Docflow.DocumentKinds.Equals(x.DocumentKind, _obj.DocumentKind) && x.Status == DirRX.LocalActs.StandardForm.Status.Active).ToList();
    }
    
  }
}