using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PeriodicActionItemsTemplate.Structures.Module
{
  partial class LightActionItem
  {
    public int Id { get; set; }
    
    /// <summary>
    /// Статус поручения.
    /// </summary>
    public Sungero.Core.Enumeration? Status { get; set; }
    
    /// <summary>
    /// Дата завершения.
    /// </summary>
    public DateTime? ActualDate { get; set; }
    
    /// <summary>
    /// Срок.
    /// </summary>
    public DateTime? Deadline { get; set; }
    
    /// <summary>
    /// Автор поручения.
    /// </summary>
    public IUser Author { get; set; }
    
    /// <summary>
    /// Исполнитель.
    /// </summary>
    public Sungero.Company.IEmployee Assignee { get; set; }
    
    /// <summary>
    /// Текст поручения.
    /// </summary>
    public string ActionItem { get; set; }
    
    /// <summary>
    /// Состояние.
    /// </summary>
    public Sungero.Core.Enumeration? ExecutionState { get; set; }
    
    /// <summary>
    /// Постановщик.
    /// </summary>
    public DirRX.Solution.IEmployee Initiator { get; set; }
    
    /// <summary>
    /// Приоритет.
    /// </summary>
    public DirRX.ActionItems.IPriority Priority { get; set; }
    
    /// <summary>
    /// Категория.
    /// </summary>
    public DirRX.ActionItems.ICategory Category { get; set; }
    
    /// <summary>
    /// Соисполнители.
    /// </summary>
    public List<string> CoAssigneesShortNames { get; set; }
    
/// <summary>
    /// Дата старта.
    /// </summary>
    public DateTime? StartedDate { get; set; }
    
  }

}