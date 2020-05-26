using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalTask;

namespace DirRX.Solution.Shared
{
  partial class ApprovalTaskFunctions
  {

    public override void SetVisibleProperties(Sungero.Docflow.Structures.ApprovalTask.RefreshParameters refreshParameters)
    {
      base.SetVisibleProperties(refreshParameters);
      var taskProperties = _obj.State.Properties;
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      // Добавить управление видимостью свойства "Подписание на бумаге".
      taskProperties.NeedPaperSigning.IsVisible = document != null && !Sungero.Docflow.ContractualDocumentBases.Is(document) && refreshParameters.SignatoryIsVisible;  
      // Если вложение договор/ДС, не отображать в карточке задаче поле "Способ доставки".
      taskProperties.DeliveryMethod.IsVisible = !(document != null && (Solution.Contracts.Is(document) || Solution.SupAgreements.Is(document)));
      taskProperties.ExchangeService.IsVisible = !(document != null && (Solution.Contracts.Is(document) || Solution.SupAgreements.Is(document)));
    }
    
    /// <summary>
    /// Определить текущий этап.
    /// </summary>
    /// <param name="task">Задача.</param>
    /// <param name="stageType">Тип этапа.</param>
    /// <returns>Текущий этап.</returns>

    [Public]
    public static Solution.IApprovalStage GetStage(IApprovalTask task, Enumeration stageType)
    {
      var stage = task.ApprovalRule.Stages
        .Where(s => s.Stage.StageType == stageType)
        .FirstOrDefault(s => s.Number == task.StageNumber);
      
      if (stage != null)
        return Solution.ApprovalStages.As(stage.Stage);
      
      return null;
    }

    /// <summary>
    /// Увеличение срока задания на 1 день, если оно стартовано после 12 часов.
    /// </summary>
    /// <param name="deadline">Текущий срок.</param>
    /// <returns>Новый срок.</returns>
    public static DateTime? UpdateRelativeDeadline(IRecipient recipient, DateTime? deadline)
    {
      if (!deadline.HasValue)
        return deadline;
      
      if (Calendar.Now.ToUserTime(recipient).TimeOfDay.Hours >= 12)
        deadline = deadline.Value.AddWorkingDays(recipient, 1);
      
      return deadline;
    }
    
    #region Скопировано из стандартной.

    /// <summary>
    /// Получить сертификаты текущего пользователя.
    /// </summary>
    /// <returns>Сертификаты текущего пользователя.</returns>
    public static List<ICertificate> GetCertificates()
    {
      var now = Calendar.Now;
      return Certificates.GetAllCached(c => Users.Current.Equals(c.Owner) &&
                                       (c.Enabled == true) &&
                                       (!c.NotBefore.HasValue || c.NotBefore <= now) &&
                                       (!c.NotAfter.HasValue || c.NotAfter >= now))
        .ToList();
    }
    
    #endregion
  }
}