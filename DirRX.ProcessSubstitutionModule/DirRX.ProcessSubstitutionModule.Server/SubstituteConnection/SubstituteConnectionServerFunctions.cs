using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProcessSubstitutionModule.SubstituteConnection;

namespace DirRX.ProcessSubstitutionModule.Server
{
  partial class SubstituteConnectionFunctions
  {

    /// <summary>
    /// Создать системное замещение.
    /// </summary>
    [Remote, Public]
    public void UpdateSystemSubstitution()
    {
      if (_obj.ProcessSubstitutionID.HasValue)
      {
        // Создать новые замещения, обновить старые и удалить ненужные.
        var systemSubstitutionsForCreate = new List<Structures.Module.Substitution>();
        var systemSubstitutionsForUpdate = new List<Structures.Module.Substitution>();
        var systemSubstitutionsForDelete = new List<Structures.Module.Substitution>();
        var processSubstitution = GetProcessSubstitution();
        
        if (processSubstitution != null)
        {
          FillAllSubstetuteLists(processSubstitution, systemSubstitutionsForCreate, systemSubstitutionsForUpdate, systemSubstitutionsForDelete);
          
          CreateSubstitutes(processSubstitution, systemSubstitutionsForCreate);
          UpdateSubstitutes(processSubstitution, systemSubstitutionsForUpdate);
        }
        else
          systemSubstitutionsForDelete = GetExistingSubstetuteList();
        
        DeleteSubstitutes(systemSubstitutionsForDelete);
        
        if (processSubstitution != null)
        {
          _obj.NeedUpdateSubtitution = false;
          _obj.Save();
        }
        else
          SubstituteConnections.Delete(_obj);
      }
    }
    
    /// <summary>
    /// Получить замещение по процессам.
    /// </summary>
    /// <returns>Замещение по процессам.</returns>
    private IProcessSubstitution GetProcessSubstitution()
    {
      return ProcessSubstitutions.GetAll(w => w.Id == _obj.ProcessSubstitutionID).FirstOrDefault();
    }
    
    /// <summary>
    /// Получить список уникальных пар замещающий - пользователь.
    /// </summary>
    /// <param name="processSubstitution">Замещение по процессу.</param>
    /// <returns>Список уникальных пар замещающий - пользователь.</returns>
    private List<Structures.Module.Substitution> GetCurrentSubstetuteList(IProcessSubstitution processSubstitution)
    {
      var list = new List<Structures.Module.Substitution>();
      
      foreach (var substitute in processSubstitution.SubstitutionCollection.Select(r => r.Substitute).Distinct())
      {
        list.Add(Structures.Module.Substitution.Create(substitute, processSubstitution.Employee));
      }
      
      return list;
    }

    /// <summary>
    /// Получить список существующих замещений.
    /// </summary>
    /// <returns>Список уникальных пар замещающий - пользователь из существующих замещений.</returns>
    private List<Structures.Module.Substitution> GetExistingSubstetuteList()
    {
      var list = new List<Structures.Module.Substitution>();
      
      foreach (var row in _obj.SysSubstitutionCollection)
      {
        list.Add(Structures.Module.Substitution.Create(row.SysSubstitution.Substitute, row.SysSubstitution.User));
      }
      
      return list;
    }
    
    /// <summary>
    /// Заполнить поля замещения.
    /// </summary>
    /// <param name="processSubstitution">Замещение по процессу.</param>
    /// <param name="substitution">Замещение.</param>
    /// <param name="row">Пара замещающий-замещаемый.</param>
    private void FillSubstitute(IProcessSubstitution processSubstitution, ISubstitution substitution, Structures.Module.Substitution row)
    {
      substitution.User = row.User;
      substitution.Substitute = row.Substitute;
      substitution.StartDate = processSubstitution.BeginDate;
      substitution.EndDate = processSubstitution.EndDate;
      substitution.Comment = processSubstitution.Note;
      substitution.IsSystem = true;
    }
    
    /// <summary>
    /// Заполнить список отфильтрованными значениями.
    /// </summary>
    /// <param name="substitution">Замещение.</param>
    private void FillList(List<Structures.Module.Substitution> mainList, List<Structures.Module.Substitution> filteredList)
    {
      foreach (var row in filteredList)
      {
        mainList.Add(row);
      }
    }
    
