using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.RecordCustom.Server
{
  public class ModuleFunctions
  {
    
    #region Работа с SQL

    /// <summary>
    /// Выполнить SQL-запрос.
    /// </summary>
    /// <param name="format">Формат запроса.</param>
    /// <param name="args">Аргументы запроса, подставляемые в формат.</param>
    /// <remarks>Функция дублируется из Docflow, т.к. нельзя исп. params в public-функциях.</remarks>
    public static void ExecuteSQLCommandFormat(string format, params object[] args)
    {
      var command = string.Format(format, args);
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommand(command);
    }
    #endregion
  }
}