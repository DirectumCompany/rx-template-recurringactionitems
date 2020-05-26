using System;
using System.Collections.Generic;
using System.Linq;
// CORE: System.Net.Mail: Запрещено использование класса.
using System.Net.Mail;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Workflow;
using Sungero.Docflow;
using Sungero.Company;
//using Nustache.Core;
// CORE: Sungero.Domain.LinqExpressions: Запрещено использование класса.
using Sungero.Domain;
// CORE: CommonLibrary: Запрещено использование класса.
// CORE: Sungero.Domain.LinqExpressions: Запрещено использование класса.
using Sungero.Domain.LinqExpressions;
using CommonLibrary;
using DirRX.ProcessSubstitutionModule;

namespace DirRX.Solution.Module.MailAdapter.Server
{
  partial class ModuleFunctions
  {
    /// <summary>
    /// Запустить рассылку по новым заданиям.
    /// </summary>
    /// <param name="mailClient">Почтовый клиент. // CORE System.Net.Mail.SmtpClient: Запрещено использование класса.</param>
    /// <param name="previousRun">Дата прошлого запуска рассылки.</param>
    /// <param name="notificationDate">Дата текущей рассылки.</param>
    /// <returns>True, если хотя бы одно письмо было отправлено, иначе - false.</returns>
    [Public]
    public override bool? TrySendNewAssignmentsMailing(System.Net.Mail.SmtpClient mailClient, DateTime previousRun, DateTime notificationDate)
    {
      Logger.Debug("Checking new assignments for mailing");
      var hasErrors = false;
      // CORE: строковая константа.
      var newAssignments = AssignmentBases
        .GetAll(a => previousRun <= a.Created && a.Created < notificationDate && a.IsRead == false && a.Status != Sungero.Workflow.AssignmentBase.Status.Aborted)
        .Expand("Performer")
        .ToList();
      
      // Добавить к списку newAssignments задания, помеченные на отправку вручную в MailLogs.
      newAssignments.AddRange(DirRX.MailAdapter.MailLogses.GetAll(l => previousRun <= l.ReqNotifDate && l.ReqNotifDate <= notificationDate &&
                                                                  l.SendState == DirRX.MailAdapter.MailLogs.SendState.InProcess && l.SendType == DirRX.MailAdapter.MailLogs.SendType.Manual &&
                                                                  l.Assignment.Status == Sungero.Workflow.AssignmentBase.Status.InProcess)
                              .Select(m => m.Assignment));
      
      return SendAssignmentsMailing(mailClient, previousRun, notificationDate, newAssignments, false);
    }
    
    /// <summary>
    /// Запустить рассылку по просроченным заданиям.
    /// </summary>
    /// <param name="mailClient">Почтовый клиент.</param>
    /// <param name="previousRun">Дата прошлого запуска рассылки.</param>
    /// <param name="notificationDate">Дата текущей рассылки.</param>
    /// <returns>True, если хотя бы одно письмо было отправлено, иначе - false.</returns>
    // CORE System.Net.Mail.SmtpClient: Запрещено использование класса.
    [Public]
    public override bool? TrySendExpiredAssignmentsMailing(System.Net.Mail.SmtpClient mailClient, DateTime previousRun, DateTime notificationDate)
    {
      Logger.Debug("Checking expired assignments for mailing");
      // CORE: строковая константа.
      var expiredAssignments = AssignmentBases
        .GetAll(a => a.Status == Sungero.Workflow.AssignmentBase.Status.InProcess  &&
                (a.Deadline.HasValue && a.Deadline.Value.HasTime() &&
                 previousRun <= a.Deadline && a.Deadline < notificationDate ||
                 a.Deadline.HasValue && !a.Deadline.Value.HasTime() &&
                 previousRun <= a.Deadline.Value.AddDays(1) && a.Deadline.Value.AddDays(1) < notificationDate))
        .Expand("Performer")
        .ToList();
      
      // Добавить к списку newAssignments задания, помеченные на отправку вручную в MailLogs.
      expiredAssignments.AddRange(DirRX.MailAdapter.MailLogses.GetAll(l => previousRun <= l.ReqNotifDate && l.ReqNotifDate < notificationDate &&
                                                                      l.SendState != DirRX.MailAdapter.MailLogs.SendState.Sent && l.SendType == DirRX.MailAdapter.MailLogs.SendType.Manual &&
                                                                      l.Assignment.Status != Sungero.Workflow.AssignmentBase.Status.InProcess && (l.Assignment.Deadline.HasValue && l.Assignment.Deadline.Value.HasTime() &&
                                                                                                                                                  l.Assignment.Deadline < notificationDate || l.Assignment.Deadline.HasValue && !l.Assignment.Deadline.Value.HasTime() &&
                                                                                                                                                  l.Assignment.Deadline.Value.AddDays(1) < notificationDate))
                                  .Select(a => a.Assignment));
      
      return SendAssignmentsMailing(mailClient, previousRun, notificationDate, expiredAssignments, true);
    }
    
