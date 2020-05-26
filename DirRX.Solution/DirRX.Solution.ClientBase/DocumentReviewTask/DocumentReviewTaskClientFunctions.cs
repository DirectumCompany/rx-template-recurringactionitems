using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.DocumentReviewTask;

namespace DirRX.Solution.Client
{
  partial class DocumentReviewTaskFunctions
  {
	/// <summary>
    /// Скрыть поля от редактирования.
    /// </summary>       
    public void HideField()
    {
    	var state = !Functions.DocumentReviewTask.IsStarted(_obj);
    	
    	_obj.State.Properties.Addressee.IsEnabled = state;
    	_obj.State.Properties.Author.IsEnabled = state;
    	_obj.State.Properties.Deadline.IsEnabled = state;
    	_obj.State.Properties.SubcribersDirRX.IsEnabled = state;
    }
	
  }
}