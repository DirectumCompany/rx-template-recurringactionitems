using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PeriodicActionItemsTemplate.RepeatSetting;

namespace DirRX.PeriodicActionItemsTemplate.Server
{
  partial class RepeatSettingFunctions
  {
    #region Скопировано из стандартной.
    
    /// <summary>
    /// Добавить получателей в группу исполнителей поручения, исключая дублирующиеся записи.
    /// </summary>
    /// <param name="recipient">Реципиент.</param>
    /// <returns>Если возникили ошибки/хинты, возвращает текст ошибки, иначе - пустая строка.</returns>
    [Public, Remote]
    public string SetRecipientsToAssignees(IRecipient recipient)
    {
      var error = string.Empty;
      var performers = new List<IRecipient> { recipient };
      var employees = Sungero.Company.PublicFunctions.Module.Remote.GetEmployeesFromRecipientsRemote(performers);
      if (employees.Count > Sungero.RecordManagement.PublicConstants.ActionItemExecutionTask.MaxCompoundGroup)
        return Sungero.RecordManagement.ActionItemExecutionTasks.Resources.BigGroupWarningFormat(Sungero.RecordManagement.PublicConstants.ActionItemExecutionTask.MaxCompoundGroup);
      
      var currentPerformers = _obj.ActionItemsParts.Select(x => x.Assignee);
      employees = employees.Except(currentPerformers).ToList();
      
      foreach (var employee in employees)
        _obj.ActionItemsParts.AddNew().Assignee = employee;
      
      return error;
    }
    
    #endregion
    
    /// <summary>
    /// Создать запись справочника по настройке периодичности поручения.
    /// </summary>
    /// <returns></returns>
    [Remote, Public]
    public static DirRX.PeriodicActionItemsTemplate.IRepeatSetting CreateRepeatSetting()
    {
      return RepeatSettings.Create();
    }
    
    /// <summary>
    /// Сформировать тему для поручения.
    /// </summary>
    /// <returns>Тема поручения.</returns>
    [Remote]
    public string CreateSubject()
    {
      var subjectTemplate = _obj.IsCompoundActionItem == true ?
        Sungero.RecordManagement.ActionItemExecutionTasks.Resources.ComponentActionItemExecutionSubject :
        Sungero.RecordManagement.ActionItemExecutionTasks.Resources.TaskSubject;
      
      return Functions.RepeatSetting.GetActionItemExecutionSubject(_obj, subjectTemplate);
    }
  }
}