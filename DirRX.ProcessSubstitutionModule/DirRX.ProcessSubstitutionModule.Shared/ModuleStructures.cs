using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProcessSubstitutionModule.Structures.Module
{ 

  /// <summary>
  /// Статистика заданий по замещению.
  /// </summary>
  partial class AssignmentsCount
  {
    /// <summary>
    /// Общее число заданий.
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Общее число непрочитанных заданий.
    /// </summary>
    public int UnreadedCount { get; set; }
  }
  
  /// <summary>
  /// Замещение.
  /// </summary>
  partial class Substitution
  {
    /// <summary>
    /// Замещающий (кто).
    /// </summary>
    public IUser Substitute { get; set; }
    
    /// <summary>
    /// Замещаемый (кого).
    /// </summary>
    public IUser User { get; set; }
  }
  
  /// <summary>
  /// Фильтр заданий.
  /// </summary>
  partial class ProcessSubstitutionFilter
  {    
    /// <summary>
    /// Замещаемый (кого).
    /// </summary>
    public DirRX.Solution.IEmployee Employee { get; set; }
    
    /// <summary>
    /// Показывать задания.
    /// </summary>
    public bool ShowAssignments { get; set; }    
    
    /// <summary>
    /// Показывать уведомления.
    /// </summary>
    public bool ShowNotices { get; set; }    
    
    /// <summary>
    /// Задания в работе.
    /// </summary>
    public bool InWork { get; set; }
    
    /// <summary>
    /// Выполненные задания.
    /// </summary>
    public bool Completed { get; set; }
    
    /// <summary>
    /// Количество дней.
    /// </summary>
    public int DayCount { get; set; }
  }
}