using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProcessSubstitutionModule.ProcessSubstitution;

namespace DirRX.ProcessSubstitutionModule.Server
{
  partial class ProcessSubstitutionFunctions
  {    
    /// <summary>
    /// Получить список ФИО сотрудников, которых замещает текущий сотрудник.
    /// </summary>
    public string GetAllSubstitutions()
    {
      var beginDate = _obj.BeginDate.HasValue ? _obj.BeginDate.Value : DateTime.ParseExact("01.01.1900", "dd.MM.yyyy", new System.Globalization.CultureInfo("ru-RU", true));
      var endDate = _obj.EndDate.HasValue ? _obj.EndDate.Value : DateTime.MaxValue;
      
      var list = ProcessSubstitutions.GetAll(s => s.Id != _obj.Id &&
                                             s.SubstitutionCollection.Any(c => Users.Equals(c.Substitute, _obj.Employee)) &&
                                             (!s.BeginDate.HasValue || s.BeginDate.HasValue && s.BeginDate.Value <= endDate)  &&
                                             (!s.EndDate.HasValue || s.EndDate.HasValue && s.EndDate.Value >= beginDate));
      
      return string.Join(", ", list.Select(n => n.Employee.DisplayValue));
    }    
  }
}