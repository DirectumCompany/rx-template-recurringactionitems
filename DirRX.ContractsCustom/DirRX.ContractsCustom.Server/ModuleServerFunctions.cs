using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution;
using System.Text;
using System.IO;
using Aspose.Words.Drawing;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text.RegularExpressions;

namespace DirRX.ContractsCustom.Server
{
  public class ModuleFunctions
  {

    /// <summary>
    /// Получить территорию для передачи в SAP.
    /// </summary>
    /// <param name="territory">Территория.</param>
    /// <returns>Территория для передачи в SAP.</returns>
    [Public, Remote(IsPure = true)]
    public string GetTerritorySAP(Enumeration? territory)
    {
      // Передавать только первые два символа.
      string territorySAP = territory.HasValue ? DirRX.Solution.Contracts.Info.Properties.Territory.GetLocalizedValue(territory) : string.Empty;
      if (!string.IsNullOrEmpty(territorySAP))
        territorySAP = territorySAP.Substring(0, 2);
      
      return territorySAP;
    }

    /// <summary>
    /// Получить табельный номер сотрудника для передачи в SAP.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <returns>Табельный номер.</returns>
    [Public, Remote(IsPure = true)]
    public string GetEmployeeSAP(Sungero.Company.IEmployee employee)
    {
      if (employee == null)
        return string.Empty;

      string employeeSAP = DirRX.Solution.Employees.As(employee).PersonnelNumber;
      return string.IsNullOrEmpty(employeeSAP) ? string.Empty : employeeSAP;
    }

    /// <summary>
    /// Получить валюту для передачи в SAP.
    /// </summary>
    /// <param name="currency">Валюта.</param>
    /// <returns>Валюта для передачи в SAP.</returns>
    [Public, Remote(IsPure = true)]
    public string GetCurrencySAP(Sungero.Commons.ICurrency currency)
    {
      if (currency == null)
        return string.Empty;

      string currencySAP = currency.AlphaCode;
      return string.IsNullOrEmpty(currencySAP) ? string.Empty : currencySAP;
    }

    /// <summary>
    /// Получить сумму для передачи в SAP.
    /// </summary>
    /// <param name="amount">Сумма.</param>
    /// <returns>Сумма для передачи в SAP.</returns>
    [Public, Remote(IsPure = true)]
    public string GetAmountSAP(double? amount)
    {
      return string.Format("{0:0.00}", amount).Replace(",", ".");
    }

    /// <summary>
    /// Получить регистрационный номер договора для передачи в SAP.
    /// </summary>
    /// <param name="regNumber">Регистрационный номер.</param>
    /// <returns>Регистрационный номер для передачи в SAP.</returns>
    [Public, Remote(IsPure = true)]
    public string GetRegistrationNumberSAP(string regNumber)
    {
      string regNumberSAP = string.IsNullOrEmpty(regNumber) ? string.Empty : regNumber;
      
      // Добавить ведущие нули до 4-х символов.
      if (regNumberSAP.Length < 4)
        while (regNumberSAP.Length < 4)
          regNumberSAP = "0" + regNumberSAP;
      
      return regNumberSAP;
    }

    /// <summary>
    /// Получить дату окончания действия договорного документа для передачи в SAP.
    /// </summary>
    /// <param name="validTill">Дата окончания действия.</param>
    /// <param name="validFrom">Дата начала действия.</param>
    /// <returns>Отформатированная строка с датой для передачи в SAP.</returns>
    [Public, Remote(IsPure = true)]
    public string GetValidTillSAP(DateTime? validTill, DateTime? validFrom)
    {
      return validTill.HasValue ? validTill.Value.ToString("dd.MM.yyyy") :
        validFrom.HasValue ? validFrom.Value.AddYears(100).ToString("dd.MM.yyyy") : string.Empty;
    }

