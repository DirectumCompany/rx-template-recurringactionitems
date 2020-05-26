using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace DirRX.ContractsCustom.Server
{
  public partial class ModuleInitializer
  {
    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      // Создание типов и видов документов.
      CreateDocumentTypes();
      CreateDocumentKinds();
      
      // Создание ролей.
      InitializationLogger.Debug("Init: Create roles.");
      CreateRoles();
      
      // Создание ролей согласования.
      InitializationLogger.Debug("Init: Create approval roles.");
      CreateDefaultApprovalRoles();
      
      CreateTenderPurchaseCounterparty();
      
      // Создать записи справочника Константы
      InitializationLogger.Debug("Init: Create contract constants.");
      CreateConstants();
      
      // Создать записи справочника Статусы договоров.
      InitializationLogger.Debug("Init: Create contract statuses.");
      CreateContractStatuses();
      
      // Создать записи справочника Способы доставки.
      InitializationLogger.Debug("Init: Create delivery methods.");
      CreateMailDeliveryMethods();
      
      // Выдача прав.
      InitializationLogger.Debug("Init: Grant rights.");
      GrantRightToResponsibleSettingContract();
      GrantRightToResponsiblesECD();
      GrantRightToAllUsers();
      GrantRightsOnDocuments(Roles.AllUsers);
      GrantRightsOnFolders();
      GrantRightsOnReports();

      // Создать таблицы для отчетов.
      CreateReportsTables();
      
    }
    
    #region Создание констант.

    /// <summary>
    /// Создать перечень констант.
    /// </summary>
    public static void CreateConstants()
    {
      CreateConstant(Constants.Module.GeneralPeriodContractAndAdditAgreementGuid.ToString(), Constants.Module.GeneralPeriodContractAndAdditAgreementName, ContractsCustom.ContractConstant.TypeConst.Period);
      CreateConstant(Constants.Module.OriginalDeadlineGuid.ToString(), Constants.Module.OriginalDeadlineName, ContractsCustom.ContractConstant.TypeConst.Period);
      CreateConstant(Constants.Module.SendToPerformerConstantGuid.ToString(), Constants.Module.SendToPerformerConstantName, ContractsCustom.ContractConstant.TypeConst.Period);
      CreateConstant(Constants.Module.SendToSupervisorConstantGuid.ToString(), Constants.Module.SendToSupervisorConstantName, ContractsCustom.ContractConstant.TypeConst.Period);
      CreateConstant(Constants.Module.SendToFirstManagerConstantGuid.ToString(), Constants.Module.SendToFirstManagerConstantName, ContractsCustom.ContractConstant.TypeConst.Period);
      CreateConstant(Constants.Module.OriginalsControlTaskDeadlineConstantGuid.ToString(),
                     Constants.Module.OriginalsControlTaskDeadlineConstantName,
                     ContractsCustom.ContractConstant.TypeConst.Period,
                     ContractsCustom.ContractConstant.Unit.Day);
      CreateConstant(Constants.Module.ConfirmActivationConditionTaskDeadlineGuid.ToString(),
                     Constants.Module.ConfirmActivationConditionTaskDeadlineName,
                     ContractsCustom.ContractConstant.TypeConst.Period,
                     ContractsCustom.ContractConstant.Unit.Day);
      CreateConstant(Constants.Module.ContractExecutedRemindGuid.ToString(), Constants.Module.ContractExecutedRemindName, ContractsCustom.ContractConstant.TypeConst.Period, ContractsCustom.ContractConstant.Unit.Day);
      CreateConstant(Constants.Module.ContractMaxAmountGuid.ToString(), Constants.Module.ContractMaxAmountName, ContractsCustom.ContractConstant.TypeConst.Amount);
      CreateConstant(Constants.Module.TermDevelopNewContractFormGuid.ToString(), Constants.Module.TermDevelopNewContractFormName,
                     ContractsCustom.ContractConstant.TypeConst.Period, ContractsCustom.ContractConstant.Unit.Day);
      // Константа для изменения признака "Получение корпоративного одобрения".
      CreateConstant(Constants.Module.CorporateApprovalAmountGuid.ToString(), Constants.Module.CorporateApprovalAmountName, ContractsCustom.ContractConstant.TypeConst.Amount);
      CreateConstant(Constants.Module.BookValueAssetsGuid.ToString(), Constants.Module.BookValueAssetsName, ContractsCustom.ContractConstant.TypeConst.Amount);
      CreateConstant(Constants.Module.SendWithResposibleDeadlineGuid.ToString(),
                     Constants.Module.SendWithResposibleDeadlineName,
                     ContractsCustom.ContractConstant.TypeConst.Period,
                     ContractsCustom.ContractConstant.Unit.Day);
      CreateConstant(Constants.Module.ConfirmContractExecutedDeadlineGuid.ToString(),
                     Constants.Module.ConfirmContractExecutedDeadlineName,
                     ContractsCustom.ContractConstant.TypeConst.Period,
                     ContractsCustom.ContractConstant.Unit.Day);
      CreateConstant(Constants.Module.SendToIMSConstantGuid.ToString(),
                     Constants.Module.SendToIMSConstantName,
                     ContractsCustom.ContractConstant.TypeConst.Period,
                     ContractsCustom.ContractConstant.Unit.Day);
    }

    /// <summary>
    /// Создать новую константу.
    /// </summary>
    /// <param name="sid">Sid.</param>
    /// <param name="name">Наименование константы.</param>
    /// <param name="typeConst">Тип константы.</param>
    public static void CreateConstant(string sid, string name, Enumeration typeConst)
    {
      var constant = ContractConstants.GetAll().Where(r => Equals(r.Sid, sid)).FirstOrDefault();
      if (constant == null)
      {
        constant = ContractConstants.Create();
        constant.Sid = sid;
      }
      constant.Name = name;
      constant.TypeConst = typeConst;
      
      if (typeConst == ContractsCustom.ContractConstant.TypeConst.Amount)
        constant.Currency = PublicFunctions.CurrencyRate.Remote.GetCurrencyRUB();
      
      constant.Save();
    }
    
    /// <summary>
    /// Создать новую константу.
    /// </summary>
    /// <param name="sid">Sid.</param>
    /// <param name="name">Наименование константы.</param>
    /// <param name="typeConst">Тип константы.</param>
    /// <param name="unit">Единица измерения.</param>
    [Public]
    public static void CreateConstant(string sid, string name, Enumeration typeConst, Enumeration unit)
    {
      var constant = ContractConstants.GetAll().Where(r => Equals(r.Sid, sid)).FirstOrDefault();
      if (constant == null)
      {
        constant = ContractConstants.Create();
        constant.Sid = sid;
      }
      constant.Name = name;
      constant.TypeConst = typeConst;
      constant.Unit = unit;
      constant.Save();
    }
    
    #endregion
    
    #region Создание статусов договоров.
    
    /// <summary>
    /// Создать перечень статусов договоров.
    /// </summary>
    public static void CreateContractStatuses()
    {
      #region Статусы согласования.
      
      // Проверка контрагента.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.CounterpartyCheckingGuid,
                           Resources.ContractStatusNameCounterpartyChecking,
                           Resources.ContractStatusDescCounterpartyChecking);
      // На согласовании.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.OnApprovingGuid,
                           Resources.ContractStatusNameOnApproving,
                           Resources.ContractStatusDescOnApproving);
      // На доработке.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.OnReworkGuid,
                           Resources.ContractStatusNameOnRework,
                           Resources.ContractStatusDescOnRework);
      // Получение корпоративного одобрения.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.CorpAcceptanceGuid,
                           Resources.ContractStatusNameCorpAcceptance,
                           Resources.ContractStatusDescCorpAcceptance);
      // Получение согласования ПАО «ЛУКОЙЛ».
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.LukoilApprovedGuid,
                           Resources.ContractStatusNameLukoilApproved,
                           Resources.ContractStatusDescLukoilApproved);
      // Согласован.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.ApprovedGuid,
                           Resources.ContractStatusNameApproved,
                           Resources.ContractStatusDescApproved);
      // Отказ от заключения договора.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.RejectedGuid,
                           Resources.ContractStatusNameRejected,
                           Resources.ContractStatusDescRejected);
      // Передан Подписанту на подтверждение в электронном виде.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.SendedToSignerGuid,
                           Resources.ContractStatusNameSendedToSigner,
                           Resources.ContractStatusDescSendedToSigner);
      // Подтвержден Подписантом в электронном виде.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.SignerAcceptedGuid,
                           Resources.ContractStatusNameSignerAccepted,
                           Resources.ContractStatusDescSignerAccepted);
      #endregion
      
      #region Статусы движения скан-копий.
      
      // Контрагенту отправлен PDF-файл на подписание.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.PDFSendedForSigningGuid,
                           Resources.ContractStatusNamePDFSendedForSigning,
                           Resources.ContractStatusDescPDFSendedForSigning);
      // Контрагенту отправлена скан-копия на подписание.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.ScanSendedCounterpartyForSigningGuid,
                           Resources.ContractStatusNameScanSendedCounterpartyForSigning,
                           Resources.ContractStatusDescScanSendedCounterpartyForSigning);
      // Подписана скан-копия со стороны Контрагента.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.ScanSignedByCounterpartyGuid,
                           Resources.ContractStatusNameScanSignedByCounterparty,
                           Resources.ContractStatusDescScanSignedByCounterparty);
      // Скан-копия передана на подписание в Обществе.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.ScanSendedBusinessUnitForSigningGuid,
                           Resources.ContractStatusNameScanSendedBusinessUnitForSigning,
                           Resources.ContractStatusDescScanSendedBusinessUnitForSigning);
      // Скан-копия подписана всеми сторонами.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.ScanSignedByAllSidesGuid,
                           Resources.ContractStatusNameScanSignedByAllSides,
                           Resources.ContractStatusDescScanSignedByAllSides);
      // Контрагент отказался подписать документ.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.CounterpartyRejectedSigningGuid,
                           Resources.ContractStatusNameCounterpartyRejectedSigning,
                           Resources.ContractStatusDescCounterpartyRejectedSigning);
      #endregion
      
      #region Статусы движения оригиналов.
      
      // Оригинал передан на подписание в Обществе.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.OriginalSendedBusinessUnitForSigningGuid,
                           Resources.ContractStatusNameOriginalSendedBusinessUnitForSigning,
                           Resources.ContractStatusDescOriginalSendedBusinessUnitForSigning);
      // Оригинал подписан в Обществе.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.OriginalSignedByBusinessUnitGuid,
                           Resources.ContractStatusNameOriginalSignedByBusinessUnit,
                           Resources.ContractStatusDescOriginalSignedByBusinessUnit);
      // Подписант отказался подписать документ.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.SignerRejectedSigningGuid,
                           Resources.ContractStatusNameSignerRejectedSigning,
                           Resources.ContractStatusDescSignerRejectedSigning);
      // Оригиналы подписаны всеми сторонами.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.OriginalSignedByAllSidesGuid,
                           Resources.ContractStatusNameOriginalSignedByAllSides,
                           Resources.ContractStatusDescOriginalSignedByAllSides);
      // Ожидает отправки контрагенту.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.OriginalWaitingForSendingGuid,
                           Resources.ContractStatusNameOriginalWaitingForSending,
                           Resources.ContractStatusDescOriginalWaitingForSending);
      // Оригинал документа помещен в пакет для отправки.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.OriginalPlacedForSendingGuid,
                           Resources.ContractStatusNameOriginalPlacedForSending,
                           Resources.ContractStatusDescOriginalPlacedForSending);
      // Оригинал документа принят к отправке.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.OriginalAcceptedForSendingGuid,
                           Resources.ContractStatusNameOriginalAcceptedForSending,
                           Resources.ContractStatusDescOriginalAcceptedForSending);
      // Оригинал документа отправлен контрагенту.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.OriginalSendedToCounterpartyGuid,
                           Resources.ContractStatusNameOriginalSendedToCounterparty,
                           Resources.ContractStatusDescOriginalSendedToCounterparty);
      // Получен оригинал, подписанный Контрагентом.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.OriginalReceivedFromCounterpartyGuid,
                           Resources.ContractStatusNameOriginalReceivedFromCounterparty,
                           Resources.ContractStatusDescOriginalReceivedFromCounterparty);
      // Контрагент вернул неподписанный документ.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.OriginalReceivedNonSignedGuid,
                           Resources.ContractStatusNameOriginalReceivedNonSigned,
                           Resources.ContractStatusDescOriginalReceivedNonSigned);
      // Оригинал возвращен почтовой службой.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.OriginalReturnedByPostGuid,
                           Resources.ContractStatusNameOriginalReturnedByPost,
                           Resources.ContractStatusDescOriginalReturnedByPost);
      // Документ помещен в архив.
      CreateContractStatus(PublicConstants.Module.ContractStatusGuid.OriginalArchivedGuid,
                           Resources.ContractStatusNameOriginalArchived,
                           Resources.ContractStatusDescOriginalArchived);
      
      #endregion
    }
    
    /// <summary>
    /// Создать статус договора.
    /// </summary>
    public static void CreateContractStatus(Guid sid, string name, string description)
    {
      var status = ContractStatuses.GetAll().Where(r => Equals(r.Sid, sid.ToString())).FirstOrDefault();
      if (status == null)
      {
        status = ContractStatuses.Create();
        status.Sid = sid.ToString();
      }
      status.Name = name;
      status.Description = description;
      status.Save();
    }
    
    #endregion
    
    #region Создание типов и видов документов.
    
    /// <summary>
    /// Создать типы документов.
    /// </summary>
    public static void CreateDocumentTypes()
    {
      InitializationLogger.Debug("Init: Create document types");
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateDocumentType(Resources.MemoForPayment, MemoForPayment.ClassTypeGuid, Sungero.Docflow.DocumentType.DocumentFlow.Contracts, true);
    }
    
    /// <summary>
    /// Создать виды документов.
    /// </summary>
    public static void CreateDocumentKinds()
    {
      InitializationLogger.Debug("Init: Create document kinds.");
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(DirRX.ContractsCustom.Resources.ApplicationNewContractForm,
                                                                              DirRX.ContractsCustom.Resources.ApplicationNewContractFormShort,
                                                                              Sungero.Docflow.DocumentKind.NumberingType.NotNumerable,
                                                                              Sungero.Docflow.DocumentKind.DocumentFlow.Inner, false, false,
                                                                              Sungero.Docflow.Server.SimpleDocument.ClassTypeGuid,
                                                                              new Sungero.Domain.Shared.IActionInfo[] { Sungero.Docflow.OfficialDocuments.Info.Actions.SendForApproval },
                                                                              Constants.Module.DocumentKindGuid.ApplicationNewContractFormGuid, false);
      
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(DirRX.ContractsCustom.Resources.StandardContractForm,
                                                                              DirRX.ContractsCustom.Resources.StandardContractFormShort,
                                                                              Sungero.Docflow.DocumentKind.NumberingType.NotNumerable,
                                                                              Sungero.Docflow.DocumentKind.DocumentFlow.Inner, false, false,
                                                                              Sungero.Docflow.Server.SimpleDocument.ClassTypeGuid,
                                                                              new Sungero.Domain.Shared.IActionInfo[] { Sungero.Docflow.OfficialDocuments.Info.Actions.SendForApproval, Sungero.Docflow.OfficialDocuments.Info.Actions.SendForFreeApproval },
                                                                              Constants.Module.DocumentKindGuid.StandardContractFormGuid, false);
    }
    
    #endregion

    #region Создание ролей.
    
    /// <summary>
    /// Создать предопределенные роли.
    /// </summary>
    public static void CreateRoles()
    {
      // Сотрудники клиентского сервиса.
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.ContractsCustom.Resources.CustomerServiceEmployeesRoleName,
                                                                      DirRX.ContractsCustom.Resources.CustomerServiceEmployeesRoleName,
                                                                      Constants.Module.RoleGuid.CustomerServiceEmployeesRole);
      // Сотрудники ДПО.
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.ContractsCustom.Resources.DPOEmployeesRoleName,
                                                                      DirRX.ContractsCustom.Resources.DPOEmployeesRoleName,
                                                                      Constants.Module.RoleGuid.DPOEmployeesRole);

      // Ответственный за настройку модуля Договоры.
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.ContractsCustom.Resources.ResponsibleSettingContractRoleName,
                                                                      DirRX.ContractsCustom.Resources.ResponsibleSettingContractRoleName,
                                                                      Constants.Module.RoleGuid.ResponsibleSettingContractRole);
      // Сотрудники ЕЦД.
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.ContractsCustom.Resources.ECDEmployeesRoleName,
                                                                      DirRX.ContractsCustom.Resources.ECDEmployeesRoleDescription,
                                                                      Constants.Module.RoleGuid.ECDEmployeesRole);
      // Сотрудник ДПО.
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.ContractsCustom.Resources.DPOEmployeRoleName,
                                                                      DirRX.ContractsCustom.Resources.DPOEmployeRoleName,
                                                                      Constants.Module.RoleGuid.DPOEmployeRole);
      
      // Ответственный за SAP.
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.ContractsCustom.Resources.SAPResponsibleRoleName,
                                                                      DirRX.ContractsCustom.Resources.SAPResponsibleRoleName,
                                                                      Constants.Module.RoleGuid.SAPResponsibleRole);      
      // Ответственные за отправку.
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateRole(DirRX.ContractsCustom.Resources.SendingResponsiblesRoleName,
                                                                      DirRX.ContractsCustom.Resources.SendingResponsiblesRoleName,
                                                                      Constants.Module.RoleGuid.SendingResponsiblesRole);   
    }
    
    /// <summary>
    /// Создать роли для согласования договороных документов.
    /// </summary>
    public static void CreateDefaultApprovalRoles()
    {
      InitializationLogger.Debug("Init: Create default contracts roles.");
      
      CreateApprovalRole(ContractsCustom.ContractsRole.Type.ContractResp, ContractsRoles.Resources.Performer);
      CreateApprovalRole(ContractsCustom.ContractsRole.Type.CoExecutor, ContractsRoles.Resources.CoExecutor);
      // Роли согласования согласно справочнику Настройки согласования.
      CreateApprovalRole(ContractsCustom.ContractsRole.Type.Stage2Approvers, ContractsRoles.Resources.Stage2Approvers);
      CreateApprovalRole(ContractsCustom.ContractsRole.Type.NonLukoil, ContractsRoles.Resources.NonLukoil);
      CreateApprovalRole(ContractsCustom.ContractsRole.Type.NonStandart, ContractsRoles.Resources.NonStandart);
    }
    
    /// <summary>
    /// Создать роль для согласования договороных документов.
    /// </summary>
    /// <param name="roleType">Тип роли.</param>
    /// <param name="description">Описание роли.</param>
    public static void CreateApprovalRole(Enumeration roleType, string description)
    {
      InitializationLogger.DebugFormat("Init: Create contract action items role {0}", ContractsRoles.Info.Properties.Type.GetLocalizedValue(roleType));
      
      var role = ContractsRoles.GetAll().Where(r => Equals(r.Type, roleType)).FirstOrDefault();
      if (role == null)
        role = ContractsRoles.Create();
      
      role.Type = roleType;
      role.Description = description;
      role.Save();
    }
    
    /// <summary>
    /// Создать системного контрагента для рассылки нескольким адресатам.
    /// </summary>
    public static void CreateTenderPurchaseCounterparty()
    {
      var needLink = false;
      var guid = DirRX.ContractsCustom.Constants.Module.TenderPurchaseCounterpartyGuid;
      var name = DirRX.ContractsCustom.Resources.TenderPurchaseCounterparty;
      var company = DirRX.Solution.Companies.As(DirRX.ContractsCustom.PublicFunctions.Module.Remote.GetTenderPurchaseCounterparty());
      if (company == null)
      {
        company = DirRX.Solution.Companies.Create();
        needLink = true;
      }
      
      company.Name = name;
      company.State.IsEnabled = false;
      company.Save();
      
      if (needLink)
        Sungero.Docflow.PublicFunctions.Module.CreateExternalLink(company, guid);
    }
    
    #endregion
    
    #region Выдача прав.
    
    /// <summary>
    /// Выдать права роли "Ответственный за настройку модуля Договоры".
    /// </summary>
    public static void GrantRightToResponsibleSettingContract()
    {
      var contractSettingResponsible = Roles.GetAll().Where(n => n.Sid == Constants.Module.RoleGuid.ResponsibleSettingContractRole).FirstOrDefault();
      if (contractSettingResponsible == null)
        return;
      
      // Полные права на справочники.
      ContractsCustom.ContractSettingses.AccessRights.Grant(contractSettingResponsible, DefaultAccessRightsTypes.FullAccess);
      ContractsCustom.ContractSettingses.AccessRights.Save();
      
      ContractsCustom.DefaultReasonses.AccessRights.Grant(contractSettingResponsible, DefaultAccessRightsTypes.FullAccess);
      ContractsCustom.DefaultReasonses.AccessRights.Save();
      
      Solution.ContractCategories.AccessRights.Grant(contractSettingResponsible, DefaultAccessRightsTypes.FullAccess);
      Solution.ContractCategories.AccessRights.Save();
      
      ContractsCustom.ContractSubcategories.AccessRights.Grant(contractSettingResponsible, DefaultAccessRightsTypes.FullAccess);
      ContractsCustom.ContractSubcategories.AccessRights.Save();
      
      Solution.MailDeliveryMethods.AccessRights.Grant(contractSettingResponsible, DefaultAccessRightsTypes.FullAccess);
      Solution.MailDeliveryMethods.AccessRights.Save();
      
      ContractsCustom.ContractConstants.AccessRights.Grant(contractSettingResponsible, DefaultAccessRightsTypes.FullAccess);
      ContractsCustom.ContractConstants.AccessRights.Save();
      
      ContractsCustom.RequiredDocumentsSettingses.AccessRights.Grant(contractSettingResponsible, DefaultAccessRightsTypes.FullAccess);
      ContractsCustom.RequiredDocumentsSettingses.AccessRights.Save();
    }
    
    /// <summary>
    /// Выдать права всем пользователям.
    /// </summary>
    public static void GrantRightToAllUsers()
    {
      var allUsers = Roles.AllUsers;
      
      ContractConstants.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      ContractConstants.AccessRights.Save();
      
      ContractSettingses.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      ContractSettingses.AccessRights.Save();

      MatchingSettings.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      MatchingSettings.AccessRights.Save();
      
      ContractSubcategories.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      ContractSubcategories.AccessRights.Save();
      
      CurrencyRates.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      CurrencyRates.AccessRights.Save();
      
      DefaultReasonses.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      DefaultReasonses.AccessRights.Save();
      
      ShippingPackages.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      ShippingPackages.AccessRights.Save();
      
      IMSContractCodes.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      IMSContractCodes.AccessRights.Save();
      
      ContractStatuses.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      ContractStatuses.AccessRights.Save();
      
      SendWithResposibleTasks.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Create);
      SendWithResposibleTasks.AccessRights.Save();
      
      RequiredDocumentsSettingses.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
      RequiredDocumentsSettingses.AccessRights.Save();
    }

    /// <summary>
    /// Назначить права на документы и справочники.
    /// </summary>
    public static void GrantRightsOnDocuments(IRole allUsers)
    {
      InitializationLogger.Debug("Init: Grant rights on documents");

      MemoForPayments.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Change);
      MemoForPayments.AccessRights.Save();
    }

    /// <summary>
    /// Выдать права на папки.
    /// </summary>
    public static void GrantRightsOnFolders()
    {
      var ecdRole = Roles.GetAll().Where(r => r.Sid == Constants.Module.RoleGuid.ECDEmployeesRole).FirstOrDefault();
      if (ecdRole != null)
      {
        DirRX.Solution.Module.ContractsUI.SpecialFolders.OriginalsOnSigning.AccessRights.Grant(ecdRole, DefaultAccessRightsTypes.Read);
        DirRX.Solution.Module.ContractsUI.SpecialFolders.OriginalsOnSigning.AccessRights.Save();
        DirRX.Solution.Module.ContractsUI.SpecialFolders.OriginalsToBeReturned.AccessRights.Grant(ecdRole, DefaultAccessRightsTypes.Read);
        DirRX.Solution.Module.ContractsUI.SpecialFolders.OriginalsToBeReturned.AccessRights.Save();
        DirRX.Solution.Module.ContractsUI.SpecialFolders.OriginalsToSend.AccessRights.Grant(ecdRole, DefaultAccessRightsTypes.Read);
        DirRX.Solution.Module.ContractsUI.SpecialFolders.OriginalsToSend.AccessRights.Save();
        DirRX.Solution.Module.ContractsUI.SpecialFolders.CopyOnSigning.AccessRights.Grant(ecdRole, DefaultAccessRightsTypes.Read);
        DirRX.Solution.Module.ContractsUI.SpecialFolders.CopyOnSigning.AccessRights.Save();
        DirRX.Solution.Module.ContractsUI.SpecialFolders.Registration.AccessRights.Grant(ecdRole, DefaultAccessRightsTypes.Read);
        DirRX.Solution.Module.ContractsUI.SpecialFolders.Registration.AccessRights.Save();
      }
    }
    
    /// <summary>
    /// Выдать права на отчеты.
    /// </summary>
    public static void GrantRightsOnReports()
    {
      var allUsers = Roles.AllUsers;
      Reports.AccessRights.Grant(Reports.GetCustomEnvelopeC4Report().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
      Reports.AccessRights.Grant(Reports.GetContractReport().Info, allUsers, DefaultReportAccessRightsTypes.Execute);
    }
    
    /// <summary>
    /// Выдать права роли "Сотрудники ЕЦД".
    /// </summary>
    public static void GrantRightToResponsiblesECD()
    {
      var role = Roles.GetAll().Where(n => n.Sid == Constants.Module.RoleGuid.ECDEmployeesRole).FirstOrDefault();
      if (role == null)
        return;
      
      // Права на изменение Пакетов на отправку.
      ContractsCustom.ShippingPackages.AccessRights.Grant(role, DefaultAccessRightsTypes.Change);
      ContractsCustom.ShippingPackages.AccessRights.Save();
    }
    
    #endregion
    
    #region Отчеты
    
    /// <summary>
    /// Создать таблицы для отчетов.
    /// </summary>
    public static void CreateReportsTables()
    {
      var envelopesReportsTableName = Constants.CustomEnvelopeC4Report.EnvelopesTableName;
      Sungero.Docflow.PublicFunctions.Module.DropReportTempTables(new[] {envelopesReportsTableName});
      Sungero.Docflow.Server.ModuleFunctions.ExecuteSQLCommandFormat(Queries.CustomEnvelopeC4Report.CreateEnvelopesTable, new[] { envelopesReportsTableName });
      
      var contractReportTableName = Constants.ContractReport.SourceTableName;
      Sungero.Docflow.PublicFunctions.Module.DropReportTempTables(new[] { contractReportTableName});
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.ContractReport.CreateTable, new[] { contractReportTableName });
    }
    
    #endregion
    
    #region Создание способов доставки.
    public static void CreateMailDeliveryMethods()
    {
      Sungero.Docflow.Server.ModuleInitializer.CreateMailDeliveryMethod(DirRX.ContractsCustom.Resources.WithRensposibleMailDeliveryMethod, Constants.Module.WithRensposibleMailDeliveryMethod.ToString());
    }
    #endregion
  }
}