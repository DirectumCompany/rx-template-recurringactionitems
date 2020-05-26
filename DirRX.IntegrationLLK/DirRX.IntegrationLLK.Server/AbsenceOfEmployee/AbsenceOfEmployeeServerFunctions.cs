using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.IntegrationLLK.AbsenceOfEmployee;

namespace DirRX.IntegrationLLK.Server
{
  partial class AbsenceOfEmployeeFunctions
  {

    # region Для использования при интеграции.
    
    /// <summary>
    /// Создать отсутствие сотрудника.
    /// </summary>
    /// <param name="item">Экземпляр отсутствия сотрудника загруженного из файла.</param>
    /// <returns>Отсутствие сотрудника.</returns>
    [Public]
    public static IAbsenceOfEmployee CreateEmployeeAbsence(Structures.Module.IAbsenceOfEmployee item)
    {
      var absence = AbsenceOfEmployees.Create();
      absence.Employee = item.Employee;
      absence.StartDate = item.StartDate;
      absence.EndDate = item.EndDate;
      absence.Reason = item.AbsenceType;
      absence.Comment = item.Comment;
      absence.Status = Status.Active;
      absence.Save();
      
      return absence;
    }
    
    /// <summary>
    /// Получить текущее отсутствие сотрудника.
    /// </summary>
    /// <param name="item">Экземпляр отсутствия сотрудника загруженного из файла.</param>
    /// <returns>Отсутствие сотрудника.</returns>
    [Public]
    public static IAbsenceOfEmployee GetTodayEmployeeAbsence(Structures.Module.IAbsenceOfEmployee item)
    {
      return AbsenceOfEmployees.GetAll(a => a.Status == IntegrationLLK.AbsenceOfEmployee.Status.Active &&
                                       DirRX.Solution.Employees.Equals(a.Employee, item.Employee) && 
                                       a.StartDate <= Calendar.Today && 
                                       a.EndDate >= Calendar.Today).FirstOrDefault();
    }

    #endregion
  }
}