using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.RecordCustom.Structures.DocumentationsForTax
{

	/// <summary>
	/// Строка отчета реестра документов налоговой группы.
	/// </summary>
	partial class DocumentsTaxGroup
	{
		public string ReportSessionId { get; set; }
		public int IncomingLetterId { get; set; }
		public int? OutgoingLetterId { get; set; }
		public string IncomingLetterHyperLink { get; set; }
		public string OutgoingLetterHyperLink { get; set; }
		public string Correspondent { get; set; }
		public string RegNumber { get; set; }
		public string DocumentDate { get; set; }
		public string Number { get; set; }
		public string DateOF { get; set; }
		public string RequarimentNumber { get; set; }
		public string Content { get; set; }
		public string Assignee { get; set; }
		public string CoAssignee { get; set; }
		public string PageCountIncoming { get; set; }
		public string DatePerformer { get; set; }
		public string CreateDateActionItem { get; set; }
		public string MaxDeadline { get; set; }
		public string ActualDate { get; set; }
		public string OutgoingLetter { get; set; }
		public string Prepared { get; set; }
		public string PageCountOutgoingLetter { get; set; }
		public int IsExpired { get; set; }
	}

}