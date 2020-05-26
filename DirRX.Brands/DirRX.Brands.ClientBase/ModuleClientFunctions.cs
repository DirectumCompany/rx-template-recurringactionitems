using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Brands.Client
{
  public class ModuleFunctions
  {

    /// <summary>
    /// Загрузить данные о регистрации товарных знаков из xlsx файла.
    /// </summary>
    public virtual void BrandRegistrImport()
    {
      if (!Sungero.CoreEntities.Users.Current.IncludedIn(Constants.Module.IntellectualPropertySpecialistRoleGuid) &&
          !Sungero.CoreEntities.Users.Current.IncludedIn(Roles.Administrators))
      {
        Dialogs.ShowMessage(DirRX.Brands.Resources.AccessErrorName,
                            DirRX.Brands.Resources.BrandImportAccessError, MessageType.Information);
        return;
      }
      
      var dialog = Dialogs.CreateInputDialog(Resources.BrandsImportDialogTitle);
      var fileSelector = dialog.AddFileSelect(Resources.BrandsImportFileSelectTitle, true).WithFilter("Файлы xlsx", ".xlsx", new string[] {});
      var importButton = dialog.Buttons.AddCustom(Resources.BrandsImportButtonTitle);
      dialog.Buttons.AddCancel();
      dialog.SetOnButtonClick(b =>
                              {
                                if (b.Button == importButton && b.IsValid)
                                {
                                  try
                                  {
                                    // Получить данные из xlsx файла.
                                    var content = fileSelector.Value.Content;
                                    var fileContent = Sungero.Docflow.Structures.Module.ByteArray.Create(content);
                                    var registrList = Functions.Module.GetBrandRegistrDataFromExcel(fileContent.Bytes);
                                    if (registrList.Any())
                                    {
                                      // Проверить полученное содержимое и сформировать список ошибок.
                                      var checkingErrors = Functions.Module.Remote.CheckAndCreateBrandRegistrDataFromExcel(registrList);

                                      var errorRows = checkingErrors.Select(r => r.Row);
                                      int successCount = registrList.FindAll(r => !errorRows.Contains(r.Row)).Count;
                                      Dialogs.NotifyMessage(Resources.BrandsImportSuccessfulFormat(successCount.ToString(), registrList.Count.ToString()));
                                    }
                                  }
                                  catch (System.IO.IOException ex)
                                  {
                                    b.AddError(Resources.BrandsImportErrorSubjectFormat(ex.Message), fileSelector);
                                  }
                                  catch (Exception ex)
                                  {
                                    b.AddError(Resources.BrandsImportErrorSubjectFormat(ex.Message), fileSelector);
                                    Logger.Error(string.Empty, ex);
                                  }
                                }
                              });
      dialog.Show();
    }

    /// <summary>
    /// Получить данные из файла Excel, содержащие структуры регистраций товарных знаков.
    /// </summary>
    /// <param name="contentFile">Массив байт с содержимым xlsx файла.</param>
    /// <returns>Список структур регистраций товарных знаков.</returns>
    public List<Brands.Structures.Module.BrandRegistrData> GetBrandRegistrDataFromExcel(byte[] contentFile)
    {
      List<Structures.Module.BrandRegistrData> registrList = new List<Structures.Module.BrandRegistrData>();
      
      using (SpreadsheetDocument doc = SpreadsheetDocument.Open(new MemoryStream(contentFile), false))
      {
        WorkbookPart workbookPart = doc.WorkbookPart;
        Worksheet sheet = workbookPart.WorksheetParts.First().Worksheet;
        SharedStringTable sst = workbookPart.GetPartsOfType<SharedStringTablePart>().First().SharedStringTable;
        var rows = sheet.Descendants<Row>();
        
        // Вычислить лист и строки с содержимым.
        try
        {
          rows = workbookPart.WorksheetParts
            .FirstOrDefault(p => p.Worksheet.Descendants<Row>().FirstOrDefault() != null)
            .Worksheet.Descendants<Row>();
        }
        catch
        {
          throw new Exception(Resources.BrandsImportIncorrectSheetNotFound);
        }
        
        if (rows == null)
          throw new Exception(Resources.BrandsImportIncorrectSheetNotFound);
        
        // Получить буквенные обозначения колонок по первой строке.
        int columnCount = 0;
        var headRow = rows.FirstOrDefault();
        string[] cellCoordinates = new string[12];
        foreach (Cell c in headRow.Elements<Cell>())
        {
          string cellName = c.CellReference.Value;
          string columnName = cellName.Replace("1", string.Empty);
          string cellValue = GetCellValue(headRow, cellName, sst).Trim().ToUpper();
          switch (cellValue)
          {
            case "ИД":
              cellCoordinates[0] = columnName;
              columnCount++;
              break;
            case "ТОВАРНЫЙ ЗНАК":
              cellCoordinates[1] = columnName;
              columnCount++;
              break;
            case "№ РЕГИСТРАЦИИ":
              cellCoordinates[2] = columnName;
              columnCount++;
              break;
            case "ВИД РЕГИСТРАЦИИ":
              cellCoordinates[3] = columnName;
              columnCount++;
              break;
            case "СТРАНА":
              cellCoordinates[4] = columnName;
              columnCount++;
              break;
            case "КЛАСС МКТУ":
              cellCoordinates[5] = columnName;
              columnCount++;
              break;
            case "ТОВАРНАЯ ГРУППА":
              cellCoordinates[6] = columnName;
              columnCount++;
              break;
            case "ДАТА РЕГИСТРАЦИИ":
              cellCoordinates[7] = columnName;
              columnCount++;
              break;
            case "ДЕЙСТВУЕТ ДО":
              cellCoordinates[8] = columnName;
              columnCount++;
              break;
            case "СТАТУС":
              cellCoordinates[9] = columnName;
              columnCount++;
              break;
            case "ОСПАРИВАНИЕ":
              cellCoordinates[10] = columnName;
              columnCount++;
              break;
            case "ПРИМЕЧАНИЕ":
              cellCoordinates[11] = columnName;
              columnCount++;
              break;
          }
        }
        
        if (columnCount != 12)
          throw new Exception(Resources.BrandsImportIncorrectColumnCount);
        
        #region Обработка принятого XLSX.
        int rowIndex = 2;
        // Пропускаем заголовки таблицы.
        foreach (Row row in rows.Skip(1))
        {
          try
          {
            // Пропустить, если строка без содержимого.
            if (string.IsNullOrEmpty(row.InnerText))
              continue;
            
            var item = Structures.Module.BrandRegistrData.Create();
            item.Row = rowIndex;

            int id = 0;
            if (int.TryParse(GetCellValue(row, cellCoordinates[0] + rowIndex.ToString(), sst), out id))
              item.BrandID = id;
            item.BrandName = GetCellValue(row, cellCoordinates[1] + rowIndex.ToString(), sst);
            item.RegistrNum = GetCellValue(row, cellCoordinates[2] + rowIndex.ToString(), sst);
            item.RegistrKind = GetCellValue(row, cellCoordinates[3] + rowIndex.ToString(), sst).ToUpper();
            item.CountryName = GetCellValue(row, cellCoordinates[4] + rowIndex.ToString(), sst);
            item.ICGSClass = GetCellValue(row, cellCoordinates[5] + rowIndex.ToString(), sst);
            item.ProductGroupName = GetCellValue(row, cellCoordinates[6] + rowIndex.ToString(), sst);
            
            string startDateValue = GetCellValue(row, cellCoordinates[7] + rowIndex.ToString(), sst);
            if (!string.IsNullOrEmpty(startDateValue))
            {
              double startOADate;
              if (double.TryParse(startDateValue, out startOADate))
                item.RegistrDate = DateTime.FromOADate(startOADate);
              else
                item.RegistrDate = DateTime.ParseExact(startDateValue, "dd.MM.yyyy", new CultureInfo("ru-RU", true));
            }
            else
              item.RegistrDate = null;
            
            string endDateValue = GetCellValue(row, cellCoordinates[8] + rowIndex.ToString(), sst);
            if (!string.IsNullOrEmpty(endDateValue))
            {
              double endOADate;
              if (double.TryParse(endDateValue, out endOADate))
                item.ValidTill = DateTime.FromOADate(endOADate);
              else
                item.ValidTill = DateTime.ParseExact(endDateValue, "dd.MM.yyyy", new CultureInfo("ru-RU", true));
            }
            else
              item.ValidTill = null;
            
            item.RegistrStatus = GetCellValue(row, cellCoordinates[9] + rowIndex.ToString(), sst).ToUpper();
            item.IsAppeal = GetCellValue(row, cellCoordinates[10] + rowIndex.ToString(), sst) == "+";
            item.Note = GetCellValue(row, cellCoordinates[11] + rowIndex.ToString(), sst);

            registrList.Add(item);
          }
          catch (Exception ex)
          {
            Logger.Error(string.Empty, ex);
            throw new Exception(string.Format("При обработке строки №{0} возникла ошибка: {1}", rowIndex, ex.Message));
          }
          
          rowIndex++;
        }
        
        #endregion
      }
      
      return registrList;
    }
    
    #region Работа с OpenXml.
    
    /// <summary>
    /// Получить значение ячейки xlsx файла.
    /// </summary>
    /// <param name="row">OpenXml строка xlsx файла.</param>
    /// <param name="cellCoordinates">Позиция ячейки (имя ячейки в excel).</param>
    /// <param name="stringTable">OpenXml таблица строковых значений.</param>
    /// <returns>Значение ячейки.</returns>
    private string GetCellValue(DocumentFormat.OpenXml.Spreadsheet.Row row, string cellCoordinates, DocumentFormat.OpenXml.Spreadsheet.SharedStringTable stringTable)
    {
      //var cell = row.Elements<Cell>()..ElementAt(index); // int index
      var cell = row.Elements<Cell>().FirstOrDefault(c => cellCoordinates.Equals(c.CellReference.Value));
      if (cell != null && cell.CellValue != null)
      {
        if (cell.DataType == null)
          return cell.CellValue.InnerText.Trim();
        else
          return stringTable.ChildElements[int.Parse(cell.CellValue.Text)].InnerText.Trim();
      }
      
      return string.Empty;
    }
    #endregion
  }
}