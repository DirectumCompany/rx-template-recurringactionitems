using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.LocalActs.Server
{
  public class ModuleFunctions
  {

    /// <summary>
    /// Получить сотрудников по подразделению и нашей организации.
    /// </summary>
    /// <param name="businessUnit">Наша организация.</param>
    /// <param name="departments">Список подразделений.</param>
    /// <returns>Список видов документов.</returns>
    [Remote]
    public List<Sungero.Company.IEmployee> GetFilteredEmployees(Sungero.Company.IBusinessUnit businessUnit, List<Sungero.Company.IDepartment> departments)
    {
      var employees = DirRX.Solution.Employees.GetAll()
        .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
      
      if (businessUnit != null)
        employees = employees.Where(d => Equals(d.BusinessUnit, businessUnit));
      
      if (departments.Any())
        employees = employees.Where(d => departments.Contains(d.Department));
      
      return employees.Cast<Sungero.Company.IEmployee>().ToList();
    }
    
    /// <summary>
    /// Получить список типовых форм по теме.
    /// </summary>
    /// <param name="orderSubject">Тема.</param>
    /// <returns>Список типовых форм.</returns>
    [Remote]
    public List<IStandardForm> GetFilteredStandardForms(List<IOrderSubject> orderSubjects)
    {
      var standardForms = StandardForms.GetAll()
        .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
      
      // Типовые формы фильтруются по теме.
      if (orderSubjects.Any())
        return standardForms.Where(d => orderSubjects.Contains(d.Subject)).ToList();
      
      return standardForms.ToList();
    }
    
    /// <summary>
    /// Получить список видов документов по типу документа.
    /// </summary>
    /// <param name="documentType">Тип документа.</param>
    /// <returns>Список видов документов.</returns>
    [Remote]
    public List<Sungero.Docflow.IDocumentKind> GetFilteredDocKinds(Sungero.Docflow.IDocumentType documentType, List<Enumeration> directions)
    {
      var docKinds = Sungero.Docflow.PublicFunctions.DocumentKind.Remote.GetDocumentKinds()
        .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
      
      // Виды документов фильтруются по типу.
      if (documentType != null)
        docKinds = docKinds.Where(d => Equals(d.DocumentType, documentType));
      
      if (directions.Any())
        docKinds = docKinds.Where(d => directions.Contains(d.DocumentFlow.Value));
      
      return docKinds.ToList();
    }
    
    /// <summary>
    /// Получить список подразделений по Нашей организации.
    /// </summary>
    /// <param name="businessUnit">Наша организация.</param>
    /// <returns>Список подразделений.</returns>
    [Remote]
    public List<Sungero.Company.IDepartment> GetFilteredDepartments(Sungero.Company.IBusinessUnit businessUnit)
    {
      var departments = Sungero.Company.PublicFunctions.Department.Remote.GetDepartments()
        .Where(d => d.Status == Sungero.CoreEntities.DatabookEntry.Status.Active);
      
      // Подразделения фильтруются по НОР.
      if (businessUnit != null)
        return departments.Where(d => Equals(d.BusinessUnit, businessUnit)).ToList();
      
      return departments.ToList();
    }

    #region Скопировано из стандартной разработки рассылки о новых заданиях.

    /// <summary>
    /// Получить дату последней рассылки уведомлений.
    /// </summary>
    /// <returns>Дата последней рассылки.</returns>
    [Public]
    public static DateTime GetLastNotificationDate(string key)
    {
      var command = string.Format(Queries.Module.SelectDocflowParamsValue, key);
      try
      {
        var executionResult = Sungero.Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
        var date = string.Empty;
        if (!(executionResult is DBNull) && executionResult != null)
          date = executionResult.ToString();
        else
          return Calendar.Today;
        
        Logger.DebugFormat("Last notification by assignment date in DB is {0} (UTC)", date);
        
        DateTime result = Calendar.FromUtcTime(DateTime.Parse(date, null, System.Globalization.DateTimeStyles.AdjustToUniversal));

        if ((result - Calendar.Now).TotalDays > 1)
          return Calendar.Today;
        else
          return result;
      }
      catch (Exception ex)
      {
        Logger.Error("Error while getting last notification by assignment date", ex);
        return Calendar.Today;
      }
    }
    
    /// <summary>
    /// Обновить дату последней рассылки уведомлений.
    /// </summary>
    /// <param name="notificationDate">Дата рассылки уведомлений.</param>
    [Public]
    public static void UpdateLastNotificationDate(string key, DateTime notificationDate)
    {
      var newDate = notificationDate.Add(-Calendar.UtcOffset).ToString("yyyy-MM-ddTHH:mm:ss.ffff+0");
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.Module.InsertOrUpdateDocflowParamsValue, new[] { key, newDate });
      Logger.DebugFormat("Last notification by assignment date is set to {0} (UTC)", newDate);
    }
    
    #endregion
    

    /// <summary>
    /// Получить роль, участники которых исключаются из списка ознакомления.
    /// </summary>
    /// <returns>Роль</returns>
    [Remote(IsPure = true), Public]
    public static IRole GetExcludeFromAcquaintanceTaskRole()
    {
      return Roles.GetAll(r => r.Sid == LocalActs.PublicConstants.Module.RoleGuid.ExcludeFromAcquaintanceTaskRole).FirstOrDefault();
    }
    
    #region Лист согласования.
    
    /// <summary>
    /// Заполнить SQL таблицу для формирования отчета "Лист согласования".
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="reportSessionId">Идентификатор отчета.</param>
    [Public]
    public static void UpdateApprovalSheetReportTable(Sungero.Docflow.IOfficialDocument document, string reportSessionId)
    {
      var filteredSignatures = new Dictionary<string, Sungero.Domain.Shared.ISignature>();
      
      var setting = Sungero.Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(null);
      var showNotApproveSign = setting != null ? setting.ShowNotApproveSign == true : false;
      
      foreach (var version in document.Versions.OrderByDescending(v => v.Created))
      {
        var versionSignatures = Signatures.Get(version).Where(s => (showNotApproveSign || s.SignatureType != SignatureType.NotEndorsing)
                                                              && s.IsExternal != true
                                                              && !filteredSignatures.ContainsKey(Solution.PublicFunctions.ApprovalTask.GetSignatureKey(s, version.Number.Value)));
        var lastSignaturesInVersion = versionSignatures
          .GroupBy(v => Solution.PublicFunctions.ApprovalTask.GetSignatureKey(v, version.Number.Value))
          .Select(grouping => grouping.Where(s => s.SigningDate == grouping.Max(last => last.SigningDate)).First());
        
        foreach (Sungero.Domain.Shared.ISignature signature in lastSignaturesInVersion)
        {
          filteredSignatures.Add(Solution.PublicFunctions.ApprovalTask.GetSignatureKey(signature, version.Number.Value), signature);
          if (!signature.IsValid)
            foreach (var error in signature.ValidationErrors)
              Logger.DebugFormat("UpdateApprovalSheetReportTable: reportSessionId {0}, document {1}, version {2}, signatory {3}, substituted user {7}, signature {4}, with error {5} - {6}",
                                 reportSessionId, document.Id, version.Number,
                                 signature.Signatory != null ? signature.Signatory.Name : signature.SignatoryFullName, signature.Id, error.ErrorType, error.Message,
                                 signature.SubstitutedUser != null ? signature.SubstitutedUser.Name : string.Empty);
          
          var employeeName = string.Empty;
          if (signature.SubstitutedUser == null)
          {
            var additionalInfo = (signature.AdditionalInfo ?? string.Empty).Replace("|", " ").Trim();
            employeeName = string.Format("<b>{1}</b>{0}", signature.SignatoryFullName, Solution.PublicFunctions.ApprovalTask.AddEndOfLine(additionalInfo)).Trim();
          }
          else
          {
            var additionalInfos = (signature.AdditionalInfo ?? string.Empty).Split(new char[] { '|' }, StringSplitOptions.None);
            if (additionalInfos.Count() == 3)
            {
              // Замещающий.
              var signatoryAdditionalInfo = additionalInfos[0];
              if (!string.IsNullOrEmpty(signatoryAdditionalInfo))
                signatoryAdditionalInfo = Solution.PublicFunctions.ApprovalTask.AddEndOfLine(string.Format("<b>{0}</b>", signatoryAdditionalInfo));
              var signatoryText = Solution.PublicFunctions.ApprovalTask.AddEndOfLine(string.Format("{0}{1}", signatoryAdditionalInfo, signature.SignatoryFullName));
              
              // Замещаемый.
              var substitutedUserAdditionalInfo = additionalInfos[1];
              if (!string.IsNullOrEmpty(substitutedUserAdditionalInfo))
                substitutedUserAdditionalInfo = Solution.PublicFunctions.ApprovalTask.AddEndOfLine(string.Format("<b>{0}</b>", substitutedUserAdditionalInfo));
              var substitutedUserText = string.Format("{0}{1}", substitutedUserAdditionalInfo, signature.SubstitutedUserFullName);
              
              // Замещающий за замещаемого.
              var onBehalfOfText = Solution.PublicFunctions.ApprovalTask.AddEndOfLine(DirRX.LocalActs.Resources.OnBehalfOf);
              employeeName = string.Format("{0}{1}{2}", signatoryText, onBehalfOfText, substitutedUserText);
            }
            else if (additionalInfos.Count() == 2)
            {
              // Замещаюший / Система.
              var signatoryText = Solution.PublicFunctions.ApprovalTask.AddEndOfLine(signature.SignatoryFullName);
              
              // Замещаемый.
              var substitutedUserAdditionalInfo = additionalInfos[0];
              if (!string.IsNullOrEmpty(substitutedUserAdditionalInfo))
                substitutedUserAdditionalInfo = Solution.PublicFunctions.ApprovalTask.AddEndOfLine(string.Format("<b>{0}</b>", substitutedUserAdditionalInfo));
              var substitutedUserText = string.Format("{0}{1}", substitutedUserAdditionalInfo, signature.SubstitutedUserFullName);
              
              // Система за замещаемого.
              var onBehalfOfText = Solution.PublicFunctions.ApprovalTask.AddEndOfLine(DirRX.LocalActs.Resources.OnBehalfOf);
              employeeName = string.Format("{0}{1}{2}", signatoryText, onBehalfOfText, substitutedUserText);
            }
          }
          
          var riskInfo = string.Empty;
          var resultString = string.Empty;
          // Информация о риске
          var riskBySignatory = Risks.Null;
          AccessRights.AllowRead(
            () =>
            {
              var lastApprovalTask = DirRX.LocalActs.PublicFunctions.Module.GetLastApprovalTask(document);
              if (lastApprovalTask != null)
              {
                IUser userSignatory = null;
                if (signature.SubstitutedUser != null)
                  userSignatory = signature.SubstitutedUser;
                else
                  userSignatory = signature.Signatory;

                riskBySignatory = lastApprovalTask.RiskAttachmentGroup.Risks.Where(r => r.Status == DirRX.LocalActs.Risk.Status.Active
                                                                                   && Equals(r.Author, userSignatory)).SingleOrDefault();
                if (riskBySignatory != null)
                  resultString = DirRX.LocalActs.Resources.ApprovedWithRisk;
              }
            });

          if (riskBySignatory != null && riskBySignatory.AccessRights.CanRead(Users.Current))
            riskInfo = string.Format("{0}:{1}{2}{3}{4}{5}",
                                     DirRX.LocalActs.Resources.RiskLevel, Environment.NewLine, riskBySignatory.Level.Name, Environment.NewLine, Environment.NewLine, riskBySignatory.Description);
          
          var commandText = Queries.ApprovalSheetReport.InsertIntoApprovalSheetReportTable;
          
          using (var command = SQL.GetCurrentConnection().CreateCommand())
          {
            var separator = ", ";
            var errorString = Sungero.Docflow.PublicFunctions.Module.GetSignatureValidationErrorsAsString(signature, separator);
            var signErrors = string.IsNullOrWhiteSpace(errorString)
              ? DirRX.Solution.ApprovalTasks.Resources.SignStatusActive
              : Sungero.Docflow.PublicFunctions.Module.ReplaceFirstSymbolToUpperCase(errorString.ToLower());
            if (string.IsNullOrEmpty(resultString))
              resultString = Solution.PublicFunctions.ApprovalTask.GetEndorsingResultFromSignature(signature, false);
            command.CommandText = commandText;
            SQL.AddParameter(command, "@reportSessionId",  reportSessionId, System.Data.DbType.String);
            SQL.AddParameter(command, "@employeeName",  employeeName, System.Data.DbType.String);
            SQL.AddParameter(command, "@resultString",  resultString, System.Data.DbType.String);
            SQL.AddParameter(command, "@comment",  signature.Comment, System.Data.DbType.String);
            SQL.AddParameter(command, "@signErrors",  signErrors, System.Data.DbType.String);
            SQL.AddParameter(command, "@versionNumber",  version.Number, System.Data.DbType.Int32);
            SQL.AddParameter(command, "@SignatureDate",  signature.SigningDate.FromUtcTime(), System.Data.DbType.DateTime);
            SQL.AddParameter(command, "@Risk", riskInfo, System.Data.DbType.String);
            
            command.ExecuteNonQuery();
          }
        }
      }
    }
    
    /// <summary>
    /// Получить последнюю задачу на согласование документа.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Задача.</returns>
    [Public]
    public static Solution.IApprovalTask GetLastApprovalTask(Sungero.Docflow.IOfficialDocument document)
    {
      return DirRX.Solution.ApprovalTasks.GetAll(t => t.AttachmentDetails.Any(g => g.GroupId == DirRX.LocalActs.Constants.Module.DocumentGroupApprovalTask && g.AttachmentId == document.Id))
        .OrderByDescending(d => d.Started)
        .FirstOrDefault();
    }
    
    #endregion
  }
}