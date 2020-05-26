using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Domain.Shared;

namespace DirRX.IntegrationLLK.Client
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Получает XDocument из xml файла.
    /// </summary>
    /// <param name="path">Путь к файлу.</param>
    /// <returns>XElement документ.</returns>
    private static XDocument GetXDocumentFromFile(string path)
    {
      if (!File.Exists(path)) throw new FileNotFoundException(string.Format("Не найден файл {0}", path));
      return XDocument.Load(path);
    }
    
    /// <summary>
    /// Обрабатывает пакеты службы ввода DCTS.
    /// </summary>
    /// <param name="lineSender">Наименование линии.</param>
    /// <param name="instanceInfos">Путь к xml файлу DCTS c информацией об экземплярах захвата и о захваченных файлах.</param>
    /// <param name="deviceInfo">Путь к xml файлу DCTS c информацией об устройствах ввода.</param>
    /// <param name="inputFiles">Путь к xml файлу DCTS c информацией об отправляемых в конечную систему файлах.</param>
    /// <param name="folder">Путь к папке хранения файлов, переданных в пакете.</param>
    public static void ProcessingDCTS(string lineSender, string instanceInfos, string deviceInfo, string inputFiles, string folder)
    {
      var instanceInfosXDoc = GetXDocumentFromFile(instanceInfos);
      var inputFilesXDoc = GetXDocumentFromFile(inputFiles);
      var deviceInfoXDoc = GetXDocumentFromFile(deviceInfo);
      var barcode = GetBarcodeFromXml(instanceInfosXDoc);
      var id = barcode != string.Empty ? Convert.ToInt32(barcode) : 0;
      // поиск штрихкода в документе.
      var doc = Functions.Module.Remote.LocateDocumentById(id);
      ProcessEntity(inputFilesXDoc.ToString(), instanceInfosXDoc.ToString(), folder, doc, lineSender);
    }
    
    /// <summary>
    /// Получить значение из xml-файла по имени элемента.
    /// </summary>
    /// <param name="elementName">Имя элемента.</param>
    /// <returns>Значение элемента.</returns>
    public static string GetBarcodeFromXml(System.Xml.Linq.XDocument file)
    {
      try
      {
        var barcode = file.Element("CaptureInstanceInfoList").Element("FileSystemCaptureInstanceInfo").Element("Files").Element("FileInfo").Element("Barcodes")
          .Value.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
        return barcode[barcode.Length - 1].Trim();
      }
      catch (Exception ex)
      {
        Logger.DebugFormat("Ошибка при чтении файла InstanceInfos.xml", ex);
      }
      return string.Empty;

    }
    
    /// <summary>
    /// Обрабатывает захваченный DCTS документ.
    /// </summary>
    /// <param name="xmlFiles">Строка с xml-данными DCTS по захваченным файлам.</param>
    /// <param name="xmlInfo">Строка с xml-данными DCTS с дополнительной информацией.</param>
    /// <param name="isCaptureByMail">Признак захвата через почту.</param>
    /// <param name="folder">Папка с xml-данными DCTS.</param>
    /// <param name="existEntity">Сущность системы.</param>
    /// <param name="lineSender">Наименование линии.</param>
    public static void ProcessEntity(string xmlFiles, string xmlInfo, string folder, IEntity existEntity, string lineSender)
    {
      XElement filesElement = XElement.Parse(xmlFiles);
      XElement infoElement = XElement.Parse(xmlInfo);
      
      // Сформировать словарь с файлами. Ключ - путь, значение - описание.
      var fileDict = new Dictionary<string, string>();
      foreach (var el in filesElement.Element("Files").Elements())
      {
        fileDict.Add(Path.Combine(folder, Path.GetFileName(el.Element("FileName").Value)), el.Element("FileDescription").Value);
      }
      
      // Сформировать словарь с дополнительными параметрами. Ключ - имя параметра, значение - описание.
      var paramDict = new Dictionary<string, string>();
      paramDict.Add("CaptureService", "FileSystem");
      FullProcessEntity(fileDict, paramDict, existEntity, lineSender);
    }
    
    /// <summary>
    /// Производит полный процесс обработки сущности.
    /// </summary>
    /// <param name="files">Xml-данные DCTS по захваченным файлам.</param>
    /// <param name="pars">Xml-данные DCTS с дополнительной информацией.</param>
    /// <param name="existEntity">Существующая сущность с которым ассоциировано данное правило (новая сущность не создается).</param>
    /// <param name="lineSender">Наименование линии.</param>
    protected static void FullProcessEntity(Dictionary<string, string> files, Dictionary<string, string> pars, IEntity existEntity, string lineSender)
    {
      var doc = OfficialDocuments.As(existEntity);
      
      if (doc == null)
        throw new ArgumentException(DirRX.IntegrationLLK.Resources.IncorrectTypeOfCreatedEntity, "entity");
      
      var file = files.FirstOrDefault(f => f.Value.ToUpper() != "BODY.TXT" &&
                                      f.Value.ToUpper() != "BODY.HTML");
      
      if (!string.IsNullOrEmpty(file.Key))
      {
        doc.CreateVersionFrom(file.Key);
        doc.Versions.Last().Note = DirRX.IntegrationLLK.Resources.NewVersionNoteTemplate;
        doc.Save();
        // TODO Если понадобится, то тут можно сделать метод, который позволит реализовать дополнительную функциональность при обработке существующего документа.
        Logger.DebugFormat("Обработка документа.");
        var docs = new List<Sungero.Contracts.IContractualDocument>(){Sungero.Contracts.ContractualDocuments.As(doc)};
        if (lineSender == Constants.Module.OurOriginal)
        {
          Logger.DebugFormat("OurOriginal");
          Solution.PublicFunctions.Contract.ExecuteSignedAction(docs);
        }
        else if (lineSender == Constants.Module.OurCopy)
        {
          Logger.DebugFormat("OurCopy");
          Solution.PublicFunctions.Contract.ExecuteCopySignedAction(docs);
        }
        else if (lineSender == Constants.Module.ContractorsOriginal)
        {
          Logger.DebugFormat("ContractorsOriginal");
          var contract = Solution.Contracts.As(doc);
          var supAgreement = Solution.SupAgreements.As(doc);
          // Для многосторонних договоров, создается только новая версия документа, автоматическое подписание и выполнение заданий недоступно. 
          if ((contract != null && contract.IsManyCounterparties != true)||
              (supAgreement != null && supAgreement.IsManyCounterparties != true))
          {
            Solution.PublicFunctions.Contract.ExecuteOriginalSignedAction(docs);
          }
        }
        

        Functions.Module.Remote.ExecuteTasks(doc);
      }
    }
  }
}