using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.Employee;

namespace DirRX.Solution.Server
{
  partial class EmployeeFunctions
  {
    /// <summary>
    /// Создать системное замещение.
    /// </summary>
    /// <param name="substitutedUser">Замещаемый пользователь.</param>
    /// <param name="substitute">Замещающий пользователь.</param>
    [Remote, Public]
    public static void CreateSystemSubstitution(IUser substitutedUser, IUser substitute)
    {
      if (Equals(substitutedUser, substitute))
        return;
      
      var substitution = Substitutions.Create();
      substitution.User = substitutedUser;
      substitution.Substitute = substitute;
      substitution.IsSystem = true;
      substitution.Save();
    }
    
    /// <summary>
    /// Удалить системное замещение.
    /// </summary>
    /// <param name="substitutedUser">Пользователь, для которого надо удалить замещение.</param>
    /// <param name="substitute">Руководитель.</param>
    public static void DeleteSystemSubstitution(IUser substitutedUser, IUser substitute)
    {
      var deletedSubstitution = Substitutions.GetAll()
        .Where(s => Equals(s.Substitute, substitute) && Equals(s.User, substitutedUser) && s.IsSystem == true)
        .FirstOrDefault();
      
      if (deletedSubstitution != null)
        Substitutions.Delete(deletedSubstitution);
    }
  }
}