    /// <summary>
    /// Получить все дополнительные соглашения в состоянии: Действующий, Недействующий. Закрыт в SAP, Недействующий. Открыт в SAP, Уничтоженный.
    /// </summary>
    [Public, Remote(IsPure = true)]
    public IQueryable<DirRX.Solution.ISupAgreement> GetSupAgreements(DirRX.Solution.IContract contract)
    {
      var supAgreements = SupAgreements.GetAll()
        .Where(s => Contracts.Equals(s.LeadingDocument, contract))
        .Where(s => s.LifeCycleState == DirRX.Solution.SupAgreement.LifeCycleState.Active ||
               s.LifeCycleState == DirRX.Solution.SupAgreement.LifeCycleState.Closed ||
               s.LifeCycleState == DirRX.Solution.SupAgreement.LifeCycleState.OpenSAP ||
               s.LifeCycleState == DirRX.Solution.SupAgreement.LifeCycleState.Deleted);
      return supAgreements;
    }

    /// <summary>
    /// Получить страну по коду.
    /// </summary>
    /// <param name="code">Код страны.</param>
    /// <returns>Страна.</returns>
    [Public, Remote(IsPure = true)]
    public static DirRX.Solution.ICountry GetCountryByCode(string code)
    {
      return DirRX.Solution.Countries.GetAll().Where(x => x.Code == code).FirstOrDefault();
    }

    /// <summary>
    /// Получить фильтрованный список подразделений для отчета "Общий отчет по договорам".
    /// </summary>
    /// <param name="getHeadDepartment">Вернуть только подразделения, которые являются головными.</param>
    /// <returns>Список отфильтрованных подразделений.</returns>
    /// <remarks>Исключается ООО «ЛЛК-Интернешнл».</remarks>
    [Remote(IsPure = true)]
    public static IQueryable<DirRX.Solution.IDepartment> GetDepartmentsForReport(bool getHeadDepartment)
    {
      var departments = DirRX.Solution.Departments.GetAll(x => x.HeadOffice != null && x.HeadOffice.HeadOffice != null);
      if (getHeadDepartment)
        return departments.ToList().Select(d => DirRX.Solution.Departments.As(d.HeadOffice)).Distinct().AsQueryable();

      return departments;
    }
    
    /// <summary>
    /// Определить, требуется ли корпоративное одобрение.
    /// </summary>
    /// <param name="amount">Сумма документа.</param>
    /// <param name="currency">Валюта для суммы документа.</param>
    /// <returns>Признак, требуется ли корпоративное одобрение.</returns>
    [Remote(IsPure = true), Public]
    public static bool IsCorporateApprovalRequired(double amount, Sungero.Commons.ICurrency currency)
    {
      var constant = ContractsCustom.PublicFunctions.Module.Remote.GetContractConstant(Constants.Module.CorporateApprovalAmountGuid.ToString());
      
      if (constant == null || !constant.Amount.HasValue || constant.Currency == null)
        return false;
      
      var constAmount = PublicFunctions.CurrencyRate.Remote.GetSummInRUB(constant.Amount.Value, constant.Currency);
      var totalDocumentAmount = PublicFunctions.CurrencyRate.Remote.GetSummInRUB(amount, currency);
      
      // Корпоративное одобрение требуется, если сумма документа больше или равна 25% балансовой стоимости.
      return totalDocumentAmount >= constAmount;
    }
    
    /// <summary>
    /// Получить сотрудника, ответственного за интеграцию с SAP.
    /// </summary>
    /// <returns>Сотрудник.</returns>
    [Remote(IsPure = true), Public] 
    public static DirRX.Solution.IEmployee GetSAPResponsible()
    {
      DirRX.Solution.IEmployee sapResponsible = null;
      var sapRole = Roles.GetAll().Where(r => r.Sid == Constants.Module.RoleGuid.SAPResponsibleRole).FirstOrDefault();
      if (sapRole != null)
        return sapRole.RecipientLinks.Select(r => DirRX.Solution.Employees.As(r.Member)).Where(m => m != null).FirstOrDefault();
      
      return null;
    }

    #region Построение отчета с конвертами

