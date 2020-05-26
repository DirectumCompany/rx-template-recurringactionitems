using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ContractsCustom.Shared
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Разница дат в годах.
    /// </summary>
    /// <param name="dateFrom">Дата с</param>
    /// <param name="dateTill">Дата по (включительно)</param>
    /// <returns>Разница дат в годах.</returns>
    [Public]
    public double GetDateDifferenceInYear(DateTime dateFrom, DateTime dateTill)
    {
      var difference = 0.0;
      dateTill = dateTill.AddDays(1);
      difference = GetDateDifferenceInMonth(dateFrom, dateTill) / 12.0;
      return difference;
    }

    /// <summary>
    /// Разница дат в месяцах.
    /// </summary>
    /// <param name="dateFrom">Дата с</param>
    /// <param name="dateTill">Дата по</param>
    /// <returns>Разница дат в месяцах.</returns>
    [Public]
    public int GetDateDifferenceInMonth(DateTime dateFrom, DateTime dateTill)
    {
      return ((dateTill.Year - dateFrom.Year) * 12) + dateTill.Month - dateFrom.Month;
    }
  }
}