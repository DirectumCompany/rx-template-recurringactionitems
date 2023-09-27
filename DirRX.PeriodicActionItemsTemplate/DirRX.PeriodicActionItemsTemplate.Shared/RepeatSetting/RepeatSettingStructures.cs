using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.PeriodicActionItemsTemplate.Structures.RepeatSetting
{

  /// <summary>
  /// ������� ������������� ���������� ��������.
  /// </summary>
  partial class ArbitraryScheduleItem
  {
    public DateTime StartDate { get; set; }
    
    public DateTime? Deadline { get; set; }
  }

}