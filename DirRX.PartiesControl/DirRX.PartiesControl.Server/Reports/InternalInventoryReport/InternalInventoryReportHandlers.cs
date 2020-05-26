using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PartiesControl
{
  partial class InternalInventoryReportServerHandlers
  {

    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Sungero.Docflow.PublicFunctions.Module.DeleteReportData(Constants.InternalInventoryReport.DataTable, InternalInventoryReport.ReportSessionId);
    }

    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      var entity = InternalInventoryReport.Entity;
      InternalInventoryReport.ReportSessionId = System.Guid.NewGuid().ToString();
      InternalInventoryReport.CurrentDate = Calendar.Now.ToString();
      var reportHead = string.Format("{0} {1}", DirRX.PartiesControl.Reports.Resources.InternalInventoryReport.ChekingCounterpartyFormat(entity.Counterparty.DisplayValue),
                                     entity.CheckingDate.HasValue ? DirRX.PartiesControl.Reports.Resources.InternalInventoryReport.CheckingDateFormat(entity.CheckingDate.Value.ToShortDateString()) : string.Empty);
      InternalInventoryReport.ReportHead = reportHead.Trim();
      
      if (entity.CaseFile != null)
      {
        InternalInventoryReport.PlacedToCaseFile = DirRX.PartiesControl.Reports.Resources.InternalInventoryReport.PlacedToCaseFileFormat(entity.CaseFile.DisplayValue);
        if (entity.PlacedToCaseFileDate.HasValue)
          InternalInventoryReport.PlacedToCaseFileDate = DirRX.PartiesControl.Reports.Resources.InternalInventoryReport.PlacedToCaseFileDateFormat(entity.PlacedToCaseFileDate.Value.ToShortDateString());
      }
      
      var i = 1;
      var none = DirRX.PartiesControl.Reports.Resources.InternalInventoryReport.None;
      var sendingDate = (string)none;
      var approvalTask = DirRX.Solution.ApprovalTasks.GetAll()
        .Where(t => t.Status == Sungero.Workflow.Task.Status.InProcess ||
               t.Status == Sungero.Workflow.Task.Status.Completed ||
               t.Status == Sungero.Workflow.Task.Status.UnderReview)
        .Where(t => t.AttachmentDetails
               .Any(att => att.AttachmentId == entity.Id && att.GroupId == Sungero.Docflow.Constants.Module.TaskMainGroup.ApprovalTask))
        .OrderByDescending(t => t.Started.Value)
        .FirstOrDefault();
      if (approvalTask != null)
        if (approvalTask.Started.HasValue)
          sendingDate = approvalTask.Started.Value.ToShortDateString();
      
      var tableData = new List<Structures.InternalInventoryReport.TableLine>();
      tableData.Add(CreateTableLine(i,
                                    DirRX.PartiesControl.Reports.Resources.InternalInventoryReport.CounterpartyFile,
                                    none,
                                    DirRX.PartiesControl.Reports.Resources.InternalInventoryReport.Copy,
                                    sendingDate,
                                    entity.CheckingDate.HasValue ? entity.CheckingDate.Value.ToShortDateString(): none));
      
      foreach (var row in entity.BindingDocuments.Where(d => d.ReceiveDate.HasValue))
      {
        tableData.Add(CreateTableLine(++i,
                                      row.Document != null ? row.Document.DisplayValue : none,
                                      string.IsNullOrEmpty(row.Comment) ? none : row.Comment,
                                      row.Format.HasValue ? entity.Info.Properties.BindingDocuments.Properties.Format.GetLocalizedValue(row.Format.Value) : none,
                                      row.SendDate.HasValue ? row.SendDate.Value.ToShortDateString() : none,
                                      row.ReceiveDate.HasValue ? row.ReceiveDate.Value.ToShortDateString() : none ));
      }
      
      foreach (var row in entity.SecurityServiceDocuments.Where(d => d.ReceiveDate.HasValue))
      {
        tableData.Add(CreateTableLine(++i,
                                      row.Document != null ? row.Document.DisplayValue : none,
                                      string.IsNullOrEmpty(row.Comment) ? none : row.Comment,
                                      row.Format.HasValue ? entity.Info.Properties.BindingDocuments.Properties.Format.GetLocalizedValue(row.Format.Value) : none,
                                      row.SendDate.HasValue ? row.SendDate.Value.ToShortDateString() : none,
                                      row.ReceiveDate.HasValue ? row.ReceiveDate.Value.ToShortDateString() : none ));
      }
      
      Sungero.Docflow.PublicFunctions.Module.WriteStructuresToTable(Constants.InternalInventoryReport.DataTable, tableData);
    }
    
    /// <summary>
    /// Сформировать строку таблицы.
    /// </summary>
    /// <param name="id">№ строки.</param>
    /// <param name="DocName">Заголовок документа.</param>
    /// <param name="Comment">Комментарий.</param>
    /// <param name="Format">Формат.</param>
    /// <param name="SendingDate">Дата отправки.</param>
    /// <param name="RecievingDate">Дата получения.</param>
    /// <returns>Строка таблицы.</returns>
    public Structures.InternalInventoryReport.TableLine CreateTableLine(int id, string DocName, string Comment, string Format, string SendingDate, string RecievingDate)
    {
      var tableLine = new Structures.InternalInventoryReport.TableLine();
      tableLine.ReportSessionId = InternalInventoryReport.ReportSessionId;
      tableLine.Id = id;
      tableLine.DocName = DocName;
      tableLine.Comment = Comment;
      tableLine.Format = Format;
      tableLine.SendingDate = SendingDate;
      tableLine.RecievingDate = RecievingDate;
      return tableLine;
    }
  }

}