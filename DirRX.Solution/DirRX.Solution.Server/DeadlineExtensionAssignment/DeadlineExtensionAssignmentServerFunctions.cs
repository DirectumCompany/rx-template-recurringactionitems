using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.DeadlineExtensionAssignment;

namespace DirRX.Solution.Server
{
  partial class DeadlineExtensionAssignmentFunctions
  {
    #region Из базовой.
    /// <summary>
    /// Получить срок продления в строковом формате.
    /// </summary>
    /// <param name="desiredDeadline">Срок.</param>
    /// <returns>Строковое представление.</returns>
    public static string GetDesiredDeadlineLabel(DateTime desiredDeadline)
    {
      using (TenantInfo.Culture.SwitchTo())
      {
        if (desiredDeadline == desiredDeadline.Date)
          return desiredDeadline.ToString("d");
        
        var utcOffset = Calendar.UtcOffset.TotalHours;
        var utcOffsetLabel = utcOffset >= 0 ? "+" + utcOffset.ToString() : utcOffset.ToString();
        return string.Format("{0:g} (UTC{1})", desiredDeadline, utcOffsetLabel);
      }
    }
    #endregion
  }
}