    /// <summary>
    /// Создать и заполнить временную таблицу для конвертов.
    /// </summary>
    /// <param name="reportSessionId">Идентификатор отчета.</param>
    /// <param name="shippingPackages">Список пакетов на отправку.</param>
    public static void FillEnvelopeTable(string reportSessionId, List<ContractsCustom.IShippingPackage> shippingPackages)
    {
      var id = 1;
      var dataTable = new List<Structures.Module.EnvelopeReportTableLine>();
      
      foreach (var shippingPackage in shippingPackages)
      {
        var correspondent = shippingPackage.Counterparty;
        var sender = shippingPackage.Employee;
        var senderAddress = shippingPackage.ShippingAddress;
        var employeePhone = shippingPackage.EmployeePhone;
        
        var tableLine = Structures.Module.EnvelopeReportTableLine.Create();
        tableLine.ReportSessionId = reportSessionId;
        tableLine.Id = id++;
        
        string addressToParse = string.Empty;
        string correspondentAddress = string.Empty;
        string ourAddress = string.Empty;
        string toName = string.Empty;
        string zipCodeTo = string.Empty;
        string zipCodeFrom = string.Empty;
        string fromName = string.Empty;
        string toContactName = string.Empty;
        string contactPhone = string.Empty;
        
        // Данные получателя.
        if (senderAddress != null)
        {
          addressToParse = senderAddress.Name;
        }
        
        if (correspondent != null)
        {
          if (string.IsNullOrEmpty(addressToParse))
          {
            addressToParse = !string.IsNullOrEmpty(correspondent.PostalAddress) ?
              correspondent.PostalAddress :
              correspondent.LegalAddress;
          }
          
          var person = Sungero.Parties.People.As(correspondent);
          if (person != null)
            toName = Sungero.Parties.PublicFunctions.Person.GetFullName(person, Sungero.Core.DeclensionCase.Dative);
          else
            toName = correspondent.Name;
          
          var contact = shippingPackage.Contact;
          if (contact != null)
          {
            toContactName = contact.Name;
            if (!string.IsNullOrEmpty(shippingPackage.ContactPhone))
              contactPhone = DirRX.ContractsCustom.Resources.PhoneEnvelopeReportTemplateFormat(shippingPackage.ContactPhone);
          }
        }
        
        if (!string.IsNullOrEmpty(addressToParse))
        {
          var zipCodeToParsingResult = Functions.Module.ParseZipCode(addressToParse);
          zipCodeTo = zipCodeToParsingResult.ZipCode;
          correspondentAddress = zipCodeToParsingResult.Address;
        }
        
        // Данные отправителя.
        if (sender!= null)
        {
          var businessUnit = sender.Department.BusinessUnit;
          if (businessUnit != null)
          {
            var addressFromParse = !string.IsNullOrEmpty(businessUnit.PostalAddress) ?
              businessUnit.PostalAddress :
              businessUnit.LegalAddress;
            var zipCodeFromParsingResult = Functions.Module.ParseZipCode(addressFromParse);
            zipCodeFrom = zipCodeFromParsingResult.ZipCode;
            ourAddress = zipCodeFromParsingResult.Address;
            fromName = businessUnit.Name;
          }
        }
        
        tableLine.ToName = toName;
        tableLine.ToPlace = correspondentAddress;
        // Если нет индекса, установить 6 пробелов, чтобы индекс выглядел как сетка, а не 000000.
        tableLine.ToZipCode = string.IsNullOrEmpty(zipCodeTo) ? Constants.Module.Spaces6 : zipCodeTo;
        tableLine.FromName = fromName;
        tableLine.FromPlace = ourAddress;
        tableLine.FromZipCode = zipCodeFrom;
        tableLine.EmployeePhone = string.IsNullOrEmpty(employeePhone) ? string.Empty : employeePhone;
        tableLine.ToContactName = toContactName;
        tableLine.ContactPhone = contactPhone;
        
        dataTable.Add(tableLine);
      }
      
      Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.CustomEnvelopeC4Report.EnvelopesTableName, dataTable);
    }
    
