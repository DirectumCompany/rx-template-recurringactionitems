using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.ActionItemsRole;

namespace DirRX.ActionItems.Server
{
  partial class ActionItemsRoleFunctions
  {
    #region Вычисление сотрудников по роли.

    /// <summary>
    /// Вычислить сотрудника для роли.
    /// </summary>
    /// <param name="employee">Сотрудник, по которому определяется исполнитель роли.</param>
    /// <returns>Сотрудник, вычисленный по роли.</returns>
    [Public, Remote(IsPure = true)]
    public DirRX.Solution.IEmployee GetRolePerformer(DirRX.Solution.IEmployee employee)
    {
      if (_obj.Type == DirRX.ActionItems.ActionItemsRole.Type.CEO)
        return GetCEO(employee);
      
      if (_obj.Type == DirRX.ActionItems.ActionItemsRole.Type.CEOAssistant)
        return GetCEOAssistant(employee);
      
      if (_obj.Type == DirRX.ActionItems.ActionItemsRole.Type.InitManager)
        return GetInitManager(employee);
      
      if (_obj.Type == DirRX.ActionItems.ActionItemsRole.Type.InitCEOManager)
        return GetInitCEOManager(employee);
      
      return null;
    }
    
    /// <summary>
    /// Получить ГД.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>ГД.</returns>
    [Public, Remote(IsPure = true)]
    public static DirRX.Solution.IEmployee GetCEO(DirRX.Solution.IEmployee employee)
    {
      var businessUnit = Sungero.Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(employee);
      
      return businessUnit != null ? DirRX.Solution.Employees.As(businessUnit.CEO) : null;
    }
    
    /// <summary>
    /// Получить помощника ГД.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Помощник ГД.</returns>
    private DirRX.Solution.IEmployee GetCEOAssistant(DirRX.Solution.IEmployee employee)
    {
      var businessUnit = Sungero.Company.PublicFunctions.BusinessUnit.Remote.GetBusinessUnit(employee);
      if (businessUnit == null)
        return null;
      
      // В системе можно создать только одного помощника для руководителя, поэтому берём через FirstOrDefault.
      var managersAssistant = Sungero.Company.ManagersAssistants.GetAll(a => DirRX.Solution.Employees.Equals(a.Manager, businessUnit.CEO))
        .FirstOrDefault();
      
      return managersAssistant != null ? DirRX.Solution.Employees.As(managersAssistant.Assistant) : null;
    }
    
    /// <summary>
    /// Получить руководителя сотрудника.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>ГД.</returns>
    private DirRX.Solution.IEmployee GetInitManager(DirRX.Solution.IEmployee employee)
    {
      // Приоритет у руководителя в карточке сотрудника, если он не указан, то руководитель подразделения.
      // Если сотрудник сам является руководителем своего подразделения, то руководитель головного подразделения.
      if (employee.Manager != null)
        return employee.Manager;
      else if (employee.Department.Manager != null &&
               !Solution.Employees.Equals(Solution.Employees.As(employee.Department.Manager), employee))
        return Solution.Employees.As(employee.Department.Manager);
      else if (employee.Department.HeadOffice != null)
        return Solution.Employees.As(employee.Department.HeadOffice.Manager);
      else return null;
    }
    
    /// <summary>
    /// Получить руководителя сотрудника, находящегося в прямом подчинении ГД.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Руководитель сотрудника, находящегося в прямом подчинении ГД.</returns>
    [Public, Remote(IsPure = true)]
    public static DirRX.Solution.IEmployee GetInitCEOManager(DirRX.Solution.IEmployee employee)
    {
      var CEO = GetCEO(employee);
      
      // Обработка ситуации при которой сотрудник находится в прямом подчинении ГД.
      if (DirRX.Solution.Employees.Equals(employee.Manager, CEO))
        return CEO;
      
      if (employee.Manager == null && employee.Department != null &&
          DirRX.Solution.Employees.Equals(employee.Department.Manager, CEO))
        return CEO;
      
      if (employee.Manager == null && employee.Department != null &&
          Sungero.Company.Employees.Equals(employee, employee.Department.Manager) &&
          employee.Department.HeadOffice != null &&
          DirRX.Solution.Employees.Equals(employee.Department.HeadOffice.Manager, CEO))
        return CEO;
      
      return GetManager(employee, CEO);
    }
    
    /// <summary>
    /// Поиск руководителя, находящегося в прямом подчинении ГД.
    /// </summary>
    /// <param name="employee">Сотрудник для которого производится поиск.</param>
    /// <param name="CEO">ГД.</param>
    /// <returns>Руководитель.</returns>
    private static DirRX.Solution.IEmployee GetManager(DirRX.Solution.IEmployee employee, DirRX.Solution.IEmployee CEO, int iteration = 0)
    {
      // Ограничиваем количество итераций, т.к. есть вероятность зацикливания.
      // В каждый вызов функции передаём текущий номер итерации и увеличиваем её на 1, если достигли 20-ти, то возвращаем null.
      if (iteration > 20)
        return null;
      else
        iteration++;
      
      // Приоритет у руководителя в карточке сотрудника, если он не указан, то ищем по подразделению.
      // Если сотрудник сам является руководителем своего подразделения, то ищем по головному подразделению.
      if (employee.Manager != null)
      {
        if (DirRX.Solution.Employees.Equals(employee.Manager, CEO))
          return employee;
        else
          return GetManager(employee.Manager, CEO, iteration);
      }
      else if (employee.Department != null && employee.Department.Manager != null &&
               !Sungero.Company.Employees.Equals(employee, employee.Department.Manager))
      {
        if (DirRX.Solution.Employees.Equals(employee.Department.Manager, CEO))
          return employee;
        else
          return GetManager(DirRX.Solution.Employees.As(employee.Department.Manager), CEO, iteration);
      }
      else if (employee.Department != null && employee.Department.HeadOffice != null && employee.Department.HeadOffice.Manager != null)
      {
        if (DirRX.Solution.Employees.Equals(employee.Department.HeadOffice.Manager, CEO))
          return employee;
        else
          return GetManager(DirRX.Solution.Employees.As(employee.Department.HeadOffice.Manager), CEO, iteration);
      }
      else
        return null;
    }
    
    
    #endregion
    
