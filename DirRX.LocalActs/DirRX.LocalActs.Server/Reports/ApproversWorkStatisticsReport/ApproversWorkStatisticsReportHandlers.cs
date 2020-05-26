using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Workflow;

namespace DirRX.LocalActs
{
  partial class ApproversWorkStatisticsReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      // Удалить временные данные из таблицы.
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.ApproversWorkStatisticsReport.SourceTableName, ApproversWorkStatisticsReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      AccessRights.AllowRead(
        () =>
        {
          ApproversWorkStatisticsReport.ReportSessionId = Guid.NewGuid().ToString();
          
          #region Отбор подразделений, сотрудников, задач и документов
          var beginDate = ApproversWorkStatisticsReport.BeginDate ?? Calendar.SqlMinValue;
          var endDate = ApproversWorkStatisticsReport.EndDate ?? Calendar.SqlMaxValue;
          
          List<Sungero.Company.IDepartment> depatmentsAll = new List<Sungero.Company.IDepartment>();
          List<Sungero.Company.IEmployee> employeesFilter = new List<Sungero.Company.IEmployee>();
          List<Sungero.Company.IDepartment> employeesDepatments = new List<Sungero.Company.IDepartment>();
          List<Sungero.Company.IDepartment> selectedDepatments = new List<Sungero.Company.IDepartment>();
          
          // Получить всю иерархию выбранных подразделений.
          if (ApproversWorkStatisticsReport.Departments.Any())
          {
            List<Sungero.Company.IDepartment> firstLeveldepatments = new List<Sungero.Company.IDepartment>();
            firstLeveldepatments.AddRange(ApproversWorkStatisticsReport.Departments);
            selectedDepatments.AddRange(ApproversWorkStatisticsReport.Departments);
            selectedDepatments = DirRX.ActionItems.PublicFunctions.Module.Remote.GetDepartmentHierarchyDown(firstLeveldepatments, selectedDepatments);
            
            selectedDepatments = selectedDepatments.Where(d => ApproversWorkStatisticsReport.BusinessUnit == null ||
                                                          Sungero.Company.BusinessUnits.Equals(d.BusinessUnit, ApproversWorkStatisticsReport.BusinessUnit)).OrderBy(d => d.BusinessUnit.Name)
              .ThenBy(d => GetHierarchyLevel(d)).ToList();
          }
          
          // Указан руководитель.
          if (ApproversWorkStatisticsReport.Manager != null)
          {
            // Подразделение руководителя и все нижестоящие.
            List<Sungero.Company.IDepartment> firstLeveldepatments = new List<Sungero.Company.IDepartment>();
            firstLeveldepatments.Add(ApproversWorkStatisticsReport.Manager.Department);
            List<Sungero.Company.IDepartment> subDepatments = new List<Sungero.Company.IDepartment>();
            
            subDepatments = DirRX.ActionItems.PublicFunctions.Module.Remote.GetDepartmentHierarchyDown(firstLeveldepatments, subDepatments);
            if (subDepatments.Any())
            {
              employeesDepatments.AddRange(subDepatments);
              employeesFilter.AddRange(Sungero.Company.Employees.GetAll(s => subDepatments.Contains(s.Department)).ToList());
            }
            
            // Подразделения, в которых указан выбранный Руководитель.
            var managerDepartments = Sungero.Company.Departments.GetAll(d => Sungero.Company.Employees.Equals(d.Manager, ApproversWorkStatisticsReport.Manager) && !employeesDepatments.Contains(d)).ToList();
            if (managerDepartments.Any())
            {
              employeesDepatments.AddRange(managerDepartments);
              employeesFilter = GetEmployeesDown(managerDepartments, employeesDepatments, employeesFilter);
            }
            
            // Сотрудники, у которых в карточке в поле "Непосредственный руководитель" указан Руководитель.
            var subEmployees = DirRX.Solution.Employees.GetAll(s => DirRX.Solution.Employees.Equals(s.Manager, DirRX.Solution.Employees.As(ApproversWorkStatisticsReport.Manager)) && !employeesFilter.Contains(s)).ToList();
            if (subEmployees.Any())
            {
              employeesFilter.AddRange(subEmployees);
              employeesFilter = GetSubEmployeesDown(subEmployees, employeesDepatments, employeesFilter);
            }
            
            // Отфильтровать исполнителей по выбранным подразделениям.
            if (selectedDepatments.Any())
            {
              employeesFilter = Sungero.Company.Employees.GetAll(s => employeesFilter.Contains(s) && selectedDepatments.Contains(s.Department)).ToList();
              employeesDepatments = employeesDepatments.Where(d => selectedDepatments.Contains(d)).ToList();
            }
            
            // Добавить руководителя и его подразделение, т.к. они должны быть в выборке.
            employeesFilter.Add(ApproversWorkStatisticsReport.Manager);
            employeesDepatments.Add(ApproversWorkStatisticsReport.Manager.Department);
          }
          else
          {
            if (ApproversWorkStatisticsReport.Approver != null)
            {
              employeesFilter.Add(ApproversWorkStatisticsReport.Approver);
              employeesDepatments.Add(ApproversWorkStatisticsReport.Approver.Department);
            }
            else
            {
              if (ApproversWorkStatisticsReport.Departments.Any())
              {
                employeesDepatments = selectedDepatments;
                employeesFilter = Sungero.Company.Employees.GetAll(s => selectedDepatments.Contains(s.Department)).ToList();
              }
              else
              {
                employeesDepatments = Sungero.Company.Departments.GetAll(d => ApproversWorkStatisticsReport.BusinessUnit == null ||
                                                                         Sungero.Company.BusinessUnits.Equals(d.BusinessUnit, ApproversWorkStatisticsReport.BusinessUnit)).ToList();
                employeesFilter = Sungero.Company.Employees.GetAll(s => employeesDepatments.Contains(s.Department)).ToList();
              }
            }
          }
          
          // Выборка заданий.
          var tasks = DirRX.Solution.ApprovalTasks.GetAll()
            .Where(t => t.Status == Sungero.Workflow.Task.Status.Completed || t.Status == Sungero.Workflow.Task.Status.InProcess || t.Status == Sungero.Workflow.Task.Status.Suspended
                   || t.Status == Sungero.Workflow.Task.Status.UnderReview).ToList();
          
          // Задания на согласование.
          var approvalAssignments = Sungero.Docflow.ApprovalAssignments.GetAll(a => tasks.Contains(DirRX.Solution.ApprovalTasks.As(a.Task)))
            .Where(a => a.Created >= beginDate && a.Created <= endDate.AddDays(1))
            .Where(a => employeesFilter.Contains(Sungero.Company.Employees.As(a.Performer))).Cast<DirRX.Solution.IApprovalAssignment>().ToList();
          
          var approvalAssignmentsDocuments = approvalAssignments
            .Where(t => t.DocumentGroup.OfficialDocuments.FirstOrDefault() != null)
            .Select(t => t.DocumentGroup.OfficialDocuments.FirstOrDefault())
            .Distinct().Cast<IOfficialDocument>().ToList();
          
          approvalAssignmentsDocuments = approvalAssignmentsDocuments
            .Where(d => d != null)
            .Where(d => d.DocumentKind.DocumentFlow != Sungero.Docflow.DocumentKind.DocumentFlow.Contracts)
            .Where(d => ApproversWorkStatisticsReport.DocType == null || Sungero.Docflow.DocumentTypes.Equals(d.DocumentKind.DocumentType, ApproversWorkStatisticsReport.DocType))
            .Where(d => ApproversWorkStatisticsReport.DocType == null || ApproversWorkStatisticsReport.DocType.DocumentTypeGuid != DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.Order ||
                   !ApproversWorkStatisticsReport.Subjects.Any() || ApproversWorkStatisticsReport.Subjects.Contains(DirRX.Solution.Orders.As(d).Theme))
            .Where(d => ApproversWorkStatisticsReport.DocType == null || ApproversWorkStatisticsReport.DocType.DocumentTypeGuid != DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.Order ||
                   !ApproversWorkStatisticsReport.StandardForms.Any() || ApproversWorkStatisticsReport.StandardForms.Contains(DirRX.Solution.Orders.As(d).StandardForm))
            .Where(d => ApproversWorkStatisticsReport.DocType == null || ApproversWorkStatisticsReport.DocType.DocumentTypeGuid == DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.Order ||
                   !ApproversWorkStatisticsReport.DocKinds.Any() || ApproversWorkStatisticsReport.DocKinds.Contains(d.DocumentKind))
            .ToList();
          
          // Отфильтровать задания по документам.
          approvalAssignments = approvalAssignments.Where(a => approvalAssignmentsDocuments.Contains(a.DocumentGroup.OfficialDocuments.FirstOrDefault())).ToList();
          
          // Задания на подтверждение рисков.
          var riskConfirmationAssignments = DirRX.Solution.ApprovalCheckingAssignments.GetAll(a => tasks.Contains(DirRX.Solution.ApprovalTasks.As(a.Task)))
            .Where(a => a.Created >= beginDate && a.Created <= endDate.AddDays(1))
            .Where(a => a.IsRiskConfirmation == true)
            .Where(a => employeesFilter.Contains(Sungero.Company.Employees.As(a.Performer))).ToList();
          
          var riskConfirmationDocuments = riskConfirmationAssignments
            .Where(t => t.DocumentGroup.OfficialDocuments.FirstOrDefault() != null)
            .Select(t => t.DocumentGroup.OfficialDocuments.FirstOrDefault())
            .Distinct().Cast<IOfficialDocument>().ToList();
          
          riskConfirmationDocuments = riskConfirmationDocuments
            .Where(d => d != null)
            .Where(d => ApproversWorkStatisticsReport.DocType == null || Sungero.Docflow.DocumentTypes.Equals(d.DocumentKind.DocumentType, ApproversWorkStatisticsReport.DocType))
            .Where(d => ApproversWorkStatisticsReport.DocType == null || ApproversWorkStatisticsReport.DocType.DocumentTypeGuid != DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.Order ||
                   !ApproversWorkStatisticsReport.Subjects.Any() || ApproversWorkStatisticsReport.Subjects.Contains(DirRX.Solution.Orders.As(d).Theme))
            .Where(d => ApproversWorkStatisticsReport.DocType == null || ApproversWorkStatisticsReport.DocType.DocumentTypeGuid != DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.Order ||
                   !ApproversWorkStatisticsReport.StandardForms.Any() || ApproversWorkStatisticsReport.StandardForms.Contains(DirRX.Solution.Orders.As(d).StandardForm))
            .Where(d => ApproversWorkStatisticsReport.DocType == null || ApproversWorkStatisticsReport.DocType.DocumentTypeGuid == DirRX.Solution.PublicConstants.Module.DocumentTypeGuid.Order ||
                   !ApproversWorkStatisticsReport.DocKinds.Any() || ApproversWorkStatisticsReport.DocKinds.Contains(d.DocumentKind))
            .ToList();
          
          // Отфильтровать задания по документам.
          riskConfirmationAssignments = riskConfirmationAssignments.Where(a => riskConfirmationDocuments.Contains(a.DocumentGroup.OfficialDocuments.FirstOrDefault())).ToList();
          
          #endregion
          
          #region Текст шапки, период, распечатал.
          var headText = string.Empty;
          if (ApproversWorkStatisticsReport.DocType != null)
            headText = Reports.Resources.ApproversWorkStatisticsReport.DocumentFormat(ApproversWorkStatisticsReport.DocType.DisplayValue);
          if (ApproversWorkStatisticsReport.Subjects.Any())
          {
            var subjectString = string.Empty;
            foreach (var subject in ApproversWorkStatisticsReport.Subjects)
              subjectString = string.IsNullOrEmpty(subjectString) ? subject.DisplayValue : string.Join(", ", subjectString, subject.DisplayValue);
            subjectString = Reports.Resources.ApproversWorkStatisticsReport.SubjectTemplateFormat(subjectString);
            headText = string.Join(Environment.NewLine, headText, subjectString);
          }
          if (ApproversWorkStatisticsReport.StandardForms.Any())
          {
            var formsString = string.Empty;
            foreach (var form in ApproversWorkStatisticsReport.StandardForms)
              formsString = string.IsNullOrEmpty(formsString) ? form.DisplayValue : string.Join(", ", formsString, form.DisplayValue);
            formsString = Reports.Resources.ApproversWorkStatisticsReport.StandardFormTemplateFormat(formsString);
            headText = string.Join(Environment.NewLine, headText, formsString);
          }
          if (ApproversWorkStatisticsReport.DocKinds.Any())
          {
            var kindsString = string.Empty;
            foreach (var kind in ApproversWorkStatisticsReport.DocKinds)
              kindsString = string.IsNullOrEmpty(kindsString) ? kind.DisplayValue : string.Join(", ", kindsString, kind.DisplayValue);
            kindsString = Reports.Resources.ApproversWorkStatisticsReport.DocKindTemplateFormat(kindsString);
            headText = string.IsNullOrEmpty(headText) ? kindsString : string.Join(Environment.NewLine, headText, kindsString);
          }
          ApproversWorkStatisticsReport.HeadText = headText;
          
          ApproversWorkStatisticsReport.Period = Reports.Resources.ApproversWorkStatisticsReport.PeriodFormat(ApproversWorkStatisticsReport.BeginDate.Value.ToShortDateString(), ApproversWorkStatisticsReport.EndDate.Value.ToShortDateString());
          ApproversWorkStatisticsReport.CurrentDate = Calendar.UserNow.ToString("G");
          #endregion
          
          var dataTable = new List<Structures.ApproversWorkStatisticsReport.TableLine>();
          
          if (employeesDepatments.Any())
          {
            List<Sungero.Company.IEmployee> employeesAll = new List<Sungero.Company.IEmployee>();
            
            var businessUnits = employeesDepatments.Select(d => d.BusinessUnit).Distinct().Cast<Sungero.Company.IBusinessUnit>().ToList();
            
            // Цикл по Нашим организациям.
            foreach (var businessUnit in businessUnits)
            {
              var statisticTableLine = Structures.ApproversWorkStatisticsReport.StatisticTableLine.Create(new List<IOfficialDocument>(), 0, 0, 0, 0.0, 0, 0.0);
              
              List<Sungero.Company.IDepartment> businessUnitDepatments = new List<Sungero.Company.IDepartment>();
              
              // Сортировка результатов по НОР, сначала подразделение выбранного руководителя, по уровню иерархии.
              if (ApproversWorkStatisticsReport.Manager != null)
                businessUnitDepatments = employeesDepatments.Where(d => Equals(d.BusinessUnit, businessUnit)).OrderByDescending(d => Equals(d, ApproversWorkStatisticsReport.Manager.Department)).ThenByDescending(d => Equals(d.Manager, ApproversWorkStatisticsReport.Manager))
                  .ThenBy(d => GetHierarchyLevel(d)).ThenBy(d => d.Name).ToList();
              else
                businessUnitDepatments = employeesDepatments.Where(d => Equals(d.BusinessUnit, businessUnit)).OrderBy(d => GetHierarchyLevel(d)).ThenBy(d => d.Name).ToList();
              
              foreach (var department in businessUnitDepatments)
              {
                if (!depatmentsAll.Contains(department))
                {
                  depatmentsAll.Add(department);
                  
                  // Сотрудники.
                  var employeesStatistic = GetDepartmentEmployees(department, dataTable, employeesFilter, approvalAssignments, riskConfirmationAssignments);
                  
                  // Обойти подчиненные подразделения.
                  var departmentStatistic = GetSubDepartments(department, dataTable, depatmentsAll, employeesAll, employeesFilter, approvalAssignments, riskConfirmationAssignments);
                  
                  List<IOfficialDocument> currentDocuments = new List<IOfficialDocument>();
                  if (employeesStatistic.Documents.Any())
                    currentDocuments.AddRange(employeesStatistic.Documents);
                  if (departmentStatistic.Documents.Any())
                    currentDocuments.AddRange(departmentStatistic.Documents);
                  if (currentDocuments.Any())
                    currentDocuments = currentDocuments.Distinct().ToList();
                  
                  var currentDocCount = currentDocuments.Count;
                  
                  if (currentDocCount > 0)
                  {
                    var tableLine = Structures.ApproversWorkStatisticsReport.TableLine.Create();
                    tableLine.Id = department.Id;
                    tableLine.Parent = businessUnit.Id;
                    tableLine.Manager = department.Name;
                    tableLine.ReportSessionId = ApproversWorkStatisticsReport.ReportSessionId;
                    tableLine.DocumentCount = currentDocCount;
                    tableLine.TotalAssignCount = employeesStatistic.TotalAssignCount + departmentStatistic.TotalAssignCount;
                    tableLine.DoneAssignCount = employeesStatistic.DoneAssignCount + departmentStatistic.DoneAssignCount;
                    tableLine.InWorkAssignCount = employeesStatistic.InWorkAssignCount + departmentStatistic.InWorkAssignCount;
                    tableLine.IsDepartment = true;
                    
                    if ((employeesStatistic.OverdueCount + departmentStatistic.OverdueCount) > 0)
                    {
                      tableLine.OverdueCount = employeesStatistic.OverdueCount + departmentStatistic.OverdueCount;
                      tableLine.OverdueAverageValue = (employeesStatistic.OverdueSum + departmentStatistic.OverdueSum) / 8.0 / (employeesStatistic.OverdueCount + departmentStatistic.OverdueCount);
                    }
                    
                    if ((employeesStatistic.DoneAssignCount + departmentStatistic.DoneAssignCount) > 0)
                      tableLine.AverageApproveTime = (employeesStatistic.ApproveTimeSum + departmentStatistic.ApproveTimeSum) / 8.0 / (employeesStatistic.DoneAssignCount + departmentStatistic.DoneAssignCount);
                    
                    statisticTableLine.Documents.AddRange(currentDocuments);
                    statisticTableLine.TotalAssignCount += tableLine.TotalAssignCount;
                    statisticTableLine.DoneAssignCount += tableLine.DoneAssignCount;
                    statisticTableLine.InWorkAssignCount += tableLine.InWorkAssignCount;
                    statisticTableLine.ApproveTimeSum += (employeesStatistic.ApproveTimeSum + departmentStatistic.ApproveTimeSum);
                    statisticTableLine.OverdueCount += (employeesStatistic.OverdueCount + departmentStatistic.OverdueCount);
                    statisticTableLine.OverdueSum += (employeesStatistic.OverdueSum + departmentStatistic.OverdueSum);
                    
                    dataTable.Add(tableLine);
                  }
                }
              }
              
              if (statisticTableLine.Documents.Any())
              {
                var tableLine = Structures.ApproversWorkStatisticsReport.TableLine.Create();
                tableLine.Id = businessUnit.Id;
                tableLine.Manager = businessUnit.Name;
                tableLine.ReportSessionId = ApproversWorkStatisticsReport.ReportSessionId;
                tableLine.DocumentCount = statisticTableLine.Documents.Distinct().Count();
                tableLine.TotalAssignCount = statisticTableLine.TotalAssignCount;
                tableLine.DoneAssignCount = statisticTableLine.DoneAssignCount;
                tableLine.InWorkAssignCount = statisticTableLine.InWorkAssignCount;
                tableLine.IsDepartment = false;
                
                if (statisticTableLine.OverdueCount > 0)
                {
                  tableLine.OverdueCount = statisticTableLine.OverdueCount;
                  tableLine.OverdueAverageValue = statisticTableLine.OverdueSum / 8.0 / statisticTableLine.OverdueCount;
                }
                
                if (statisticTableLine.DoneAssignCount > 0)
                {
                  tableLine.AverageApproveTime = statisticTableLine.ApproveTimeSum / 8.0 / statisticTableLine.DoneAssignCount;
                }
                
                dataTable.Add(tableLine);
              }
            }
          }
          
          Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.ApproversWorkStatisticsReport.SourceTableName, dataTable);
        });
    }
    
    /// <summary>
    /// Получить подчиненных сотрудников.
    /// </summary>
    public List<Sungero.Company.IEmployee> GetEmployeesDown(List<Sungero.Company.IDepartment> depatments, List<Sungero.Company.IDepartment> depatmentsAll, List<Sungero.Company.IEmployee> employeesAll)
    {
      foreach (Sungero.Company.IDepartment depatment in depatments)
      {
        // Сотрудники текущего подразделения.
        var employees = Sungero.Company.Employees.GetAll(s => Sungero.Company.Departments.Equals(s.Department, depatment) && !employeesAll.Contains(s)).ToList();
        if (employees.Any())
          employeesAll.AddRange(employees);
        
        var lowerDepartments = Sungero.Company.Departments.GetAll(d => Sungero.Company.Departments.Equals(d.HeadOffice, depatment) && !depatmentsAll.Contains(d)).ToList();
        if (lowerDepartments.Any())
        {
          depatmentsAll.AddRange(lowerDepartments);
          GetEmployeesDown(lowerDepartments, depatmentsAll, employeesAll);
        }
        
        if (employees.Any())
          GetSubEmployeesDown(employees.Cast<DirRX.Solution.IEmployee>().ToList(), depatmentsAll, employeesAll);
      }
      
      return employeesAll;    
    }
    
    /// <summary>
    /// Получить сотрудников, подчиненных непосредственно руководителям, и сотрудников из подчиненных подразделений.
    /// </summary>
    /// <param name="employees">Руководители.</param>
    /// <returns>Сотрудники.</returns>
    public List<Sungero.Company.IEmployee> GetSubEmployeesDown(List<DirRX.Solution.IEmployee> employees, List<Sungero.Company.IDepartment> depatmentsAll, List<Sungero.Company.IEmployee> employeesAll)
    {
      // Непосредственно подчиненные руководителю сотрудники.
      var subEmployees = DirRX.Solution.Employees.GetAll(s => employees.Contains(s.Manager) && !employeesAll.Contains(s)).ToList();
      if (subEmployees.Any())
        employeesAll.AddRange(subEmployees);
      
      // Подразделения с руководителем.
      var subDepatments = Sungero.Company.Departments.GetAll(d => employees.Contains(DirRX.Solution.Employees.As(d.Manager)) && !depatmentsAll.Contains(d)).ToList();
      if (subDepatments.Any())
      {
        depatmentsAll.AddRange(subDepatments);
        GetEmployeesDown(subDepatments, depatmentsAll, employeesAll);
      }
      
      if (subEmployees.Any())
        GetSubEmployeesDown(subEmployees, depatmentsAll, employeesAll);
      
      return employeesAll;
    }
    
    /// <summary>
    /// Получить уровень иерархии подразделения.
    /// </summary>
    /// <param name="department">Подразделение.</param>
    /// <returns>Уровень.</returns>
    public int GetHierarchyLevel(Sungero.Company.IDepartment department)
    {
      var level= 0;      
      while (department.HeadOffice != null)
      {
        level += 1;
        department = department.HeadOffice;
      }
      
      return level;
    }
    
    /// <summary>
    /// Сбор статистики по подчиненным подразделениям.
    /// </summary>
    public Structures.ApproversWorkStatisticsReport.StatisticTableLine GetSubDepartments(Sungero.Company.IDepartment department, List<Structures.ApproversWorkStatisticsReport.TableLine> dataTable,
                                                                                         List<Sungero.Company.IDepartment> depatmentsAll, List<Sungero.Company.IEmployee> employeesAll,
                                                                                         List<Sungero.Company.IEmployee> employeesFilter,
                                                                                         List<DirRX.Solution.IApprovalAssignment> approvalAssignments,
                                                                                         List<DirRX.Solution.IApprovalCheckingAssignment> riskConformationAssignments)
    {
      var statisticTableLine = Structures.ApproversWorkStatisticsReport.StatisticTableLine.Create(new List<IOfficialDocument>(), 0, 0, 0, 0.0, 0, 0.0);
      
      var lowerDepartments = Sungero.Company.Departments.GetAll(d => Sungero.Company.Departments.Equals(d.HeadOffice, department) && !depatmentsAll.Contains(d)).OrderBy(d => d.Name).ToList();
      if (lowerDepartments.Any())
      {
        depatmentsAll.AddRange(lowerDepartments);
        foreach (var lowerDepartment in lowerDepartments)
        { 
          // Сотрудники.
          var employeesStatistic = GetDepartmentEmployees(lowerDepartment, dataTable, employeesFilter, approvalAssignments, riskConformationAssignments);
          
          // Обойти подчиненные подразделения.
          var departmentStatistic = GetSubDepartments(lowerDepartment, dataTable, depatmentsAll, employeesAll, employeesFilter, approvalAssignments, riskConformationAssignments);
          
          List<IOfficialDocument> currentDocuments = new List<IOfficialDocument>();
          if (employeesStatistic.Documents.Any())
            currentDocuments.AddRange(employeesStatistic.Documents);
          if (departmentStatistic.Documents.Any())
            currentDocuments.AddRange(departmentStatistic.Documents);
          if (currentDocuments.Any())
            currentDocuments = currentDocuments.Distinct().ToList();
          
          var currentDocCount = currentDocuments.Count;
          
          if (currentDocCount > 0)
          {
            var tableLine = Structures.ApproversWorkStatisticsReport.TableLine.Create();
            tableLine.Id = lowerDepartment.Id;
            tableLine.Parent = department.Id;
            tableLine.Manager = lowerDepartment.Name;
            tableLine.ReportSessionId = ApproversWorkStatisticsReport.ReportSessionId;
            tableLine.DocumentCount = currentDocCount;
            tableLine.TotalAssignCount = employeesStatistic.TotalAssignCount + departmentStatistic.TotalAssignCount;
            tableLine.DoneAssignCount = employeesStatistic.DoneAssignCount + departmentStatistic.DoneAssignCount;
            tableLine.InWorkAssignCount = employeesStatistic.InWorkAssignCount + departmentStatistic.InWorkAssignCount;
            tableLine.IsDepartment = true;
            
            if ((employeesStatistic.OverdueCount + departmentStatistic.OverdueCount) > 0)
            {
              tableLine.OverdueCount = employeesStatistic.OverdueCount + departmentStatistic.OverdueCount;
              tableLine.OverdueAverageValue = (employeesStatistic.OverdueSum + departmentStatistic.OverdueSum) / 8.0 / (employeesStatistic.OverdueCount + departmentStatistic.OverdueCount);
            }
            
            if ((employeesStatistic.DoneAssignCount + departmentStatistic.DoneAssignCount) > 0)
              tableLine.AverageApproveTime = (employeesStatistic.ApproveTimeSum + departmentStatistic.ApproveTimeSum) / 8.0 / (employeesStatistic.DoneAssignCount + departmentStatistic.DoneAssignCount);
              
            statisticTableLine.Documents.AddRange(currentDocuments);
            statisticTableLine.TotalAssignCount += tableLine.TotalAssignCount;
            statisticTableLine.DoneAssignCount += tableLine.DoneAssignCount;
            statisticTableLine.InWorkAssignCount += tableLine.InWorkAssignCount;
            statisticTableLine.ApproveTimeSum += (employeesStatistic.ApproveTimeSum + departmentStatistic.ApproveTimeSum);
            statisticTableLine.OverdueCount += (employeesStatistic.OverdueCount + departmentStatistic.OverdueCount);
            statisticTableLine.OverdueSum += (employeesStatistic.OverdueSum + departmentStatistic.OverdueSum);
            
            dataTable.Add(tableLine);
          }
        }
      }
      
      return statisticTableLine;
    }
    
     /// <summary>
    /// Сбор статистики по работникам подразделения.
    /// </summary>
    /// <param name="department">Подразделение.</param>
    public Structures.ApproversWorkStatisticsReport.StatisticTableLine GetDepartmentEmployees(Sungero.Company.IDepartment department, List<Structures.ApproversWorkStatisticsReport.TableLine> dataTable,
                                                                                              List<Sungero.Company.IEmployee> employeesFilter,
                                                                                              List<DirRX.Solution.IApprovalAssignment> approvalAssignments, List<DirRX.Solution.IApprovalCheckingAssignment> riskConformationAssignments)
    {
      var statisticTableLine = Structures.ApproversWorkStatisticsReport.StatisticTableLine.Create(new List<IOfficialDocument>(), 0, 0, 0, 0.0, 0, 0.0);
      
      // Сотрудники подразделения.
      var employees = DirRX.Solution.Employees.GetAll(s => Sungero.Company.Departments.Equals(s.Department, department) && employeesFilter.Contains(s)).ToList();
      
      if (employees.Any())
      {
        foreach (var employee in employees)
        {
          // Статистика.
          var currentApprovalAssignments = approvalAssignments.Where(a => Equals(Sungero.Company.Employees.As(a.Performer), employee)).ToList();
          var currentRiskConformationAssignments = riskConformationAssignments.Where(a => Equals(Sungero.Company.Employees.As(a.Performer), employee)).ToList();
          
          if (currentApprovalAssignments.Any() || currentRiskConformationAssignments.Any())
          {
            List<Sungero.Workflow.IAssignment> currentAssignments = new List<Sungero.Workflow.IAssignment>();
            List<IOfficialDocument> currentDocuments = new List<IOfficialDocument>();
            
            if (currentApprovalAssignments.Any())
              currentAssignments.AddRange(currentApprovalAssignments.Cast<Sungero.Workflow.IAssignment>().ToList());
            
            if (currentRiskConformationAssignments.Any())
              currentAssignments.AddRange(currentRiskConformationAssignments.Cast<Sungero.Workflow.IAssignment>().ToList());
            
            // Документы.
            var currentApprovalDocuments = currentApprovalAssignments
              .Where(t => t.DocumentGroup.OfficialDocuments.FirstOrDefault() != null)
              .Select(t => t.DocumentGroup.OfficialDocuments.FirstOrDefault())
              .Distinct().Cast<IOfficialDocument>().ToList();
            
            var currentRiskDocuments = currentRiskConformationAssignments
              .Where(t => t.DocumentGroup.OfficialDocuments.FirstOrDefault() != null)
              .Select(t => t.DocumentGroup.OfficialDocuments.FirstOrDefault())
              .Distinct().Cast<IOfficialDocument>().ToList();
            
            if (currentApprovalDocuments.Any())
              currentDocuments.AddRange(currentApprovalDocuments);
            
            if (currentRiskDocuments.Any())
              currentDocuments.AddRange(currentRiskDocuments);
            
            currentDocuments = currentDocuments.Distinct().ToList();
            
            var currentDocCount = currentDocuments.Count;
            var totalAssignmentsCount = currentAssignments.Count;
            var completedAssignments = currentAssignments.Where(a => a.Status == Sungero.Workflow.Assignment.Status.Completed);
            var completedAssignmentsCount = completedAssignments.Count();
            // Средний срок согласования, раб. дн., не учитывается время если у инициатора запрошена информация.
            var averageApproveTime = 0.0;
            var approveTimeSum = 0.0;
            if (completedAssignmentsCount > 0)
            {
              // Получить подзадачи на запрос инициатору. Только завершенные, у которых дата завершения меньше даты завершения родительского задания.
              var requestInitiatorTimeSum = 0.0;
              var requestInitiatorTasks = LocalActs.RequestInitiatorTasks.GetAll(t => t.ParentAssignment != null && completedAssignments.Contains(Assignments.As(t.ParentAssignment)) && t.Status == Sungero.Workflow.Task.Status.Completed)
                                                                                 .Where(t => Assignments.GetAll().Where(a => Tasks.Equals(t, a.Task)).Where(a => a.Completed.HasValue).Max(a => a.Completed) < Assignments.As(t.ParentAssignment).Completed);
              if (requestInitiatorTasks.Any())
                requestInitiatorTimeSum = requestInitiatorTasks.Select(t => WorkingTime.GetDurationInWorkingHours(t.Started.Value,
                                                                                                                    Assignments.GetAll().Where(a => Tasks.Equals(t, a.Task)).Where(a => a.Completed.HasValue).Max(a => a.Completed).Value)).ToList().Sum();

             
              approveTimeSum = completedAssignments.Select(a => WorkingTime.GetDurationInWorkingHours(a.Created.Value, a.Completed.Value, a.CompletedBy)).Sum();
              averageApproveTime = (approveTimeSum - requestInitiatorTimeSum) / 8.0 / completedAssignmentsCount;
            }
            
            var inWorkAssignmentsCount = currentAssignments.Where(a => a.Status == Sungero.Workflow.Assignment.Status.InProcess).Count();
            
            // Количество просроченных заданий, Среднее время просрочки, раб. дн.
            var overdueAssignments = currentAssignments.Where(a => a.Deadline.HasValue && a.Deadline.Value < (a.Completed.HasValue ? a.Completed.Value : Calendar.Now));
            var overdueAssignmentsCount = overdueAssignments.Count();
            
            var overdueAverageValue = 0.0;
            var overduesSum = 0.0;
            if (overdueAssignmentsCount > 0)
            {
              overduesSum = overdueAssignments.Select(a => WorkingTime.GetDurationInWorkingHours(a.Deadline.Value, (a.Completed.HasValue ? a.Completed.Value : Calendar.Now), a.CompletedBy)).Sum();
              overdueAverageValue = overduesSum / 8.0 / overdueAssignmentsCount;
            }
            
            var tableLine = Structures.ApproversWorkStatisticsReport.TableLine.Create();
            tableLine.Id = employee.Id;
            tableLine.Parent = department.Id;
            tableLine.Manager = employee.Manager != null ? employee.Manager.Name : (employee.Department.Manager != null ? employee.Department.Manager.Name : string.Empty);
            tableLine.Approver = employee.Name;
            tableLine.DocumentCount = currentDocCount;
            tableLine.TotalAssignCount = totalAssignmentsCount;
            tableLine.DoneAssignCount = completedAssignmentsCount;
            tableLine.InWorkAssignCount = inWorkAssignmentsCount;
            tableLine.AverageApproveTime = averageApproveTime;
            tableLine.OverdueAverageValue = overdueAverageValue;
            tableLine.OverdueCount = overdueAssignmentsCount;            
            tableLine.IsDepartment = false;
            tableLine.ReportSessionId = ApproversWorkStatisticsReport.ReportSessionId;
            
            statisticTableLine.Documents.AddRange(currentDocuments);
            statisticTableLine.TotalAssignCount += totalAssignmentsCount;
            statisticTableLine.DoneAssignCount += completedAssignmentsCount;
            statisticTableLine.InWorkAssignCount += inWorkAssignmentsCount;
            statisticTableLine.ApproveTimeSum += approveTimeSum;
            statisticTableLine.OverdueCount += overdueAssignmentsCount;
            statisticTableLine.OverdueSum += overduesSum;
            
            dataTable.Add(tableLine);
          }
        }
      }
      
      return statisticTableLine;
    }
    
    /// <summary>
    /// Проверяет просроченна ли задача.
    /// </summary>
    /// <param name="task">Задача для проверки.</param>
    /// <returns>Признак.</returns>
    public bool IsOverdueTask(DirRX.Solution.IApprovalTask task)
    {
      if (task == null)
        return false;
      if (task.Status == Sungero.Workflow.Task.Status.Completed)
      {
        var completed = Assignments.GetAll()
          .Where(a => Tasks.Equals(task, a.Task))
          .Where(a => a.Completed.HasValue)
          .Max(a => a.Completed);
        if (completed.HasValue)
          return task.MaxDeadline.HasValue && task.MaxDeadline.Value < completed;
        else
          return false;
      }
      else
        return task.MaxDeadline.HasValue && task.MaxDeadline.Value < Calendar.Now;
    }
  }
}