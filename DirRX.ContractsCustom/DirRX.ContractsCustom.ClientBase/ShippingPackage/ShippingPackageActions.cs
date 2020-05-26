using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ContractsCustom.ShippingPackage;

namespace DirRX.ContractsCustom.Client
{
  partial class ShippingPackageCollectionActions
  {

    public virtual bool CanAccepted(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Users.Current.IncludedIn(Constants.Module.RoleGuid.SendingResponsiblesRole) && 
        ShippingPackages.AccessRights.CanUpdate() && _objs.Any() && _objs.All(p => p.PackageStatus == PackageStatus.Init);
    }

    public virtual void Accepted(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var notProcessedPackages = Functions.ShippingPackage.Remote.SetStateOnAccepted(_objs.ToList());
      if (notProcessedPackages.Count == 0)
        Dialogs.NotifyMessage( DirRX.ContractsCustom.ShippingPackages.Resources.SelectedPackagesSetStateOnAcceptedSuccessfully);
      else
      {
        var totalCount = _objs.Count();
        var updCount = totalCount - notProcessedPackages.Count;
        DirRX.Solution.PublicFunctions.Module.ShowPackagesActionResultDialog(DirRX.ContractsCustom.ShippingPackages.Resources.SelectedPackagesSetStateOnAcceptedPartiallyFormat(updCount, totalCount),
                                                                             notProcessedPackages);
      }
    }

    public virtual bool CanSent(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Users.Current.IncludedIn(Constants.Module.RoleGuid.SendingResponsiblesRole) && 
        ShippingPackages.AccessRights.CanUpdate() && _objs.Any() && _objs.All(p => p.PackageStatus == PackageStatus.Accepted);
    }

    public virtual void Sent(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var notProcessedPackages = Functions.ShippingPackage.Remote.SetStateOnSent(_objs.ToList());
      if (notProcessedPackages.Count == 0)
        Dialogs.NotifyMessage( DirRX.ContractsCustom.ShippingPackages.Resources.SelectedPackagesSetStateOnSentSuccessfully);
      else
      {
        var totalCount = _objs.Count();
        var updCount = totalCount - notProcessedPackages.Count;
        DirRX.Solution.PublicFunctions.Module.ShowPackagesActionResultDialog(DirRX.ContractsCustom.ShippingPackages.Resources.SelectedPackagesSetStateOnSentPartiallyFormat(updCount, totalCount),
                                                                             notProcessedPackages);
      }
    }

    public virtual bool CanCancelAccepted(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return Users.Current.IncludedIn(Constants.Module.RoleGuid.SendingResponsiblesRole) && 
        ShippingPackages.AccessRights.CanUpdate() && _objs.Any() && _objs.All(p => p.PackageStatus == PackageStatus.Accepted);
    }

    public virtual void CancelAccepted(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var notProcessedPackages = Functions.ShippingPackage.Remote.CancelStateOnAccepted(_objs.ToList());
      if (notProcessedPackages.Count == 0)
        Dialogs.NotifyMessage( DirRX.ContractsCustom.ShippingPackages.Resources.SelectedPackagesCancelStateOnAcceptedSuccessfully);
      else
      {
        var totalCount = _objs.Count();
        var updCount = totalCount - notProcessedPackages.Count;
        DirRX.Solution.PublicFunctions.Module.ShowPackagesActionResultDialog(DirRX.ContractsCustom.ShippingPackages.Resources.SelectedPackagesCancelStateOnAcceptedPartiallyFormat(updCount, totalCount),
                                                                             notProcessedPackages);
      }
    }
  }

  partial class ShippingPackageActions
  {
    public override void DeleteEntity(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      base.DeleteEntity(e);
    }

    public override bool CanDeleteEntity(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanDeleteEntity(e) && _obj.PackageStatus == PackageStatus.Init;
    }




    public virtual void Print(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      var report = DirRX.ContractsCustom.Reports.GetCustomEnvelopeC4Report();
      report.ShippingPackages.AddRange(new List<IShippingPackage>() { _obj });
      report.Open();
    }

    public virtual bool CanPrint(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return !_obj.State.IsInserted && !_obj.State.IsChanged;
    }

  }


}