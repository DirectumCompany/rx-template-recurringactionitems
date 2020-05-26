using System;
using Sungero.Core;

namespace DirRX.Solution.Constants
{
  public static class Module
  {

    // Имя вида документов, если требуется анализ на признак МСФО 16.
    [Public]
    public const string AnalysisDocKind = "Анализ договора на предмет признаков аренды по МСФО 16";

    /// <summary>Параметры вставки текста по тэгам.</summary>
    public static class SignatureInfo
    {
      public const string DocxDocumentExtension = "docx";
      public const string PdfDocumentExtension = "pdf";
      public const string EmployeeTagName = "ФИО подписывающего";
      public const string JobTitleTagName = "Должность подписывающего";
      public const string RegNameTagName = "Номер приказа";
      public const string RegDateTagName = "Дата документа";
    }
    
    /// <summary>Наименования поддерживаемых атрибутов при вставки текста по тэгам.</summary>
    public static class ConverterAttribute
    {
      public const string InitialsAndLastName = "InitialsAndLastName";
      public const string LastNameAndInitials = "LastNameAndInitials";
      public const string FullName = "FullName";
    }
    
    /// <summary>Наименования поддерживаемых атрибутов при вставки текста по тэгам.</summary>
    public static class DocumentTypeGuid
    {
      [Public]
      public const string Contract = "f37c7e63-b134-4446-9b5b-f8811f6c9666";
      [Public]
      public const string SupAgreement = "265f2c57-6a8a-4a15-833b-ca00e8047fa5";
      [Public]
      public const string Order = "9570e517-7ab7-4f23-a959-3652715efad3";
      [Public]
      public const string RevisionRequest = "3ec49d0f-9c4d-4d48-a56d-b267fb8ba644";
      [Public]
      public const string Memo = "95af409b-83fe-4697-a805-5a86ceec33f5";
      [Public]
      public const string MemoForPayment = "d465f226-3962-4e39-8f21-93abbf7b3182";
    }
        
    // Имя типа связи "Прочие".
    [Public]
    public const string SimpleRelationRelationName = "Simple relation";
    
    // Имя типа связи "Приложение".
    [Public]
    public const string AddendumRelationName = "Addendum";
    
    // Имя вида документов если используется Товарный знак 3-го лица.
    [Public]
    public const string TrademarkDocKind = "Анкета по товарному знаку третьего лица";
    
    // Имя вида документов для закупочных договоров.
    [Public]
    public const string PurchaseDocKind = "Заключение об экономической оправданности сделки";
    
    /// <summary>
    /// Код диалога создания выдачи документа.
    /// </summary>
    [Public]
    public const string DeliverHelpCode = "Sungero_DeliverDialog";
  }
}