    /// <summary>
    /// Получить индекс и адрес без индекса.
    /// </summary>
    /// <param name="address">Андрес с индексом.</param>
    /// <returns>Структуры с индексом и адресом без индекса.</returns>
    public static DirRX.ContractsCustom.Structures.Module.ZipCodeAndAddress ParseZipCode(string address)
    {
      if (string.IsNullOrEmpty(address))
        return DirRX.ContractsCustom.Structures.Module.ZipCodeAndAddress.Create(string.Empty, string.Empty);
      
      // Индекс распознавать с ",", чтобы их удалить из адреса. В адресе на конверте индекса быть не должно.
      var zipCodeRegex = DirRX.ContractsCustom.Resources.ZipCodeRegex;
      var zipCodeMatch = Regex.Match(address, zipCodeRegex);
      var zipCode = zipCodeMatch.Success ? zipCodeMatch.Groups[1].Value : string.Empty;
      if (!string.IsNullOrEmpty(zipCode))
        address = address.Replace(zipCodeMatch.Value, string.Empty).Trim();
      
      return DirRX.ContractsCustom.Structures.Module.ZipCodeAndAddress.Create(zipCode, address);
    }
    
    #endregion
    
    /// <summary>
    /// Получить константу по Sid.
    /// </summary>
    /// <param name="sid">Sid константы.</param>
    /// <returns>Константа.</returns>
    [Remote(IsPure = true), Public]
    public static ContractsCustom.IContractConstant GetContractConstant(string sid)
    {
      return ContractsCustom.ContractConstants.GetAll().SingleOrDefault(r => r.Sid == sid);
    }
    
    /// <summary>
    /// Получить максимальную сумму договора в рублях из константы.
    /// </summary>
    /// <returns>Максимальная сумма договора.</returns>
    [Remote(IsPure = true), Public]
    public static double? GetContractMaxAmountInRUB()
    {
      var maxAmountConstant = GetContractConstant(DirRX.ContractsCustom.PublicConstants.Module.ContractMaxAmountGuid.ToString());
      if (maxAmountConstant != null && maxAmountConstant.Amount.HasValue && maxAmountConstant.Currency != null)
        return PublicFunctions.CurrencyRate.Remote.GetSummInRUB(maxAmountConstant.Amount.Value, maxAmountConstant.Currency);
      
      return null;
    }
    
    /// <summary>
    /// Получить общую сумму доп. соглашений по договору в валюте договора.
    /// </summary>
    /// <param name="contract">Договор.</param>
    /// <returns>Сумма в валюте договора.</returns>
    [Remote(IsPure = true), Public]
    public static double GetSupAgreementsAmount(DirRX.Solution.IContract contract)
    {
      double amount = 0.0;
      var supAgreements = SupAgreements.GetAll()
        .Where(s => Contracts.Equals(s.LeadingDocument, contract))
        .Where(s => s.LifeCycleState.HasValue && (s.LifeCycleState == DirRX.Solution.SupAgreement.LifeCycleState.Active ||
                                                  s.LifeCycleState == DirRX.Solution.SupAgreement.LifeCycleState.Closed ||
                                                  s.LifeCycleState == DirRX.Solution.SupAgreement.LifeCycleState.OpenSAP))
        .Where(s => s.TransactionAmount.HasValue);
      if (supAgreements.Any())
        amount = supAgreements.Sum(s => s.TransactionAmount.Value);
      return amount;
    }

