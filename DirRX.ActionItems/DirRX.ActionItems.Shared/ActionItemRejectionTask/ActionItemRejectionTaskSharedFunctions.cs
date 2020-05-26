using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ActionItems.ActionItemRejectionTask;

namespace DirRX.ActionItems.Shared
{
  partial class ActionItemRejectionTaskFunctions
  {
    /// <summary>
    /// Сформировать тему для заданий по отклонению поручения.
    /// </summary>
    /// <param name="assignmentSubject"></param>
    /// <param name="actionItemExecutionTask">Отклоняемое поручение.</param>
    /// <returns>Тема задания.</returns>
    [Public]
    public static string GetSubjectRejectAssignment(string assignmentSubject, DirRX.Solution.IActionItemExecutionTask actionItemExecutionTask)
    {
      var subject = string.Format("{0}. {1}", assignmentSubject, actionItemExecutionTask.Subject);
      if (subject.Length > ActionItemRejectionTasks.Info.Properties.Subject.Length)
        subject = subject.Substring(0, ActionItemRejectionTasks.Info.Properties.Subject.Length);
      return subject;
    }
  }
}