    /// <summary>
    /// Получить запись замещения по пользователям.
    /// </summary>
    /// <param name="substitute">Замещающий.</param>
    /// <param name="user">Пользователь.</param>
    /// <returns>Замещение.</returns>
    private ISubstitution GetSubstituteByUsers(IUser substitute, IUser user)
    {
      return _obj.SysSubstitutionCollection
        .Where(w => Users.Equals(w.SysSubstitution.Substitute, substitute) && Users.Equals(w.SysSubstitution.User, user))
        .Select(s => s.SysSubstitution)
        .FirstOrDefault();
    }
    
    /// <summary>
    /// Сформировать списки для создания, обновления и удаления замещений.
    /// </summary>
    /// <param name="processSubstitution">Замещение по процессу.</param>
    /// <param name="systemSubstitutionsForCreate">Список создваемых замещений.</param>
    /// <param name="systemSubstitutionsForUpdate">Список обновляемых замещений.</param>
    /// <param name="systemSubstitutionsForDelete">Список удаляемых замещений.</param>
    private void FillAllSubstetuteLists(IProcessSubstitution processSubstitution,
                                        List<Structures.Module.Substitution> systemSubstitutionsForCreate,
                                        List<Structures.Module.Substitution> systemSubstitutionsForUpdate,
                                        List<Structures.Module.Substitution> systemSubstitutionsForDelete)
    {
      var currentSubstetuteList = GetCurrentSubstetuteList(processSubstitution);
      var existingSubstetuteList = GetExistingSubstetuteList();
      
      FillList(systemSubstitutionsForCreate, currentSubstetuteList
               .Where(w => !existingSubstetuteList
                      .Any(a => Users.Equals(a.User, w.User) &&
                           Users.Equals(a.Substitute, w.Substitute)))
               .ToList());
      
      FillList(systemSubstitutionsForUpdate, currentSubstetuteList
               .Where(w => existingSubstetuteList
                      .Any(a => Users.Equals(a.User, w.User) &&
                           Users.Equals(a.Substitute, w.Substitute)))
               .ToList());
      
      FillList(systemSubstitutionsForDelete, existingSubstetuteList
               .Where(w => !currentSubstetuteList
                      .Any(a => Users.Equals(a.User, w.User) &&
                           Users.Equals(a.Substitute, w.Substitute)))
               .ToList());
    }
    
    /// <summary>
    /// Создать замещения.
    /// </summary>
    /// <param name="processSubstitution">Замещение по процессу.</param>
    /// <param name="list">Список создаваемых замещений.</param>
    private void CreateSubstitutes(IProcessSubstitution processSubstitution,
                                   List<Structures.Module.Substitution> list)
    {
      foreach (var row in list)
      {
        var substitution = Substitutions.Create();
        FillSubstitute(processSubstitution, substitution, row);
        substitution.Save();
        
        var newRow = _obj.SysSubstitutionCollection.AddNew();
        newRow.SysSubstitution = substitution;
      }
    }
    
    /// <summary>
    /// Обновить замещения.
    /// </summary>
    /// <param name="processSubstitution">Замещение по процессу.</param>
    /// <param name="list">Список обновляемых замещений.</param>
    private void UpdateSubstitutes(IProcessSubstitution processSubstitution,
                                   List<Structures.Module.Substitution> list)
    {
      foreach (var row in list)
      {
        var substitution = GetSubstituteByUsers(row.Substitute, row.User);
        if (substitution != null)
        {
          FillSubstitute(processSubstitution, substitution, row);
          substitution.Save();
        }
      }
    }
    
    /// <summary>
    /// Удалить замещения.
    /// </summary>
    /// <param name="list">Список удаляемых замещений.</param>
    private void DeleteSubstitutes(List<Structures.Module.Substitution> list)
    {
      foreach (var row in list)
      {
        var substitution = GetSubstituteByUsers(row.Substitute, row.User);
        if (substitution != null)
        {
          var currentSysSubstitution = _obj.SysSubstitutionCollection.FirstOrDefault(w => Substitutions.Equals(w.SysSubstitution, substitution));
          if (currentSysSubstitution != null)
            _obj.SysSubstitutionCollection.Remove(currentSysSubstitution);
          Substitutions.Delete(substitution);
        }
      }
    }
  }
}