using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ShippingPackage;
using Sungero.Metadata;
using Sungero.Domain.Shared;

namespace DirRX.ContractsCustom.Server
{
  partial class ShippingPackageFunctions
  {
    /// <summary>
    /// Установить статус «Принят к отправке».
    /// </summary>
    /// <param name="packages">Список пакетов.</param>
    /// <returns>Список пакетов, для которых не удалось установить статус «Принят к отправке».</returns>
    [Remote]
    public static List<IShippingPackage> SetStateOnAccepted(List<IShippingPackage> packages)
    {
      var notProcessedPackages = new List<IShippingPackage>();
      // Во всех выделенных пакетах установится статус «Принят к отправке».
      foreach (var package in packages)
      {
        try
        {
          package.PackageStatus = PackageStatus.Accepted;
          package.AcceptedDate = Calendar.Now;
          package.Save();
        }
        catch (Exception ex)
        {
          notProcessedPackages.Add(package);
          Logger.Debug(DirRX.ContractsCustom.ShippingPackages.Resources.PackageStatusAcceptedErrorMessageFormat(package.Id, ex.Message));
        }
      }
      return notProcessedPackages;
    }
    
    /// <summary>
    /// Отменить действие «Принято к отправке».
    /// </summary>
    /// <param name="packages">Список пакетов.</param>
    /// <returns>Список пакетов, для которых не удалось отменить действие «Принято к отправке».</returns>
    [Remote]
    public static List<IShippingPackage> CancelStateOnAccepted(List<IShippingPackage> packages)
    {
      var notProcessedPackages = new List<IShippingPackage>();
      // Во всех выделенных пакетах отменится действие «Принято к отправке».
      foreach (var package in packages)
      {
        try
        {
          package.PackageStatus = PackageStatus.Init;
          package.AcceptedDate = null;
          package.Save();
        }
        catch (Exception ex)
        {
          notProcessedPackages.Add(package);
          Logger.Debug(DirRX.ContractsCustom.ShippingPackages.Resources.PackageCancelAcceptedErrorMessageFormat(package.Id, ex.Message));
        }
      }
      return notProcessedPackages;
    }
    
    /// <summary>
    /// Установить статус «Отправлено».
    /// </summary>
    /// <param name="packages">Список пакетов.</param>
    /// <returns>Список пакетов, для которых не удалось установить статус «Отправлено».</returns>
    [Remote]
    public static List<IShippingPackage> SetStateOnSent(List<IShippingPackage> packages)
    {
      var notProcessedPackages = new List<IShippingPackage>();
      // Во всех выделенных пакетах установится статус «Отправлено».
      foreach (var package in packages)
      {
        try
        {
          package.PackageStatus = PackageStatus.Sent;
          package.SendedDate = Calendar.Now;
          package.SendedEmployee = Sungero.Company.Employees.Current;
          package.Save();
        }
        catch (Exception ex)
        {
          notProcessedPackages.Add(package);
          Logger.Debug(DirRX.ContractsCustom.ShippingPackages.Resources.PackageStatusSentErrorMessageFormat(package.Id, ex.Message));
        }
      }
      // Запустим ФП по выполнению заданий.
      Jobs.ApprovalSendingAssigmentsComplete.Enqueue();
      return notProcessedPackages;
    }
  }
}