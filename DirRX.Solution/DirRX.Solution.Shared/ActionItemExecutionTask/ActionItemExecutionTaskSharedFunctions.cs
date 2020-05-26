using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ActionItemExecutionTask;

namespace DirRX.Solution.Shared
{
  partial class ActionItemExecutionTaskFunctions
  {
    
    /// <summary>
    /// Задание доступности и видимости свойств.
    /// </summary>
    public void SetStateProperties()
    {
      _obj.State.Properties.ReportDeadline.IsEnabled = _obj.Category != null && _obj.Category.NeedsReportDeadline.GetValueOrDefault();
      _obj.State.Properties.EscalatedText.IsVisible = _obj.IsEscalated.GetValueOrDefault();
    }
    
    #region Скопировано из стандартной.
    
    /// <summary>
    /// Получить тему поручения.
    /// </summary>
    /// <param name="task">Поручение.</param>
    /// <param name="beginningSubject">Изначальная тема.</param>
    /// <returns>Сформированная тема поручения.</returns>
    public static string GetActionItemExecutionSubject(IActionItemExecutionTask task, CommonLibrary.LocalizedString beginningSubject)
    {
      var autoSubject = Sungero.Docflow.Resources.AutoformatTaskSubject.ToString();
      
      using (TenantInfo.Culture.SwitchTo())
      {
        var subject = beginningSubject.ToString();
        var actionItem = task.ActionItem;
        
        // Добавить резолюцию в тему.
        if (!string.IsNullOrWhiteSpace(actionItem))
        {
          var hasDocument = task.DocumentsGroup.OfficialDocuments.Any();
          var formattedResolution = Sungero.RecordManagement.PublicFunctions.ActionItemExecutionTask.FormatActionItemForSubject(actionItem, hasDocument);

          // Конкретно у уведомления о старте составного поручения - всегда рисуем с кавычками.
          if (!hasDocument && subject == ActionItemExecutionTasks.Resources.WorkFromActionItemIsCreatedCompound.ToString())
            formattedResolution = string.Format("\"{0}\"", formattedResolution);

          subject += string.Format(" {0}", formattedResolution);
        }
        
        // Добавить ">> " для тем подзадач.
        var isNotMainTask = task.ActionItemType != Sungero.RecordManagement.ActionItemExecutionTask.ActionItemType.Main;
        if (isNotMainTask)
          subject = string.Format(">> {0}", subject);
        
        // Добавить имя документа, если поручение с документом.
        var document = task.DocumentsGroup.OfficialDocuments.FirstOrDefault();
        if (document != null)
          subject += ActionItemExecutionTasks.Resources.SubjectWithDocumentFormat(document.Name);
        
        subject = Sungero.Docflow.PublicFunctions.Module.TrimSpecialSymbols(subject);
        
        if (subject != beginningSubject)
          return subject;
      }
      
      return autoSubject;
    }
    
    #endregion
    
    public void GetReportDeadline()
    {
      List<DateTime?> deadlines = new List<DateTime?>();
      if (_obj.IsCompoundActionItem.GetValueOrDefault())
      {
        deadlines.AddRange(_obj.ActionItemParts.Select(x => x.Deadline));
        deadlines.Add(_obj.FinalDeadline);
      }
      else
        deadlines.Add(_obj.Deadline);
      _obj.ReportDeadline = deadlines.Max();
    }
  }
}