    /// <summary>
    /// Отправить письма по заданиям.
    /// </summary>
    /// <param name="mailClient">Почтовый клиент.</param>
    /// <param name="previousRun">Дата прошлого запуска рассылки.</param>
    /// <param name="notificationDate">Дата текущей рассылки.</param>
    /// <param name="assignmentList">Список заданий для рассылки.</param>
    /// <param name="isExpired">Признак того, что отправляются просроченные задания.</param>
    /// <returns>True, если хотя бы одно письмо было отправлено, иначе - false.</returns>
    private bool? SendAssignmentsMailing(System.Net.Mail.SmtpClient mailClient, DateTime previousRun, DateTime notificationDate, List<IAssignmentBase> assignmentList, bool isExpired)
    {
      var hasErrors = false;
      var activeSubstitutionList = ProcessSubstitutionModule.PublicFunctions.Module.GetActiveSubstitutionList();
      var anyMailSended = false;
      foreach (var assignment in assignmentList)
      {
        Logger.DebugFormat("Обработка задания с ИД: {0}.", assignment.Id);
        var employee = DirRX.MailAdapterSolution.Employees.As(assignment.Performer);
        if (employee == null)
          continue;
        var endDate = (isExpired ? notificationDate : assignment.Created.Value).AddDays(-1);
        
        Logger.DebugFormat("Поиск замещающих для сотрудника с ИД: {0}", employee.Id);
        var process = ProcessSubstitutionModule.PublicFunctions.Module.GetAssignmentProcess(assignment, assignment.Task);
        var substitutionRecords = activeSubstitutionList
          .Where(s => DirRX.Solution.Employees.Equals(s.Employee, employee))
          .Where(s => s.SubstitutionCollection.Any(c => !c.Process.HasValue || c.Process == process));
        
        var substitutors = new List<IUser>();
        foreach (var record in substitutionRecords)
        {
          if (record.SubstitutionCollection.Any(a => a.Process.HasValue))
            substitutors.AddRange(record.SubstitutionCollection.Where(s => s.Process.HasValue).Select(s => s.Substitute));
          else
            substitutors.AddRange(record.SubstitutionCollection.Select(s => s.Substitute));
        }
        
        var performers = substitutors.Select(s => DirRX.MailAdapterSolution.Employees.As(s))
          .Where(e => e.NeedNotifyNewAssignments == true && !isExpired || e.NeedNotifyExpiredAssignments == true && isExpired)
          .ToList();
        
        var subject = isExpired ?
          Sungero.Docflow.Resources.ExpiredAssignmentMailSubjectFormat(this.GetAssignmentTypeName(assignment).ToLower(),
                                                                       this.GetAuthorSubjectPart(assignment), assignment.Subject)
          : Resources.MailSubjectFormat(assignment.Subject);
        
        // Проверить, зафиксирована ли в списке MailLogs отправка для этого задания.
        var logitem = DirRX.MailAdapter.MailLogses.GetAll(l => previousRun <= l.ReqNotifDate && l.ReqNotifDate <= notificationDate && l.Assignment == assignment).FirstOrDefault();
        if (logitem == null)
          logitem = DirRX.MailAdapter.PublicFunctions.MailLogs.Remote.CreateMailLog(assignment, false);
        
        // Зафиксировать успешность рассылки.
        Logger.DebugFormat("Отправка писем по заданию с ИД: {0}.", assignment.Id);
        var mailSendResult = employee.IsExecuteThroughMail == true ? this.TrySendMailByAssignment(assignment, subject, isExpired, mailClient, performers) :
          this.TrySendMailByAssignmentBase(assignment, subject, isExpired, mailClient, employee, performers);
        
        if (!mailSendResult.IsSendMailSuccess)
        {
          hasErrors = true;
          logitem.SendState = DirRX.MailAdapter.MailLogs.SendState.Error;
        }
        else
        {
          logitem.SendState = DirRX.MailAdapter.MailLogs.SendState.Sent;
        }
        logitem.Save();
        if (mailSendResult.IsSendMailSuccess && mailSendResult.IsAnyMailSended)
          anyMailSended = true;
        
        Logger.DebugFormat("Обработка задания с ИД: {0} завершена.", assignment.Id);
      }
      if (!assignmentList.Any())
        Logger.DebugFormat("No {0} assignments for mailing", isExpired ? "expired" : "new");
      else if (!anyMailSended)
        Logger.DebugFormat("No subscribers for {0} assignments mailing", isExpired ? "expired" : "new");
      
      if (!anyMailSended && !hasErrors)
        return null;
      
      return anyMailSended || !hasErrors;
    }
    
    /// <summary>
    /// Получить локализованное имя типа задания.
    /// </summary>
    /// <param name="assignment">Базовое задание.</param>
    /// <returns>Имя типа задания.</returns>
    private string GetAssignmentTypeName(IAssignmentBase assignment)
    {
      if (Notices.Is(assignment))
        return Notices.Info.LocalizedName;
      else if (ReviewAssignments.Is(assignment))
        return ReviewAssignments.Info.LocalizedName;
      else
        return Assignments.Info.LocalizedName;
    }
    
    /// <summary>
    /// Получить часть темы письма, которая содержит автора задания.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <returns>Часть темы письма с автором задания.</returns>
    private string GetAuthorSubjectPart(IAssignmentBase assignment)
    {
      if (Equals(assignment.Author, assignment.Performer))
        return string.Empty;

      // CORE: строковая константа.
      return string.Format(" {0} {1}", Sungero.Docflow.Resources.From, this.GetFormattedUserNameInGenitive(assignment.Author.DisplayValue));
    }
    
    /// <summary>
    /// Получить форматированное имя пользователя в винительном падеже.
    /// </summary>
    /// <param name="userName">Имя пользователя.</param>
    /// <returns>Форматированное имя пользователя.</returns>
    private string GetFormattedUserNameInGenitive(string userName)
    {
      PersonFullName personalData;
      var result = userName;
      if (PersonFullName.TryParse(result, out personalData) && !string.IsNullOrEmpty(personalData.MiddleName))
      {
        personalData.DisplayFormat = PersonFullNameDisplayFormat.LastNameAndInitials;
        result = CaseConverter.ConvertPersonFullNameToTargetDeclension(personalData, Sungero.Core.DeclensionCase.Genitive);
      }
      return result;
    }
  }
}