    /// <summary>
    /// Получить системного контрагента для тендерных договоров закупки в обход фильтрации.
    /// </summary>
    /// <returns>Системный контрагент - "Контрагент для тендерных договоров закупки".</returns>
    [Remote(IsPure = true), Public]
    public static Sungero.Parties.ICounterparty GetTenderPurchaseCounterparty()
    {
      var guid = DirRX.ContractsCustom.Constants.Module.TenderPurchaseCounterpartyGuid;
      var link = Sungero.Docflow.PublicFunctions.Module.GetExternalLink(Guid.Parse("593e143c-616c-4d95-9457-fd916c4aa7f8"), guid);
      
      if (link != null && link.EntityId.HasValue)
      {
        var companyId = link.EntityId.Value;
        // HACK mukhachev: использование внутренней сессии для обхода фильтрации.
        Logger.DebugFormat("CreateDefaultTenderPurchaseCounterpartyIgnoreFiltering: companyId {0}", companyId);
        using (var session = new Sungero.Domain.Session())
        {
          var innerSession = (Sungero.Domain.ISession)session.GetType()
            .GetField("InnerSession", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(session);
          
          return Companies.As((Sungero.Domain.Shared.IEntity)innerSession.Get(typeof(ICompany), companyId));
        }
      }
      
      return null;
    }
    
    #region Простановка штрихкода и преобразование в PDF
    
    /// <summary>
    /// Получить поток с телом документа для последующей обработки.
    /// По сути HACK, который обходит ошибки при работе с MemoryStream.
    /// </summary>
    /// <param name="documents">Список документов для экспорта.</param>
    private static MemoryStream GetDocumentStream(Sungero.Docflow.IOfficialDocument document)
    {
      var directoryPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
      System.IO.Directory.CreateDirectory(directoryPath);
      var filePath = System.IO.Path.Combine(directoryPath, string.Format("{0}.blob", Guid.NewGuid().ToString()));
      document.Export(filePath);
      var outDocumentStream = new MemoryStream();
      using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        file.CopyTo(outDocumentStream);
      System.IO.Directory.Delete(directoryPath,true);
      return outDocumentStream;
    }
    
    /// <summary>
    /// Преобразовать документ в PDF и проставить штрихкод.
    /// </summary>
    /// <param name="document">Документ.</param>
    [Remote, Public]
    public static bool ConvertDocumentToPdfWithSBarcode(Sungero.Docflow.IOfficialDocument document)
    {
      var pdfConverter = PDFConverter.PDFConverter.Instance;
      Logger.DebugFormat("Converting document: {0}", document.Name);
      var documentStream = GetDocumentStream(document);
      var outDocumentStream = new MemoryStream();
      var signatures = Signatures.Get(document.LastVersion).Where(x => x.SignatureType == SignatureType.Approval);
      
      // Шрифт.
      var ttf = Path.Combine(pdfConverter.GetFontsDirectory(), "ARIAL.TTF");
      if (!File.Exists(ttf))
      {
        Logger.ErrorFormat("File of font {0} is not found. Install this font.", ttf);
        return false;
      }
      
      var font = iTextSharp.text.pdf.BaseFont.CreateFont(ttf, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
      var isLandscapeOrientation = false;
      var isDoc = false;
      var isSheet = false;
      
      var extension = document.LastVersion.AssociatedApplication.Extension;
      var appPDF = Sungero.Content.AssociatedApplications.GetAll(a => a.Extension == "pdf").FirstOrDefault();
      if (appPDF == null)
        return false;
      
      // Определение ориентации страницы.
      if (extension == "doc" || extension == "docx")
      {
        Logger.Debug("Document is doc or docx.");
        var doc = new Aspose.Words.Document(documentStream);
        Logger.Debug("Check orientation.");
        isLandscapeOrientation = doc.LastSection.PageSetup.Orientation == Aspose.Words.Orientation.Landscape;
        isDoc = true;
      }
      else if (extension == "xls" || extension == "xlsx")
      {
        Logger.Debug("Document is xls or xlsx.");
        var sheet = new Aspose.Cells.Workbook(documentStream).Worksheets.First();
        isLandscapeOrientation = sheet.PageSetup.Orientation == Aspose.Cells.PageOrientationType.Landscape;
        isSheet = true;
      }
      
      if (isDoc || isSheet)
      {
        outDocumentStream = pdfConverter.ConvertToPDF(documentStream, extension);
        outDocumentStream.Position = 0;
        document.LastVersion.PublicBody.Write(outDocumentStream);
        document.LastVersion.AssociatedApplication = appPDF;
      }
      else if (extension == "pdf")
      {
        document.LastVersion.PublicBody.Write(documentStream);
        document.LastVersion.AssociatedApplication = appPDF;
      }
      else
        return false;
      documentStream.Position = 0;
      documentStream.Close();
      
      #region Вставка штрих-кода.
      try
      {
        InsertBarcode(document, isLandscapeOrientation, font);
        document.Save();
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("Inserting of barcode is failed\n", ex);
        return false;
      }
      #endregion
      return true;
    }
    
    /// <summary>
    /// Вставить общий штамп в документ PDF.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="orientation">Ориентация страницы исходного документа.</param>
    /// <param name="font">Шрифт.</param>
    private static void InsertBarcode(Sungero.Docflow.IOfficialDocument document,
                                      bool isLandscapeOrientation,
                                      iTextSharp.text.pdf.BaseFont font)
    {
      var pdfConverter = PDFConverter.PDFConverter.Instance;
      var textX = isLandscapeOrientation ? 640f : 440f;
      var textY = 10f;
      var outDocumentStream = new MemoryStream();
      
      var documentStream = document.LastVersion.PublicBody.Read();
      PdfReader pdfReader = new PdfReader(documentStream);
      PdfStamper pdfStamper = new PdfStamper(pdfReader, outDocumentStream);
      for (var page = 1; page <= pdfReader.NumberOfPages; page++)
      {
        PdfContentByte canvas = pdfStamper.GetOverContent(page);
        // Графическое изображения для штрихкода.
        // Т.к. получамый WMF файл iTextSharp не может обработать, преобразуем файл в PNG и после этого наложим на PDF.
        var barcode = Sungero.Content.PublicFunctions.ElectronicDocument.Remote.GetBarcode(document);
        var imageFileName = Guid.NewGuid().ToString();
        var barcodePathWmf = Path.Combine(System.IO.Path.GetTempPath(), string.Format("{0}.wmf", imageFileName));
        System.IO.File.WriteAllBytes(barcodePathWmf, barcode);
        pdfConverter.ConvertWmfToPng(System.IO.Path.GetTempPath(), imageFileName);
        var barcodePathPng = Path.Combine(System.IO.Path.GetTempPath(), string.Format("{0}.png", imageFileName));
        var barcodePng = System.IO.File.ReadAllBytes(barcodePathPng);
        var image = iTextSharp.text.Image.GetInstance(barcodePng);
        image.ScaleAbsolute(126f, 44f);
        image.SetAbsolutePosition(textX, textY);
        canvas.AddImage(image);
        File.Delete(barcodePathWmf);
        File.Delete(barcodePathPng);
      }
      pdfStamper.Writer.CloseStream = false;
      pdfStamper.Close();
      outDocumentStream.Position = 0;
      document.LastVersion.PublicBody.Write(outDocumentStream);
      documentStream.Close();
      outDocumentStream.Close();
    }
    
    #endregion
    
    #region Работа со статусами договоров.

    /// <summary>
    /// Записать статус договорного документа.
    /// </summary>
    /// <param name="document">Договорной документ.</param>
    /// <param name="sid">Значение sid статуса договорного документа.</param>
    /// <param name="statusType">Тип статуса, "Статус согласования", "Статус движения скан-копии", "Статус движения оригинала".</param>
    /// <param name="removeOther">Очистить другие статусы.</param>
    [Public, Remote]
    public void SetCustomContractStatus(Sungero.Contracts.IContractualDocument document, Guid sid, string statusType, bool removeOther)
    {
      var status = DirRX.ContractsCustom.ContractStatuses.GetAll(s => s.Sid == sid.ToString()).FirstOrDefault();
      if (status == null)
      {
        Logger.Error("Не найден статус договора с sid = " + sid.ToString());
        return;
      }
      
      if (!DirRX.Solution.Contracts.Is(document) && !DirRX.Solution.SupAgreements.Is(document))
        return;
      
      var contract = DirRX.Solution.Contracts.As(document);
      var supAgreement = DirRX.Solution.SupAgreements.As(document);
      
      if (contract != null)
      {
        // Статус согласования.
        if (statusType == PublicConstants.Module.ContractStatusType.ApprovalStatus && !contract.ApproveStatuses.Any(s => DirRX.ContractsCustom.ContractStatuses.Equals(s.Status, status)))
        {
          if (removeOther)
            contract.ApproveStatuses.Clear();
          
          contract.ApproveStatuses.AddNew().Status = status;
        }
        
        // Статус движения скан-копии.
        if (statusType == PublicConstants.Module.ContractStatusType.ScanMoveStatus && !contract.ScanMoveStatuses.Any(s => DirRX.ContractsCustom.ContractStatuses.Equals(s.Status, status)))
        {
          if (removeOther)
            contract.ScanMoveStatuses.Clear();
          
          contract.ScanMoveStatuses.AddNew().Status = status;
        }
        
        // Статус движения оригинала.
        if (statusType == PublicConstants.Module.ContractStatusType.OriginalMoveStatus && !contract.OriginalMoveStatuses.Any(s => DirRX.ContractsCustom.ContractStatuses.Equals(s.Status, status)))
        {
          if (removeOther)
            contract.OriginalMoveStatuses.Clear();
          
          contract.OriginalMoveStatuses.AddNew().Status = status;
        }
      }
      
      if (supAgreement != null)
      {
        // Статус согласования.
        if (statusType == PublicConstants.Module.ContractStatusType.ApprovalStatus &&
            !supAgreement.ApproveStatuses.Any(s => DirRX.ContractsCustom.ContractStatuses.Equals(s.Status, status)))
        {
          if (removeOther)
            supAgreement.ApproveStatuses.Clear();
          
          supAgreement.ApproveStatuses.AddNew().Status = status;
        }
        
        // Статус движения скан-копии.
        if (statusType == PublicConstants.Module.ContractStatusType.ScanMoveStatus &&
            !supAgreement.ScanMoveStatuses.Any(s => DirRX.ContractsCustom.ContractStatuses.Equals(s.Status, status)))
        {
          if (removeOther)
            supAgreement.ScanMoveStatuses.Clear();
          
          supAgreement.ScanMoveStatuses.AddNew().Status = status;
        }
        
        // Статус движения оригинала.
        if (statusType == PublicConstants.Module.ContractStatusType.OriginalMoveStatus &&
            !supAgreement.OriginalMoveStatuses.Any(s => DirRX.ContractsCustom.ContractStatuses.Equals(s.Status, status)))
        {
          if (removeOther)
            supAgreement.OriginalMoveStatuses.Clear();
          
          supAgreement.OriginalMoveStatuses.AddNew().Status = status;
        }
      }
    }
    
    /// <summary>
    /// Стереть статус договорного документа.
    /// </summary>
    /// <param name="document">Договорной документ.</param>
    /// <param name="sid">Значение sid статуса договорного документа.</param>
    /// <param name="statusType">Тип статуса, "Статус согласования", "Статус движения скан-копии", "Статус движения оригинала".</param>
    [Public, Remote]
    public void RemoveCustomContractStatus(Sungero.Contracts.IContractualDocument document, Guid sid, string statusType)
    {
      if (!DirRX.Solution.Contracts.Is(document) && !DirRX.Solution.SupAgreements.Is(document))
        return;
      
      var contract = DirRX.Solution.Contracts.As(document);
      var supAgreement = DirRX.Solution.SupAgreements.As(document);
      
      if (contract != null)
      {
        // Статус согласования.
        if (statusType == PublicConstants.Module.ContractStatusType.ApprovalStatus)
        {
          var row = contract.ApproveStatuses.FirstOrDefault(s => s.Status.Sid == sid.ToString());
          if (row != null)
            contract.ApproveStatuses.Remove(row);
        }
        
        // Статус движения скан-копии.
        if (statusType == PublicConstants.Module.ContractStatusType.ScanMoveStatus)
        {
          var row = contract.ScanMoveStatuses.FirstOrDefault(s => s.Status.Sid == sid.ToString());
          if (row != null)
            contract.ScanMoveStatuses.Remove(row);
        }
        
        // Статус движения оригинала.
        if (statusType == PublicConstants.Module.ContractStatusType.OriginalMoveStatus)
        {
          var row = contract.OriginalMoveStatuses.FirstOrDefault(s => s.Status.Sid == sid.ToString());
          if (row != null)
            contract.OriginalMoveStatuses.Remove(row);
        }
      }
      
      if (supAgreement != null)
      {
        // Статус согласования.
        if (statusType == PublicConstants.Module.ContractStatusType.ApprovalStatus)
        {
          var row = supAgreement.ApproveStatuses.FirstOrDefault(s => s.Status.Sid == sid.ToString());
          if (row != null)
            supAgreement.ApproveStatuses.Remove(row);
        }
        
        // Статус движения скан-копии.
        if (statusType == PublicConstants.Module.ContractStatusType.ScanMoveStatus)
        {
          var row = supAgreement.ScanMoveStatuses.FirstOrDefault(s => s.Status.Sid == sid.ToString());
          if (row != null)
            supAgreement.ScanMoveStatuses.Remove(row);
        }
        
        // Статус движения оригинала.
        if (statusType == PublicConstants.Module.ContractStatusType.OriginalMoveStatus)
        {
          var row = supAgreement.OriginalMoveStatuses.FirstOrDefault(s => s.Status.Sid == sid.ToString());
          if (row != null)
            supAgreement.OriginalMoveStatuses.Remove(row);
        }
      }
    }
    
    #endregion
    
    /// <summary>
    /// Заполнить на закладке Выдача строки по отправке оригинала контрагенту.
    /// </summary>
    /// <param name="offDocument">Документ.</param>
    /// <param name="performer">Отправитель.</param>
    /// <param name="returnDeadline">Срок возврата.</param>
    [Public]
    public static void AddOriginalSendedToCounterpartyTracking(Sungero.Docflow.IOfficialDocument offDocument,
                                                               Sungero.Company.IEmployee performer, DateTime? returnDeadline)
    {
      var contract = Solution.Contracts.As(offDocument);
      var supAgreement = Solution.SupAgreements.As(offDocument);
      if (contract != null)
      {
        // TODO Дефект платформы, нет возможности по-другому привести типы у перекрытой коллекции.
        Solution.IContractTracking issue = contract.Tracking.AddNew() as Solution.IContractTracking;
        issue.DeliveredTo = performer;
        issue.Action = Solution.ContractTracking.Action.Sending;
        issue.DeliveryDate = Calendar.GetUserToday(performer);
        issue.ReturnDeadline = null;
        issue.IsOriginal = true;
        issue.Format = Solution.ContractTracking.Format.Original;
        
        // Если первым подписывает Общество, контрагент подписывает скан-образ, то добавим отметку о согласовании.
        if (contract.IsContractorSignsFirst != true && contract.IsScannedImageSign == true)
        {
          issue = contract.Tracking.AddNew() as Solution.IContractTracking;
          issue.DeliveredTo = performer;
          issue.Action = Solution.ContractTracking.Action.Endorsement;
          issue.DeliveryDate = Calendar.GetUserToday(performer);
          issue.ReturnDeadline = returnDeadline;
          issue.IsOriginal = true;
          issue.Format = Solution.ContractTracking.Format.Original;
          // TODO иначе не дает сохранить, т.к. при сохр OfficialDocument (строка 907) идет отправка задачи
          issue.ExternalLinkId = 0;
        }
        contract.Save();
      }
      
      if (supAgreement != null)
      {
        // TODO Дефект платформы, нет возможности по-другому привести типы у перекрытой коллекции.
        Solution.ISupAgreementTracking issue = supAgreement.Tracking.AddNew() as Solution.ISupAgreementTracking;
        issue.DeliveredTo = performer;
        issue.Action = Solution.SupAgreementTracking.Action.Sending;
        issue.DeliveryDate = Calendar.GetUserToday(performer);
        issue.ReturnDeadline = null;
        issue.IsOriginal = true;
        issue.Format = Solution.SupAgreementTracking.Format.Original;
        
        // Если первым подписывает Общество, контрагент подписывает скан-образ, то добавим отметку о согласовании.
        if (supAgreement.IsContractorSignsFirst != true && supAgreement.IsScannedImageSign == true)
        {
          issue = supAgreement.Tracking.AddNew() as Solution.ISupAgreementTracking;
          issue.DeliveredTo = performer;
          issue.Action = Solution.SupAgreementTracking.Action.Endorsement;
          issue.DeliveryDate = Calendar.GetUserToday(performer);
          issue.ReturnDeadline = returnDeadline;
          issue.IsOriginal = true;
          issue.Format = Solution.SupAgreementTracking.Format.Original;
          // TODO иначе не дает сохранить, т.к. при сохр OfficialDocument (строка 907) идет отправка задачи
          issue.ExternalLinkId = 0;
        }
        supAgreement.Save();
      }

    }
  }
}