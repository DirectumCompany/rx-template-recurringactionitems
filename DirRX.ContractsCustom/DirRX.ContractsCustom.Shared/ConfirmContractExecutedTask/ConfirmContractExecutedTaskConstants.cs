using System;
using Sungero.Core;

namespace DirRX.ContractsCustom.Constants
{
  public static class ConfirmContractExecutedTask
  {

    /// <summary>
    /// Срок по умолчанию на повторную отправку задачи на подтверждение завершения договора.
    /// </summary>
    public const int ResendingConfirmContractExecutedDeadline = 30;

  }
}