    #region Проверка вхождения сотрудника в роль.
    
    /// <summary>
    /// Проверить вхождение сотрудника в роль.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <param name="withSubstitutions">С учётом замещения.</param>
    /// <returns>True если сотрудник входит в роль.</returns>
    [Public, Remote(IsPure = true)]
    public bool IsPerformerRole(DirRX.Solution.IEmployee employee, bool withSubstitutions)
    {
      if (_obj.Type == DirRX.ActionItems.ActionItemsRole.Type.Secretary)
        return IsSecretary(employee, withSubstitutions);
      
      if (_obj.Type == DirRX.ActionItems.ActionItemsRole.Type.CEO)
        return IsCEO(employee);
      
      if (_obj.Type == DirRX.ActionItems.ActionItemsRole.Type.CEOAssistant)
        return IsCEOAssistant(employee);
      
      if (_obj.Type == DirRX.ActionItems.ActionItemsRole.Type.InitManager)
        return IsInitManager(employee);
      
      if (_obj.Type == DirRX.ActionItems.ActionItemsRole.Type.InitCEOManager)
        return IsInitCEOManager(employee);
      
      
      return false;
    }

    /// <summary>
    /// Проверить входит ли сотрудник в роль "Секретари".
    /// </summary>
    /// <param name="recepient">Сотрудник.</param>
    /// <param name="withSubstitutions">C учетом замещения или нет.</param>
    /// <returns>True если сотрудник входит в роль.</returns>
    private bool IsSecretary(DirRX.Solution.IEmployee employee, bool withSubstitutions)
    {
      var role = Roles.GetAll().FirstOrDefault(r => r.Sid == Constants.Module.RoleSecretary);
      if (role != null && role.RecipientLinks.Any())
      {
        var recepientRole = Users.As(role.RecipientLinks.FirstOrDefault().Member);

        // Замещамые текущим пользователем.
        var substitutions = new List<IUser>();
        if (withSubstitutions)
          substitutions.AddRange(Substitutions.ActiveSubstitutedUsersWithoutSystem);
        
        return employee.IncludedIn(role) || substitutions.Contains(recepientRole);
      }
      
      return false;
    }
    
    /// <summary>
    /// Является ли сотрудник ГД.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>True если это ГД.</returns>
    [Public, Remote(IsPure = true)]
    public static bool IsCEO(DirRX.Solution.IEmployee employee)
    {
      var CEOList = Sungero.Company.BusinessUnits.GetAll().Select(b => b.CEO).ToList();
      
      return CEOList.Contains(employee);
    }
    
    /// <summary>
    /// Является ли сотрудник помощником ГД.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>True если это помощник ГД.</returns>
    private bool IsCEOAssistant(DirRX.Solution.IEmployee employee)
    {
      var CEOList = Sungero.Company.BusinessUnits.GetAll().Select(b => b.CEO).ToList();

      // FIX: Исправление "Could not determine a type for class: Sungero.Company.IEmployee" при выборе категории "Поручения ГД".
      var CEOAssistants = new List<DirRX.Solution.IEmployee>();
      foreach (var assistant in Sungero.Company.ManagersAssistants.GetAll(a => a.Status == Sungero.CoreEntities.DatabookEntry.Status.Active))
        if (CEOList.Contains(assistant.Manager))
          CEOAssistants.Add(DirRX.Solution.Employees.As(assistant.Assistant));
      
      return CEOAssistants.Contains(employee);
    }
    
    /// <summary>
    /// Является ли сотрудник линейным руководителем.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>True если это непосредственный руководитель.</returns>
    private bool IsInitManager(DirRX.Solution.IEmployee employee)
    {
      return DirRX.Solution.Employees.Equals(employee, GetInitManager(employee));
    }
    
    /// <summary>
    /// Является ли сотрудник руководителем, который находится в прямом подчинении ГД.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>True если это руководитель в подчинении ГД.</returns>
    [Public, Remote(IsPure = true)]
    public static bool IsInitCEOManager(DirRX.Solution.IEmployee employee)
    {
      return DirRX.Solution.Employees.Equals(employee, GetInitCEOManager(employee));
    }
    
    #endregion
    
    /// <summary>
    /// Получить список ролей доступных для уведомлений.
    /// </summary>
    /// <returns>Список ролей</returns>
    [Public, Remote(IsPure = true)]
    public static List<DirRX.ActionItems.IActionItemsRole> GetPossibleRolesForNotices()
    {
      return ActionItemsRoles.GetAll(r => r.Type != DirRX.ActionItems.ActionItemsRole.Type.Secretary &&
                                     r.Type != DirRX.ActionItems.ActionItemsRole.Type.CEOAssistant &&
                                     r.Type != DirRX.ActionItems.ActionItemsRole.Type.InitCEOManager)
        .ToList();
    }
  }
}