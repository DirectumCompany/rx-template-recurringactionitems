using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data.SqlClient;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.IntegrationLLK.Server
{
  public class ModuleJobs
  {

    /// <summary>
    /// Фоновый процесс импорта отсутствий сотрудников.
    /// </summary>
    public virtual void AbsenceOfEmployeesJob()
    {
      string path = PublicFunctions.Module.Remote.GetServerConfigValue("EXCHANGE_HOT_FOLDER");
      string fileName = PublicFunctions.Module.Remote.GetServerConfigValue("EXCHANGE_ABSENCE_FILE_NAME");
      string delimiter = path.Substring(path.Length-1) == @"\" ? string.Empty : @"\";
      string filePath = string.Format("{0}{1}{2}", path, delimiter, fileName);
      string activeText = string.Empty;
      
      try
      {
        using (SpreadsheetDocument doc = SpreadsheetDocument.Open(filePath, false))
        {
          WorkbookPart workbookPart = doc.WorkbookPart;
          WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
          Worksheet sheet = worksheetPart.Worksheet;
          SharedStringTablePart sstpart = workbookPart.GetPartsOfType<SharedStringTablePart>().First();
          SharedStringTable sst = sstpart.SharedStringTable;
          
          var rows = sheet.Descendants<Row>();
          var cols = sheet.Descendants<Column>();

          // Вычислить лист с содержимым.
          foreach (WorksheetPart part in workbookPart.WorksheetParts)
          {
            Worksheet sheetCheck = part.Worksheet;
            rows = sheetCheck.Descendants<Row>();
            cols = sheetCheck.Descendants<Column>();

            if (cols.Count() > 0)
              break;
          }

          #region Валидация формата XLSX.
          // Проверить количество колонок.
          var headRow = rows.FirstOrDefault();
          int columnCount = headRow == null ? 0 : columnCount = headRow.Elements<Cell>().Count();
          if (columnCount != 7)
          {
            Logger.Debug(string.Format("{0}: {1}", Resources.ImportAbsenceIncorrectExcel, Resources.ImportAbsenceInvalidColumnCount));
            activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.ImportAbsenceIncorrectExcel);
            activeText = Functions.Module.NoticeTextAddLine(activeText, "\t" + Resources.ImportAbsenceInvalidColumnCount);
            
            return;
          }
          
          // Проверить ключевые поля.
          var firstRow = rows.Skip(1).FirstOrDefault();
          if (firstRow != null)
          {
            int cellColumnIndex = 1;
            bool isNumberCorrect = true;
            bool isStartCorrect = true;
            bool isEndCorrect = true;
            foreach (Cell c in firstRow.Elements<Cell>())
            {
              double oaDate;
              int testInt;
              DateTime testDate;
              switch (cellColumnIndex)
              {
                case 1:
                  // Табельный номер.
                  isNumberCorrect = int.TryParse(c.CellValue.InnerText.Trim(), out testInt);
                  break;
                case 5:
                  // Начало.
                  if (double.TryParse(c.CellValue.InnerText, out oaDate))
                    isStartCorrect = Calendar.TryParseDate(DateTime.FromOADate(oaDate).ToShortDateString(), out testDate);
                  break;
                case 6:
                  // Истечение.
                  if (double.TryParse(c.CellValue.InnerText, out oaDate))
                    isStartCorrect = Calendar.TryParseDate(DateTime.FromOADate(oaDate).ToShortDateString(), out testDate);
                  break;
              }
              cellColumnIndex++;
            }
            if (!isNumberCorrect || !isEndCorrect || !isEndCorrect)
            {
              Logger.Debug(string.Format("{0}: {1}", Resources.ImportAbsenceIncorrectExcel, Resources.ImportAbsenceInvalidFormatExcel));
              activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.ImportAbsenceIncorrectExcel);
              activeText = Functions.Module.NoticeTextAddLine(activeText, "\t" + Resources.ImportAbsenceInvalidFormatExcel);
              
              return;
            }
          }
          #endregion
          
          #region Обработка принятого XLSX.
          var rowCount = rows.Count();
          Logger.Debug(Resources.ImportAbsenceHeaderFormat(rowCount - 1));
          
          List<DirRX.Solution.IEmployee> absenceList = new List<DirRX.Solution.IEmployee>();
          
          // Пропускаем заголовки таблицы.
          var rowIndex = 1;
          foreach (Row row in rows.Skip(1))
          {
            if (string.IsNullOrEmpty(row.InnerText))
              continue;
            
            var item = Structures.Module.AbsenceOfEmployee.Create();
            int cellColumnIndex = 1;
            foreach (Cell c in row.Elements<Cell>())
            {
              // Получить значение ячейки.
              if (c.CellValue != null)
              {
                DateTime? innerDate = null;
                var innerText = c.CellValue.InnerText;
                if (c.DataType == null)
                {
                  // Формат ячейки - числа или даты.
                  double oaDate;
                  if (double.TryParse(c.InnerText, out oaDate))
                    innerDate = DateTime.FromOADate(oaDate);
                }
                else
                {
                  // Формат ячейки - строки и булевы значения.
                  if (c.DataType == CellValues.SharedString)
                    innerText = sst.ChildElements[int.Parse(c.CellValue.Text)].InnerText;
                }
                
                switch (cellColumnIndex)
                {
                  case 1:
                    // Табельный номер.
                    int pn;
                    if (int.TryParse(innerText.Trim(), out pn))
                      // Дополнить ведущими нулями до 8 символов.
                      item.PersonalNumber = string.Format("{0:d8}", pn);
                    // TODO: Если согласуем жесткий формат XLSX с табельными номерами как в БД, то не нужно дополнять ведущими нулями и надо раскомментировать.
                    //item.PersonalNumber = innerText.Trim();
                    break;
                  case 2:
                    // ФИО сотрудника.
                    item.EmployeeName = innerText.Trim();
                    break;
                  case 3:
                    // Штатная должность.
                    item.Position = innerText.Trim();
                    break;
                  case 4:
                    // Вид отсутствия или присутствия.
                    item.AbsenceType = innerText.Trim();
                    break;
                  case 5:
                    // Начало.
                    if (innerDate.HasValue)
                      item.StartDate = innerDate.Value;
                    break;
                  case 6:
                    // Истечение.
                    if (innerDate.HasValue)
                      item.EndDate = innerDate.Value;
                    break;
                  case 7:
                    // Примечание.
                    item.Comment = innerText.Trim();
                    break;
                }
                cellColumnIndex++;
              }
            }

            // Проверить значения для заполнения обязательных реквизитов из карточки отсутствия сотрудника.
            if (!string.IsNullOrWhiteSpace(item.PersonalNumber) && item.StartDate != null && item.EndDate != null)
            {
              var employee = DirRX.Solution.Employees.GetAll(e => e.PersonnelNumber.Trim().ToUpper() == item.PersonalNumber.Trim().ToUpper()).FirstOrDefault();
              if (employee != null)
              {
                try
                {
                  item.Employee = employee;
                  activeText += UpdateAbsenceOfEmployee(item);
                  absenceList.Add(employee);
                }
                catch (Exception ex)
                {
                  Logger.Error(Resources.ImportAbsenceCreateRecordError, ex);
                  activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.ImportAbsenceCreateRecordError);
                  activeText = Functions.Module.NoticeTextAddLine(activeText, ex.Message);
                }
              }
              else
              {
                // Не вычислен сотрудник.
                Logger.Error(Resources.ImportAbsenceGettingEmployeeErrorFormat(item.EmployeeName, item.PersonalNumber));
                activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.ImportAbsenceGettingEmployeeErrorFormat(item.EmployeeName, item.PersonalNumber));
              }
            }
            else
            {
              Logger.Error(Resources.ImportAbsenceRequiredPropertyEmptyFormat(rowIndex, item.EmployeeName, item.PersonalNumber));
              activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.ImportAbsenceRequiredPropertyEmptyFormat(rowIndex, item.EmployeeName, item.PersonalNumber));
            }

            rowIndex++;
          }
          #endregion
          
          #region Закрыть записи, которые истекли или отсутствуют в файле.
          var expiredAbsences = AbsenceOfEmployees.GetAll(a => a.Status == IntegrationLLK.AbsenceOfEmployee.Status.Active &&
                                                          ((a.StartDate.HasValue && a.StartDate.Value > Calendar.Today) ||
                                                           (a.EndDate.HasValue && a.EndDate.Value < Calendar.Today)));
          foreach (IAbsenceOfEmployee absence in expiredAbsences)
          {
            absence.Status = IntegrationLLK.AbsenceOfEmployee.Status.Closed;
            absence.Save();
            
            activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.ImportAbsenceClosedSuccessfullyFormat(Sungero.Core.Hyperlinks.Get(absence)));
          }
          
          var deletedAbsences = AbsenceOfEmployees.GetAll(a => a.Status == IntegrationLLK.AbsenceOfEmployee.Status.Active &&
                                                          !absenceList.Contains(a.Employee));
          foreach (IAbsenceOfEmployee absence in deletedAbsences)
          {
            absence.Status = IntegrationLLK.AbsenceOfEmployee.Status.Closed;
            absence.Save();
            
            activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.ImportAbsenceClosedSuccessfullyFormat(Sungero.Core.Hyperlinks.Get(absence)));
          }
          #endregion
        }
      }
      catch (Exception ex)
      {
        Logger.Error(Resources.ImportAbsenceReadFileErrorFormat(filePath), ex);
        activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.ImportAbsenceReadFileErrorFormat(filePath));
        activeText = Functions.Module.NoticeTextAddLine(activeText, ex.Message);
      }
      
      // Фомирование уведомления администратору.
      Functions.Module.SendImportResultsNotice(Resources.ImportAbsenceNoticeSubjectFormat(Calendar.Now.ToString("g")), activeText);
      
      // Удалить файл.
      FileInfo fileInf = new FileInfo(filePath);
      if (fileInf.Exists)
        fileInf.Delete();
    }
    
    #region Обработка считанных отсутствий сотрудников.
    
    /// <summary>
    /// Обработка принятого отсутствия сотрудника.
    /// </summary>
    /// <param name="item">Экземпляр отсутствия сотрудника загруженного из файла.</param>
    /// <returns>Сообщение с результатом обработки.</returns>
    public static string UpdateAbsenceOfEmployee(Structures.Module.IAbsenceOfEmployee item)
    {
      string activeText = string.Empty;
      bool checkSubstitution = false;
      
      var absence = IntegrationLLK.PublicFunctions.AbsenceOfEmployee.GetTodayEmployeeAbsence(item);
      if (absence == null)
      {
        if (item.StartDate <= Calendar.Today && item.EndDate >= Calendar.Today)
        {
          // Создать новое отсутствие.
          absence = IntegrationLLK.PublicFunctions.AbsenceOfEmployee.CreateEmployeeAbsence(item);
          checkSubstitution = true;
          
          Logger.DebugFormat("Зафиксировано отсутствие сотрудника {0} с табельным номером: {1}", item.EmployeeName, item.PersonalNumber);
          activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.ImportAbsenceCreatedSuccessfullyFormat(Sungero.Core.Hyperlinks.Get(absence)));
        }
        else
        {
          // Даты отсутствия не пересекаются с текущей.
          Logger.Error(Resources.ImportAbsenceWrongDatesFormat(item.EmployeeName, item.PersonalNumber, item.StartDate.ToString("d"), item.EndDate.ToString("d")));
          activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.ImportAbsenceWrongDatesFormat(item.EmployeeName, item.PersonalNumber, item.StartDate.ToString("d"), item.EndDate.ToString("d")));
        }
      }
      else
      {
        // Проверять замещение, если только изменились даты.
        checkSubstitution = absence.StartDate != item.StartDate || absence.EndDate != item.EndDate;
        
        // Обновить отсутствие при условии изменения хотя бы одного значения.
        if (checkSubstitution || absence.Reason != item.AbsenceType || absence.Comment != item.Comment)
        {
          absence.StartDate = item.StartDate;
          absence.EndDate = item.EndDate;
          absence.Reason = item.AbsenceType;
          absence.Comment = item.Comment;
          absence.Save();
          
          Logger.DebugFormat("Зафиксировано изменение в отсутствии сотрудника {0} с табельным номером: {1}", item.EmployeeName, item.PersonalNumber);
          activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.ImportAbsenceUpdatedSuccessfullyFormat(Sungero.Core.Hyperlinks.Get(absence)));
        }
      }

      if (absence != null && checkSubstitution)
      {
        // Есть ли замещающие текущего сотрудника.
        var employeeUser = Sungero.CoreEntities.Users.As(absence.Employee);
        bool isSubstitute = ProcessSubstitutionModule.ProcessSubstitutions.GetAll(s => Sungero.CoreEntities.Users.Equals(s.Employee , employeeUser) &&
                                                                                  (!s.BeginDate.HasValue || s.BeginDate.Value <= Calendar.Today) &&
                                                                                  (!s.EndDate.HasValue || s.EndDate.Value >= Calendar.Today)).Any();
        
        if (employeeUser != null && !isSubstitute)
          Functions.Module.SendSubstitutionNotice(absence);
      }
      
      return activeText;
    }
    
    #endregion

    /// <summary>
    /// Фоновый процесс импорта данных из ССПД.
    /// </summary>
    public virtual void ImportOrganizationStructureFromSSPD()
    {
      string paramKey = Constants.Module.LastSSPDImportDateTimeDocflowParamName;
      var previousDate = GetLastExecutionProccessDateTime(paramKey);
      Logger.DebugFormat("Предыдущая дата успешного импорта {0}", previousDate.ToString());
      var today = Calendar.Now;
      bool isError = false;

      var balanceUnits = new List<Structures.Module.SSPDBalanceUnit>();
      var departments = new List<Structures.Module.SSPDOrgstructure>();
      var persons = new List<Structures.Module.SSPDPerson>();
      var managers = new List<Structures.Module.SSPDManager>();

      string connectionSting = string.Format("server={0};user id={1};password={2};initial catalog={3}",
                                             PublicFunctions.Module.Remote.GetServerConfigValue("SSPD_SQL_SERVER_NAME"),
                                             PublicFunctions.Module.Remote.GetServerConfigValue("SSPD_SQL_USERNAME"),
                                             PublicFunctions.Module.Remote.GetServerConfigValue("SSPD_SQL_PASSWORD"),
                                             PublicFunctions.Module.Remote.GetServerConfigValue("SSPD_DATABASE_NAME"));
      
      // Сформировать список Guid наших организаций из настройки.
      List<string> ourFirmIDs = PublicFunctions.Module.Remote.GetServerConfigValues("//var[@name='BALANCE_UNIT_GUID']");
      List<Guid> ourFirmGuids = new List<Guid>();
      foreach (string ourFirmID in ourFirmIDs)
        ourFirmGuids.Add(Guid.Parse(ourFirmID));

      var connection = new SqlConnection(connectionSting);
      try
      {
        connection.Open();
        Logger.DebugFormat("Подключение к серверу {0} успешно установлено", PublicFunctions.Module.Remote.GetServerConfigValue("SSPD_SQL_SERVER_NAME"));
        
        var balanceUnitsViewName = PublicFunctions.Module.Remote.GetServerConfigValue("SSPD_BALANCE_UNITS_VIEW_NAME");
        if (string.IsNullOrEmpty(balanceUnitsViewName))
          throw new Exception("В файле конфигурации не указано представление для балансовых единиц.");
        
        var orgstructureViewName = PublicFunctions.Module.Remote.GetServerConfigValue("SSPD_ORGSTRUCTURE_VIEW_NAME");
        if (string.IsNullOrEmpty(orgstructureViewName))
          throw new Exception("В файле конфигурации не указано представление для оргструктуры.");
        
        var personViewName = PublicFunctions.Module.Remote.GetServerConfigValue("SSPD_PERSON_VIEW_NAME");
        if (string.IsNullOrEmpty(personViewName))
          throw new Exception("В файле конфигурации не указано представление для персон.");
        
        #region Выборка из таблицы BalanceUnit.
        var selectBalanceUnits = Queries.Module.SSPDSelectBalanceUnits;
        selectBalanceUnits = string.Format(selectBalanceUnits, balanceUnitsViewName);
        if (!previousDate.HasValue)
          selectBalanceUnits = string.Format("{0} WHERE isBlocked = 0", selectBalanceUnits);

        try
        {
          var commandBalanceUnits = new SqlCommand(selectBalanceUnits, connection);
          using (var readerBalanceUnits = commandBalanceUnits.ExecuteReader())
          {
            while (readerBalanceUnits.Read())
            {
              balanceUnits.Add(Structures.Module.SSPDBalanceUnit.Create(readerBalanceUnits.GetGuid(readerBalanceUnits.GetOrdinal("balanceUnitId")),
                                                                        SafeGetString(readerBalanceUnits, readerBalanceUnits.GetOrdinal("balanceUnitName")),
                                                                        SafeGetInt(readerBalanceUnits, readerBalanceUnits.GetOrdinal("ksssBalanceUnitId")),
                                                                        SafeGetInt(readerBalanceUnits, readerBalanceUnits.GetOrdinal("ksssContragentId")),
                                                                        readerBalanceUnits.GetBoolean(readerBalanceUnits.GetOrdinal("isBlocked"))));
            }

            Logger.DebugFormat("Количество организаций (наших организаций) готовых к обработке: {0}", balanceUnits.Count.ToString());
          }
        }
        catch (Exception ex)
        {
          Logger.Error("При получении данных из таблицы BalanceUnit возникла ошибка", ex);
          isError = true;
        }
        #endregion

        #region Выборка из таблицы Orgstructure.
        var selectOrgstructure = previousDate.HasValue ? Queries.Module.SSPDSelectActualOrgstructure : Queries.Module.SSPDSelectActiveOrgstructure;
        selectOrgstructure = string.Format(selectOrgstructure, orgstructureViewName);

        try
        {
          var commandOrgstructure = new SqlCommand(selectOrgstructure, connection);

          if (previousDate.HasValue)
          {
            var parameter = commandOrgstructure.CreateParameter();
            parameter.ParameterName = "@previousDate";
            parameter.Direction = System.Data.ParameterDirection.Input;
            parameter.DbType = System.Data.DbType.DateTime;
            parameter.Value = previousDate.Value;
            commandOrgstructure.Parameters.Add(parameter);
          }
          using (var readerOrgstructure = commandOrgstructure.ExecuteReader())
          {
            while (readerOrgstructure.Read())
            {
              departments.Add(Structures.Module.SSPDOrgstructure.Create(readerOrgstructure.GetGuid(readerOrgstructure.GetOrdinal("orgstructureId")),
                                                                        SafeGetString(readerOrgstructure, readerOrgstructure.GetOrdinal("ouid")),
                                                                        SafeGetString(readerOrgstructure, readerOrgstructure.GetOrdinal("displayName")),
                                                                        SafeGetString(readerOrgstructure, readerOrgstructure.GetOrdinal("ounr")),
                                                                        readerOrgstructure.GetGuid(readerOrgstructure.GetOrdinal("balanceUnitId")),
                                                                        SafeGetString(readerOrgstructure, readerOrgstructure.GetOrdinal("pouid")),
                                                                        readerOrgstructure.GetInt32(readerOrgstructure.GetOrdinal("isBlocked")) != 0,
                                                                        null,
                                                                        null));
            }
          }
          Logger.DebugFormat("Количество подразделений (подразделений контрагентов) готовых к обработке: {0}", departments.Count.ToString());
        }
        catch (Exception ex)
        {
          Logger.Error("При получении данных из таблицы Orgstructure возникла ошибка", ex);
          isError = true;
        }
        #endregion

        #region Выборка из таблицы Person.
        string selectPersons = previousDate.HasValue ? Queries.Module.SSPDSelectActualPersons : Queries.Module.SSPDSelectActivePersons;
        selectPersons = string.Format(selectPersons, personViewName);

        try
        {
          var commandPerson = new SqlCommand(selectPersons, connection);
          if (previousDate.HasValue)
          {
            var parameter = commandPerson.CreateParameter();
            parameter.ParameterName = "@previousDate";
            parameter.Direction = System.Data.ParameterDirection.Input;
            parameter.DbType = System.Data.DbType.DateTime;
            parameter.Value = previousDate.Value;
            commandPerson.Parameters.Add(parameter);
          }
          using (var readerPersons = commandPerson.ExecuteReader())
          {
            while (readerPersons.Read())
            {
              string fullName = string.Format("{0} {1} {2}",
                                              SafeGetString(readerPersons, readerPersons.GetOrdinal("Sn")),
                                              SafeGetString(readerPersons, readerPersons.GetOrdinal("givenName")),
                                              SafeGetString(readerPersons, readerPersons.GetOrdinal("middleName"))).Trim();

              string officeAddress = string.Empty;
              officeAddress = Functions.Module.AddStringPart(officeAddress, SafeGetString(readerPersons, readerPersons.GetOrdinal("postalCode")));
              officeAddress = Functions.Module.AddStringPart(officeAddress, SafeGetString(readerPersons, readerPersons.GetOrdinal("Co")));
              officeAddress = Functions.Module.AddStringPart(officeAddress, SafeGetString(readerPersons, readerPersons.GetOrdinal("L")));
              officeAddress = Functions.Module.AddStringPart(officeAddress, SafeGetString(readerPersons, readerPersons.GetOrdinal("Street")));
              officeAddress = Functions.Module.AddStringPart(officeAddress, SafeGetString(readerPersons, readerPersons.GetOrdinal("Building")));
              officeAddress = Functions.Module.AddStringPart(officeAddress, SafeGetString(readerPersons, readerPersons.GetOrdinal("House")));
              officeAddress = Functions.Module.AddStringPart(officeAddress, SafeGetString(readerPersons, readerPersons.GetOrdinal("physicalDeliveryOfficeName")));

              string officePhones = string.Empty;
              officePhones = Functions.Module.AddStringPart(officePhones, SafeGetString(readerPersons, readerPersons.GetOrdinal("phoneCity")));
              officePhones = Functions.Module.AddStringPart(officePhones, SafeGetString(readerPersons, readerPersons.GetOrdinal("phoneInt")));

              persons.Add(Structures.Module.SSPDPerson.Create(readerPersons.GetGuid(readerPersons.GetOrdinal("personId")),
                                                              SafeGetString(readerPersons, readerPersons.GetOrdinal("Sn")),
                                                              SafeGetString(readerPersons, readerPersons.GetOrdinal("givenName")),
                                                              SafeGetString(readerPersons, readerPersons.GetOrdinal("middleName")),
                                                              fullName,
                                                              SafeGetString(readerPersons, readerPersons.GetOrdinal("Pernr")),
                                                              SafeGetString(readerPersons, readerPersons.GetOrdinal("personLogin")),
                                                              SafeGetString(readerPersons, readerPersons.GetOrdinal("Title")),
                                                              SafeGetString(readerPersons, readerPersons.GetOrdinal("Mail")),
                                                              officeAddress,
                                                              officePhones,
                                                              readerPersons.GetGuid(readerPersons.GetOrdinal("orgstructureId")),
                                                              readerPersons.GetGuid(readerPersons.GetOrdinal("balanceUnitId")),
                                                              readerPersons.GetInt32(readerPersons.GetOrdinal("isBlocked")) != 0,
                                                              readerPersons.GetInt32(readerPersons.GetOrdinal("isManager")) == 1,
                                                              null,
                                                              null));
            }
          }
          Logger.DebugFormat("Количество сотрудников (персон) готовых к обработке: {0}", persons.Count.ToString());
        }
        catch (Exception ex)
        {
          Logger.Error("При получении данных из таблицы Person возникла ошибка", ex);
          isError = true;
        }
        #endregion
        
        #region Выборка из таблицы Person. Руководители подразделений.
        string selectManagers = Queries.Module.SSPDSelectActiveManagers;
        selectManagers = string.Format(selectManagers, personViewName);

        try
        {
          var commandManager = new SqlCommand(selectManagers, connection);
          
          using (var readerManagers = commandManager.ExecuteReader())
          {
            while (readerManagers.Read())
            {
              string fullName = string.Format("{0} {1} {2}",
                                              SafeGetString(readerManagers, readerManagers.GetOrdinal("Sn")),
                                              SafeGetString(readerManagers, readerManagers.GetOrdinal("givenName")),
                                              SafeGetString(readerManagers, readerManagers.GetOrdinal("middleName"))).Trim();

              managers.Add(Structures.Module.SSPDManager.Create(readerManagers.GetGuid(readerManagers.GetOrdinal("personId")),
                                                                fullName,
                                                                SafeGetString(readerManagers, readerManagers.GetOrdinal("Pernr")),
                                                                readerManagers.GetGuid(readerManagers.GetOrdinal("orgstructureId")),
                                                                readerManagers.GetGuid(readerManagers.GetOrdinal("balanceUnitId"))));
            }
          }
          Logger.DebugFormat("Количество руководителей готовых к обработке: {0}", managers.Count.ToString());
        }
        catch (Exception ex)
        {
          Logger.Error("При получении данных из таблицы Person возникла ошибка", ex);
          isError = true;
        }
        #endregion

      }
      catch (Exception ex)
      {
        Logger.Error("При подключении к БД ССПД возникла ошибка", ex);
        isError = true;
      }
      finally
      {
        connection.Close();
      }

      // Обработка принятых данных.
      string activeText = string.Empty;
      if (!isError)
      {
        bool isUnitsError = false;
        activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDBalanceUnitsCountHeaderFormat(balanceUnits.Count.ToString()));
        string businessUnitsResult = UpdateBusinessUnits(balanceUnits, ourFirmGuids, out isUnitsError);
        isError = isError || isUnitsError;
        activeText = string.IsNullOrEmpty(businessUnitsResult) ? string.Empty : Functions.Module.NoticeTextAddLine(activeText, businessUnitsResult);

        int departmentsCount = departments.Count;
        if (departmentsCount > 0)
        {
          bool isDepartmentsError = false;
          activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDDepartmentsCountHeaderFormat(departmentsCount.ToString()));
          activeText = Functions.Module.NoticeTextAddLine(activeText, UpdateDepartments(departments, ourFirmGuids, out isDepartmentsError));
          isError = isError || isDepartmentsError;
        }
        
        int personsCount = persons.Count;
        if (personsCount > 0)
        {
          bool isPersonsError = false;
          activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDEmployeesCountHeaderFormat(personsCount.ToString()));
          activeText = Functions.Module.NoticeTextAddLine(activeText, UpdateEmployees(persons, ourFirmGuids, out isPersonsError));
          isError = isError || isPersonsError;
        }

        bool isManagersError = false;
        string managersResult = UpdateManagers(managers, departments, ourFirmGuids, out isManagersError);
        isError = isError || isManagersError;
        if (!string.IsNullOrEmpty(managersResult))
          activeText = Functions.Module.NoticeTextAddLine(activeText, managersResult);
      }
      
      // Фомирование уведомления администратору.
      if (!string.IsNullOrEmpty(activeText))
        Functions.Module.SendImportResultsNotice(Resources.SSPDImportResultNoticeSubjectFormat(Calendar.Now.ToString("g")), activeText);
      else if (isError)
        Functions.Module.SendImportResultsNotice(Resources.SSPDImportResultNoticeSubjectFormat(Calendar.Now.ToString("g")), Resources.SSPDNegativeDBRead);
      
      // Если все удачно, то изменить дату последнего импорта.
      if (!isError)
        UpdateLastNotificationDate(paramKey, today);
    }

    #region Обработка импортированных данных из ССПД.

    /// <summary>
    /// Обновить наши организации.
    /// </summary>
    /// <param name="balanceUnits">Список структурированных наших организаций из ССПД.</param>
    /// <param name="ourFirmGuids">Список Guid наших организаций из настройки.</param>
    /// <param name="isError">Признак наличия ошибки при сохранении.</param>
    /// <returns>Сообщения для уведомления администратору.</returns>
    public static string UpdateBusinessUnits(List<Structures.Module.SSPDBalanceUnit> balanceUnits, List<Guid> ourFirmGuids, out bool isError)
    {
      isError = false;
      
      if (balanceUnits.Count == 0)
        return string.Empty;

      string activeText = string.Empty;
      
      // Получить GUID типа справочника Наши организации и Организации.
      string businessUnitTypeGuid = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(typeof(DirRX.Solution.IBusinessUnit)).NameGuid.ToString();
      string companyTypeGuid = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(typeof(DirRX.Solution.ICompany)).NameGuid.ToString();

      foreach (Structures.Module.SSPDBalanceUnit balanceUnit in balanceUnits)
      {
        // Если Guid принятой записи есть в списке наших организаций, обработать запись нашей организации, иначе запись организации.
        if (ourFirmGuids.Contains(balanceUnit.BalanceUnitGUID))
        {
          #region Обработка полученной нашей организации.
          var extLink = PublicFunctions.Module.GetExternalLink(businessUnitTypeGuid, balanceUnit.BalanceUnitGUID.ToString(), Constants.Module.SSPDSystemCode);

          DirRX.Solution.IBusinessUnit businessUnit = null;
          if (extLink != null && extLink.EntityId.HasValue)
            businessUnit = DirRX.Solution.BusinessUnits.Get(extLink.EntityId.Value);
          else
          {
            if (!balanceUnit.isBlocked)
              businessUnit = DirRX.Solution.BusinessUnits.Create();
          }

          if (businessUnit == null)
          {
            Logger.DebugFormat("Запись нашей организации закрыта и не требует занесения {0} (GUID:{1})", balanceUnit.BalanceUnitName, balanceUnit.BalanceUnitGUID);
            continue;
          }
          
          var businessUnitStatus = balanceUnit.isBlocked ? DirRX.Solution.BusinessUnit.Status.Closed : DirRX.Solution.BusinessUnit.Status.Active;
          if (businessUnit.Name != balanceUnit.BalanceUnitName ||
              businessUnit.KSSSBalanceUnitId != balanceUnit.KSSSBalanceUnitID ||
              businessUnit.KSSSContragentId != balanceUnit.KSSSContragentID ||
              businessUnit.Status != businessUnitStatus)
          {
            bool isTransactionError = false;
            Transactions.Execute(
              () =>
              {
                try
                {
                  businessUnit.Name = balanceUnit.BalanceUnitName;
                  businessUnit.KSSSBalanceUnitId = balanceUnit.KSSSBalanceUnitID;
                  businessUnit.KSSSContragentId = balanceUnit.KSSSContragentID;
                  businessUnit.Status = businessUnitStatus;
                  businessUnit.Save();

                  PublicFunctions.Module.SetExternalEntityLink(businessUnit.Id, businessUnitTypeGuid, balanceUnit.BalanceUnitGUID.ToString(), "BalanceUnit", Constants.Module.SSPDSystemCode);
                  Logger.DebugFormat("Запись нашей организации успешно обновлена {0} (ID:{1})", businessUnit.Name, businessUnit.Id);
                  activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDPositiveResultHeaderFormat("\t", Sungero.Core.Hyperlinks.Get(businessUnit)));
                }
                catch (Exception ex)
                {
                  isTransactionError = true;
                  Logger.ErrorFormat("При обновлении записи нашей организации {0} возникла ошибка (GUID:{1})", ex, balanceUnit.BalanceUnitName, balanceUnit.BalanceUnitGUID);
                  activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDNegativeResultHeaderFormat(balanceUnit.BalanceUnitName, balanceUnit.BalanceUnitGUID, ex.Message.Trim()));
                }
              });
            isError = isError || isTransactionError;
          }
          #endregion
        }
        else
        {
          #region Обработка полученной организации.
          var extLink = PublicFunctions.Module.GetExternalLink(companyTypeGuid, balanceUnit.BalanceUnitGUID.ToString(), Constants.Module.SSPDSystemCode);

          DirRX.Solution.ICompany company = null;
          if (extLink != null && extLink.EntityId.HasValue)
            company = DirRX.Solution.Companies.Get(extLink.EntityId.Value);
          else
          {
            if (!balanceUnit.isBlocked)
              company = DirRX.Solution.Companies.Create();
          }

          if (company == null)
          {
            Logger.DebugFormat("Запись организации закрыта и не требует занесения {0} (GUID:{1})", balanceUnit.BalanceUnitName, balanceUnit.BalanceUnitGUID);
            continue;
          }
          
          var companyStatus = balanceUnit.isBlocked ? DirRX.Solution.Company.Status.Closed : DirRX.Solution.Company.Status.Active;
          if (company.Name != balanceUnit.BalanceUnitName ||
              company.Status != companyStatus)
          {
            bool isTransactionError = false;
            Transactions.Execute(
              () =>
              {
                try
                {
                  company.Name = balanceUnit.BalanceUnitName;
                  company.Status = companyStatus;
                  company.Save();

                  PublicFunctions.Module.SetExternalEntityLink(company.Id, companyTypeGuid, balanceUnit.BalanceUnitGUID.ToString(), "BalanceUnit", Constants.Module.SSPDSystemCode);
                  Logger.DebugFormat("Запись организации успешно обновлена {0} (ID:{1})", company.Name, company.Id);
                  activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDPositiveResultHeaderFormat("\t", Sungero.Core.Hyperlinks.Get(company)));
                }
                catch (Exception ex)
                {
                  isTransactionError = true;
                  Logger.ErrorFormat("При обновлении записи организации {0} возникла ошибка (GUID:{1})", ex, balanceUnit.BalanceUnitName, balanceUnit.BalanceUnitGUID);
                  activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDNegativeResultHeaderFormat(balanceUnit.BalanceUnitName, balanceUnit.BalanceUnitGUID, ex.Message.Trim()));
                }
              });
            isError = isError || isTransactionError;
          }
          #endregion
        }
      }
      
      return activeText;
    }

    /// <summary>
    /// Обновить подразделения.
    /// </summary>
    /// <param name="departments">Список структурированных подразделений из ССПД.</param>
    /// <param name="ourFirmGuids">Список Guid наших организаций из настройки.</param>
    /// <param name="isError">Признак наличия ошибки при сохранении.</param>
    /// <returns>Сообщения для уведомления администратору.</returns>
    public static string UpdateDepartments(List<Structures.Module.SSPDOrgstructure> departments, List<Guid> ourFirmGuids, out bool isError)
    {
      isError = false;
      
      if (departments.Count == 0)
        return string.Empty;
      
      string activeText = string.Empty;

      // Получить GUID типа справочника Подразделения, Подразделения контрагентов.
      string departmentTypeGuid = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(typeof(DirRX.Solution.IDepartment)).NameGuid.ToString();
      string orgDepartmentTypeGuid = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(typeof(IDepartCompanies)).NameGuid.ToString();

      foreach (Structures.Module.SSPDOrgstructure department in departments)
      {
        // Если Guid принятой записи есть в списке наших организаций, обработать запись нашей организации, иначе запись организации.
        if (ourFirmGuids.Contains(department.BalanceUnitGUID))
        {
          #region Обработка полученного подразделения.
          var extLinkDepartment = PublicFunctions.Module.GetExternalLink(departmentTypeGuid, department.OrgstructureGUID.ToString(), Constants.Module.SSPDSystemCode);
          var departmentRX = (extLinkDepartment != null && extLinkDepartment.EntityId.HasValue) ? DirRX.Solution.Departments.Get(extLinkDepartment.EntityId.Value) : DirRX.Solution.Departments.Create();

          if (departmentRX == null)
          {
            Logger.ErrorFormat("Не найдена внешняя ссылка на подразделение с GUID: {0}", department.OrgstructureGUID.ToString());
            continue;
          }
          
          bool isTransactionError = false;
          Transactions.Execute(
            () =>
            {
              try
              {
                departmentRX.BusinessUnit = PublicFunctions.Module.GetBusinessUnitByExtGUID(department.BalanceUnitGUID.ToString());
                departmentRX.Name = department.DisplayName;
                departmentRX.OUID = department.OUID;
                departmentRX.Code = department.OUHR;
                
                // Проверка на изменение для исключения дублирования системных замещений (событие до сохранения в транзакции подразделения стандартной версии).
                var status = department.isBlocked ? DirRX.Solution.Department.Status.Closed : DirRX.Solution.Department.Status.Active;
                if (departmentRX.Status != status)
                  departmentRX.Status = status;
                
                departmentRX.Save();

                // Сохранить созданную запись для последующего вычисления головного подразделения.
                department.Department = departmentRX;

                // Записать внешнюю ссылку на созданную запись.
                PublicFunctions.Module.SetExternalEntityLink(departmentRX.Id, departmentTypeGuid, department.OrgstructureGUID.ToString(), "Orgstructure", Constants.Module.SSPDSystemCode);
                Logger.DebugFormat("Запись подразделения успешно обновлена {0} (ID:{1})", departmentRX.Name, departmentRX.Id);
                activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDPositiveResultHeaderFormat("\t", Sungero.Core.Hyperlinks.Get(departmentRX)));
              }
              catch (Exception ex)
              {
                isTransactionError = true;
                Logger.ErrorFormat("При обновлении записи подразделения {0} возникла ошибка (GUID:{1})", ex, department.DisplayName, department.OrgstructureGUID);
                activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDNegativeResultHeaderFormat(department.DisplayName, department.OrgstructureGUID, ex.Message.Trim()));
              }
            });
          isError = isError || isTransactionError;
          #endregion
        }
        else
        {
          #region Обработка полученного подразделения организации.
          var extLinkOrgDepartment = PublicFunctions.Module.GetExternalLink(orgDepartmentTypeGuid, department.OrgstructureGUID.ToString(), Constants.Module.SSPDSystemCode);
          var orgDepartmentRX = (extLinkOrgDepartment != null && extLinkOrgDepartment.EntityId.HasValue) ? DepartCompanieses.Get(extLinkOrgDepartment.EntityId.Value) : DepartCompanieses.Create();
          if (orgDepartmentRX == null)
          {
            Logger.ErrorFormat("Не найдена внешняя ссылка на подразделение организации с GUID: {0}", department.OrgstructureGUID.ToString());
            continue;
          }
          
          bool isTransactionError = false;
          Transactions.Execute(
            () =>
            {
              try
              {
                orgDepartmentRX.Counterparty = PublicFunctions.Module.GetOrganizationByExtGUID(department.BalanceUnitGUID.ToString());
                orgDepartmentRX.Name = department.DisplayName;
                orgDepartmentRX.OUID = department.OUID;
                orgDepartmentRX.Code = department.OUHR;
                orgDepartmentRX.Status = department.isBlocked ? IntegrationLLK.DepartCompanies.Status.Closed : IntegrationLLK.DepartCompanies.Status.Active;
                orgDepartmentRX.Save();
                // Сохранить созданную запись для последующего вычисления головного подразделения.
                department.CompanyDepartment = orgDepartmentRX;
                // Записать внешнюю ссылку на созданную запись.
                PublicFunctions.Module.SetExternalEntityLink(orgDepartmentRX.Id, orgDepartmentTypeGuid, department.OrgstructureGUID.ToString(), "Orgstructure", Constants.Module.SSPDSystemCode);
                Logger.DebugFormat("Запись подразделения организации успешно обновлена {0} (ID:{1})", orgDepartmentRX.Name, orgDepartmentRX.Id);
                activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDPositiveResultHeaderFormat("\t", Sungero.Core.Hyperlinks.Get(orgDepartmentRX)));
              }
              catch (Exception ex)
              {
                isTransactionError = true;
                Logger.ErrorFormat("При обновлении записи подразделения организации {0} возникла ошибка (GUID:{1})", ex, department.DisplayName, department.OrgstructureGUID);
                activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDNegativeResultHeaderFormat(department.DisplayName, department.OrgstructureGUID, ex.Message.Trim()));
              }
            });
          isError = isError || isTransactionError;
          #endregion
        }
      }

      #region Вычисление головных подразделений.
      departments = departments.Where(d => (d.Department != null || d.CompanyDepartment != null) && !string.IsNullOrEmpty(d.POUID)).ToList();
      foreach (Structures.Module.SSPDOrgstructure department in departments)
      {
        // Если Guid принятой записи есть в списке наших организаций, обработать запись подразделения, иначе запись подразделения контрагента.
        if (ourFirmGuids.Contains(department.BalanceUnitGUID))
        {
          // Обработка подразделения.
          var departmentRX = department.Department;
          var headDepartmentRX = DirRX.Solution.Departments.GetAll(d => d.OUID.Trim().ToUpper() == department.POUID.Trim().ToUpper()).FirstOrDefault();
          if (headDepartmentRX != null)
          {
            bool isTransactionError = false;
            Transactions.Execute(
              () =>
              {
                try
                {
                  departmentRX.HeadOffice = headDepartmentRX;
                  departmentRX.Save();

                  PublicFunctions.Module.SetExternalEntityLink(departmentRX.Id, departmentTypeGuid, department.OrgstructureGUID.ToString(), "Orgstructure", Constants.Module.SSPDSystemCode);
                  Logger.DebugFormat("Для подразделения успешно обновлено головное подразделение {0} (ID:{1})", departmentRX.Name, departmentRX.Id);
                }
                catch (Exception ex)
                {
                  isTransactionError = true;
                  Logger.ErrorFormat("При обновлении записи подразделения {0} возникла ошибка (GUID:{1})", ex, department.DisplayName, department.OrgstructureGUID);
                  activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDNegativeResultHeaderFormat(department.DisplayName, department.OrgstructureGUID, ex.Message.Trim()));
                }
              });
            isError = isError || isTransactionError;
          }
        }
        else
        {
          // Обработка подразделения организации.
          var orgDepartmentRX = department.CompanyDepartment;
          var headDepartmentRX = DepartCompanieses.GetAll(d => d.OUID.Trim().ToUpper() == department.POUID.Trim().ToUpper()).FirstOrDefault();
          if (headDepartmentRX != null)
          {
            bool isTransactionError = false;
            Transactions.Execute(
              () =>
              {
                try
                {
                  orgDepartmentRX.HeadOffice = headDepartmentRX;
                  orgDepartmentRX.Save();
                  
                  PublicFunctions.Module.SetExternalEntityLink(orgDepartmentRX.Id, orgDepartmentTypeGuid, department.OrgstructureGUID.ToString(), "Orgstructure", Constants.Module.SSPDSystemCode);
                  Logger.DebugFormat("Для подразделения успешно обновлено головное подразделение {0} (ID:{1})", orgDepartmentRX.Name, orgDepartmentRX.Id);
                }
                catch (Exception ex)
                {
                  isTransactionError = true;
                  Logger.ErrorFormat("При обновлении записи подразделения {0} возникла ошибка (GUID:{1})", ex, department.DisplayName, department.OrgstructureGUID);
                  activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDNegativeResultHeaderFormat(department.DisplayName, department.OrgstructureGUID, ex.Message.Trim()));
                }
              });
            isError = isError || isTransactionError;
          }
        }
      }
      #endregion
      
      return activeText;
    }

    /// <summary>
    /// Обновить сотрудников.
    /// </summary>
    /// <param name="balanceUnits">Список структурированных сотрудников из ССПД.</param>
    /// <param name="ourFirmGuids">Список Guid наших организаций из настройки.</param>
    /// <param name="isError">Признак наличия ошибки при сохранении.</param>
    /// <returns>Сообщения для уведомления администратору.</returns>
    public static string UpdateEmployees(List<Structures.Module.SSPDPerson> persons, List<Guid> ourFirmGuids, out bool isError)
    {
      isError = false;
      
      if (persons.Count == 0)
        return string.Empty;
      
      string activeText = string.Empty;
      
      // Получить GUID типа справочника Сотрудники, Персоны.
      string employeeTypeGuid = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(typeof(DirRX.Solution.IEmployee)).NameGuid.ToString();
      string contactTypeGuid = Sungero.Metadata.Services.MetadataSearcher.FindEntityMetadata(typeof(DirRX.Solution.IContact)).NameGuid.ToString();
      
      // Получить из конфигурации тип аутентификации.
      bool isWinAuth = PublicFunctions.Module.Remote.GetServerConfigValue("SSPD_WIN_AUTHENTICATION").ToUpper() == "TRUE";

      foreach (Structures.Module.SSPDPerson person in persons)
      {
        // Если Guid принятой записи есть в списке наших организаций, обработать запись нашей организации, иначе запись организации.
        if (ourFirmGuids.Contains(person.BalanceUnitGUID))
        {
          #region Обработка полученного сотрудника.
          var extLinkEmployee = PublicFunctions.Module.GetExternalLink(employeeTypeGuid, person.PersonGUID.ToString(), Constants.Module.SSPDSystemCode);
          var employee = (extLinkEmployee != null && extLinkEmployee.EntityId.HasValue) ? DirRX.Solution.Employees.Get(extLinkEmployee.EntityId.Value) : DirRX.Solution.Employees.Create();

          if (employee == null)
          {
            Logger.ErrorFormat("Не найдена внешняя ссылка на сотрудника с GUID: {0}", person.PersonGUID.ToString());
            continue;
          }

          bool isTransactionError = false;
          Transactions.Execute(
            () =>
            {
              try
              {
                string message = string.Empty;
                
                // Вычисление персоны.
                var personRX = employee.Person;
                if (personRX == null)
                  personRX = Sungero.Parties.People.Create();
                personRX.LastName = person.SurName;
                personRX.FirstName = person.GivenName;
                personRX.MiddleName = person.MiddleName;
                personRX.Save();
                employee.Person = personRX;
                
                // Вычисление учетной записи.
                if (!string.IsNullOrEmpty(person.PersonLogin))
                {
                  Sungero.CoreEntities.ILogin loginRX;
                  if (employee.Login != null)
                    loginRX = employee.Login;
                  else
                  {
                    // Если у сотрудника не указана учетная запись, поискать учетную запись с принятым логином.
                    loginRX = Sungero.CoreEntities.Logins.GetAll(l => l.LoginName.ToUpper() == person.PersonLogin.ToUpper()).FirstOrDefault();
                    if (loginRX == null)
                    {
                      loginRX = Sungero.CoreEntities.Logins.Create();
                      
                      if (isWinAuth)
                        loginRX.TypeAuthentication = Sungero.CoreEntities.Login.TypeAuthentication.Windows;
                      else
                      {
                        loginRX.TypeAuthentication = Sungero.CoreEntities.Login.TypeAuthentication.Password;
                        loginRX.NeedChangePassword = true;
                      }
                    }
                  }
                  loginRX.LoginName = person.PersonLogin;
                  loginRX.Save();
                  employee.Login = loginRX;
                }

                // Вычисление должности.
                var positionRX = Sungero.Company.JobTitles.GetAll(t => t.Name.ToUpper() == person.Position.ToUpper()).FirstOrDefault();
                if (positionRX == null)
                {
                  positionRX = Sungero.Company.JobTitles.Create();
                  positionRX.Name = person.Position;
                  positionRX.Save();
                }

                var departmentRX = PublicFunctions.Module.GetDepartmentByExtGUID(person.OrgstructureGUID.ToString());
                if (departmentRX == null)
                  Logger.ErrorFormat("Не найдена внешняя ссылка на подразделение с GUID: {0}", person.OrgstructureGUID.ToString());

                employee.Department = departmentRX;
                employee.JobTitle = positionRX;
                employee.BusinessAddress = person.OfficeAddress;
                employee.Phone = person.OfficePhones;
                employee.PersonnelNumber = person.PersonalNumber;
                employee.Email = person.Mail;
                if (string.IsNullOrEmpty(employee.Email))
                {
                  employee.NeedNotifyExpiredAssignments = false;
                  employee.NeedNotifyNewAssignments = false;
                  message = ", " + Resources.SSPDEmployeeEmptyMailText;
                }
                
                employee.Status = person.isBlocked ? DirRX.Solution.Employee.Status.Closed : DirRX.Solution.Employee.Status.Active;
                employee.Save();

                // Сохранить созданную запись для последующего вычисления руководителей.
                person.Employee = employee;

                // Записать внешнюю ссылку на созданную запись.
                PublicFunctions.Module.SetExternalEntityLink(employee.Id, employeeTypeGuid, person.PersonGUID.ToString(), "Person", Constants.Module.SSPDSystemCode);
                Logger.DebugFormat("Запись сотрудника успешно обновлена {0} (ID:{1})", employee.Name, employee.Id);
                activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDPositiveResultHeaderFormat(Sungero.Core.Hyperlinks.Get(employee), message));
              }
              catch (Exception ex)
              {
                isTransactionError = true;
                Logger.ErrorFormat("При обновлении записи сотрудника {0} возникла ошибка (GUID:{1})", ex, person.FullName, person.PersonGUID);
                activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDNegativeResultHeaderFormat(person.FullName, person.PersonGUID, ex.Message.Trim()));
              }
            });
          isError = isError || isTransactionError;
          #endregion
        }
        else
        {
          #region Обработка полученного контакта.
          var extLink = PublicFunctions.Module.GetExternalLink(contactTypeGuid, person.PersonGUID.ToString(), Constants.Module.SSPDSystemCode);
          var contact = (extLink != null && extLink.EntityId.HasValue) ? DirRX.Solution.Contacts.Get(extLink.EntityId.Value) : DirRX.Solution.Contacts.Create();

          if (contact == null)
          {
            Logger.ErrorFormat("Не найдена внешняя ссылка на контакта с GUID: {0}", person.PersonGUID.ToString());
            continue;
          }

          bool isTransactionError = false;
          Transactions.Execute(
            () =>
            {
              try
              {
                var personRX = contact.Person == null ? Sungero.Parties.People.Create() : Sungero.Parties.People.GetAll(p => Sungero.Parties.People.Equals(p, contact.Person)).FirstOrDefault();
                personRX.LastName = person.SurName;
                personRX.FirstName = person.GivenName;
                personRX.MiddleName = person.MiddleName;
                personRX.Save();
                contact.Person = personRX;

                contact.Phone = person.OfficePhones;
                contact.Email = person.Mail;
                contact.JobTitle = person.Position;
                contact.PersonnelNumber = person.PersonalNumber;
                contact.Company = PublicFunctions.Module.GetOrganizationByExtGUID(person.BalanceUnitGUID.ToString());
                contact.Subdivision = PublicFunctions.Module.GetOrgDepartmentByExtGUID(person.OrgstructureGUID.ToString());
                contact.Login = person.PersonLogin;
                contact.Status = person.isBlocked ? Sungero.Parties.Contact.Status.Closed : Sungero.Parties.Contact.Status.Active;
                contact.Save();

                // Сохранить созданную запись для последующего вычисления руководителей.
                person.Contact = contact;

                // Записать внешнюю ссылку на созданную запись.
                PublicFunctions.Module.SetExternalEntityLink(contact.Id, contactTypeGuid, person.PersonGUID.ToString(), "Person", Constants.Module.SSPDSystemCode);
                Logger.DebugFormat("Запись контакта успешно обновлена {0} (ID:{1})", contact.Name, contact.Id);
                activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDPositiveResultHeaderFormat("\t", Sungero.Core.Hyperlinks.Get(contact)));
              }
              catch (Exception ex)
              {
                isTransactionError = true;
                Logger.ErrorFormat("При обновлении записи контакта {0} возникла ошибка (GUID:{1})", ex, person.FullName, person.PersonGUID);
                activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDNegativeResultHeaderFormat(person.FullName, person.PersonGUID, ex.Message.Trim()));
              }
            });
          isError = isError || isTransactionError;
          #endregion
        }
      }
      
      return activeText;
    }
    
    /// <summary>
    /// Обновить руководителей подразделений.
    /// </summary>
    /// <param name="managers">Список структурированных руководителей из ССПД.</param>
    /// <param name="departments">Список структурированных подразделений из ССПД.</param>
    /// <param name="ourFirmGuids">Список Guid наших организаций из настройки.</param>
    /// <param name="isError">Признак наличия ошибки при сохранении.</param>
    /// <returns>Сообщения для уведомления администратору.</returns>
    public static string UpdateManagers(List<Structures.Module.SSPDManager> managers, List<Structures.Module.SSPDOrgstructure> departments, List<Guid> ourFirmGuids, out bool isError)
    {
      isError = false;
      
      if (managers.Count == 0)
        return string.Empty;
      
      string activeText = string.Empty;

      #region Обработка подразделений с несколькими руководителями.
      var doubleManagerDepartments = managers.GroupBy(m => m.OrgstructureGUID).Where(d => d.Count() > 1).Select(m => m.Key);
      foreach (Guid departmentGuid in doubleManagerDepartments)
      {
        var department = PublicFunctions.Module.GetDepartmentByExtGUID(departmentGuid.ToString());
        if (department != null)
        {
          Logger.DebugFormat("Для подразделения (GUID: {0}) указаны несколько руководителей.", departmentGuid);
          if (department.Manager == null)
            activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDUpdateManagerDoublesTextFormat(Sungero.Core.Hyperlinks.Get(department)));
        }
        else
          Logger.ErrorFormat("Не найдена внешняя ссылка на подразделение с GUID: {0}", departmentGuid.ToString());
      }
      #endregion
      
      #region Обработка подразделений с единственными руководителями.
      var singleManagers = managers.Where(m => !doubleManagerDepartments.Contains(m.OrgstructureGUID));
      foreach (Structures.Module.SSPDManager manager in singleManagers)
      {
        // Если Guid принятой записи есть в списке наших организаций, обработать запись сотрудника, иначе запись контакта.
        if (ourFirmGuids.Contains(manager.BalanceUnitGUID))
        {
          #region Обработка полученного сотрудника.
          var department = PublicFunctions.Module.GetDepartmentByExtGUID(manager.OrgstructureGUID.ToString());
          if (department == null)
            Logger.ErrorFormat("Не найдена внешняя ссылка на подразделение с GUID: {0}", manager.OrgstructureGUID.ToString());
          else
          {
            bool isTransactionError = false;
            Transactions.Execute(
              () =>
              {
                try
                {
                  var managerRX = DirRX.Solution.Employees.As(department.Manager);
                  string managerPersonalNumber = managerRX == null ? string.Empty : managerRX.PersonnelNumber.Trim().ToUpper();
                  if (managerPersonalNumber != manager.PersonalNumber.Trim().ToUpper())
                  {
                    var employee = PublicFunctions.Module.GetEmployeeByExtGUID(manager.PersonGUID.ToString());
                    if (employee != null)
                    {
                      department.Manager = employee;
                      department.Save();
                      Logger.DebugFormat("В подразделении '{0}' обновлен руководитель: {1}", department.DisplayValue, employee.Name);
                      activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDUpdateManagerPositiveTextFormat(Sungero.Core.Hyperlinks.Get(department), Sungero.Core.Hyperlinks.Get(employee)));
                    }
                    else
                      Logger.ErrorFormat("Не найдена внешняя ссылка на сотрудника с GUID: {0}", manager.OrgstructureGUID.ToString());
                  }
                }
                catch (Exception ex)
                {
                  isTransactionError = true;
                  Logger.ErrorFormat("При обновлении руководителя подразделения возникла ошибка (GUID руководителя:{0})", ex, manager.PersonGUID);
                  activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDUpdateManagerNegativeTextFormat(manager.FullName, manager.PersonGUID, ex.Message));
                }
              });
            isError = isError || isTransactionError;
          }
          #endregion
        }
        else
        {
          #region Обработка полученного контакта.
          var department = PublicFunctions.Module.GetOrgDepartmentByExtGUID(manager.OrgstructureGUID.ToString());
          if (department == null)
            Logger.ErrorFormat("Не найдена внешняя ссылка на подразделение организации с GUID: {0}", manager.OrgstructureGUID.ToString());
          else
          {
            bool isTransactionError = false;
            Transactions.Execute(
              () =>
              {
                try
                {
                  var managerRX = DirRX.Solution.Contacts.As(department.Manager);
                  string managerPersonalNumber = managerRX == null ? string.Empty : managerRX.PersonnelNumber.Trim().ToUpper();
                  if (managerPersonalNumber != manager.PersonalNumber.Trim().ToUpper())
                  {
                    var contact = PublicFunctions.Module.GetContactByExtGUID(manager.PersonGUID.ToString());
                    if (contact != null)
                    {
                      department.Manager = contact;
                      department.Save();
                      Logger.DebugFormat("В подразделении организации '{0}' обновлен руководитель: {1}", department.DisplayValue, contact.Name);
                      activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDUpdateManagerPositiveTextFormat(Sungero.Core.Hyperlinks.Get(department), Sungero.Core.Hyperlinks.Get(contact)));
                    }
                    else
                      Logger.ErrorFormat("Не найдена внешняя ссылка на контакт с GUID: {0}", manager.PersonGUID.ToString());
                  }
                }
                catch (Exception ex)
                {
                  isTransactionError = true;
                  Logger.ErrorFormat("При обновлении руководителя подразделения организации возникла ошибка (GUID руководителя:{0})", ex, manager.PersonGUID);
                  activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDUpdateManagerNegativeTextFormat(manager.FullName, manager.PersonGUID, ex.Message));
                }
              });
            isError = isError || isTransactionError;
          }
          #endregion
        }
      }
      # endregion
      
      #region Обработка подразделений без руководителей.
      var gettedOurDepartmentsOUID = departments.Where(d => !string.IsNullOrEmpty(d.OUID) && ourFirmGuids.Contains(d.BalanceUnitGUID)).Select(o => o.OUID).ToList();
      var gettedOrgDepartmentsOUID = departments.Where(d => !string.IsNullOrEmpty(d.OUID) && !ourFirmGuids.Contains(d.BalanceUnitGUID)).Select(o => o.OUID).ToList();

      // Подразделения нашей организации.
      var noManagerDepartments = DirRX.Solution.Departments.GetAll(d => gettedOurDepartmentsOUID.Contains(d.OUID) && d.Manager == null).ToList();
      foreach (var department in noManagerDepartments)
        activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDUpdateDeptNoManagerTextFormat(Sungero.Core.Hyperlinks.Get(department)));
      
      // Подразделения организаций.
      if (gettedOrgDepartmentsOUID.Any())
        activeText = Functions.Module.NoticeTextAddLine(activeText, Resources.SSPDUpdateOrgDeptNoManagerText);
      # endregion
      
      return activeText;
    }
    
    #endregion
    
    #region Получить строку из значения SqlDataReader.
    public static string SafeGetString(SqlDataReader reader, int colIndex)
    {
      if(!reader.IsDBNull(colIndex))
        return reader.GetString(colIndex);
      return string.Empty;
    }
    #endregion
    #region Получить целое число из значения SqlDataReader.
    public static int? SafeGetInt(SqlDataReader reader, int colIndex)
    {
      if(!reader.IsDBNull(colIndex))
        return reader.GetInt32(colIndex);
      return null;
    }
    #endregion
    
    #region Запись и считывание дат последнего выполнения фоновых процессов.
    
    /// <summary>
    /// Получить дату последнего выполнения фонового процесса.
    /// </summary>
    /// <param name="key">Имя ключа в таблице параметров.</param>
    /// <returns>Дата последнего выполнения.</returns>
    public static DateTime? GetLastExecutionProccessDateTime(string key)
    {
      var command = string.Format(Queries.Module.SelectDocflowParamsValue, key);
      try
      {
        var executionResult = Sungero.Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
        var date = string.Empty;
        if ((executionResult is DBNull) || executionResult == null)
          return null;

        date = executionResult.ToString();
        Logger.DebugFormat("Время последнего выполнения данного фонового процесса записанное в БД: {0} (UTC)", date);

        return Calendar.FromUtcTime(DateTime.Parse(date, null, System.Globalization.DateTimeStyles.AdjustToUniversal));
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("При получении времени последнего выполнения фонового процесса с ключом {0} возникла ошибка", ex, key);
        return null;
      }
    }

    /// <summary>
    /// Обновить дату последней рассылки уведомлений.
    /// </summary>
    /// <param name="key">Имя ключа в таблице параметров.</param>
    /// <param name="notificationDate">Дата рассылки уведомлений.</param>
    public static void UpdateLastNotificationDate(string key, DateTime notificationDate)
    {
      var newDate = notificationDate.Add(-Calendar.UtcOffset).ToString("yyyy-MM-ddTHH:mm:ss.ffff+0");
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.Module.InsertOrUpdateDocflowParamsValue, new[] { key, newDate });
      Logger.DebugFormat("Last notification by assignment date is set to {0} (UTC)", newDate);
    }
    
    #endregion
  }
}