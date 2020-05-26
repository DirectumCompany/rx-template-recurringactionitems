using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.ApprovalSigningAssignment;

namespace DirRX.Solution.Client
{
  partial class ApprovalSigningAssignmentFunctions
  {
	/// <summary>
    /// Показ поля "За кого" для вида док. "Исходящее письмо".
    /// </summary>     
    public void HideFildForWhom()
    {
    	if (_obj.DocumentGroup.OfficialDocuments.Any())
    		_obj.State.Properties.ForWhomDirRX.IsVisible = DirRX.Solution.OutgoingLetters.Is(_obj.DocumentGroup.OfficialDocuments.First());
    }
  }
}