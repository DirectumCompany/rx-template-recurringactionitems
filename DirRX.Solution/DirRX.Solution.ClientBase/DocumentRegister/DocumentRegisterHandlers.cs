using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.DocumentRegister;

namespace DirRX.Solution
{
  partial class DocumentRegisterNumberFormatItemsClientHandlers
  {

    public override IEnumerable<Enumeration> NumberFormatItemsElementFiltering(IEnumerable<Enumeration> query)
    {
      query = base.NumberFormatItemsElementFiltering(query);
      // Ограничить выбор элемента Территория только для договорным документопотоком.
      if (_obj.DocumentRegister.DocumentFlow != Sungero.Docflow.DocumentRegister.DocumentFlow.Contracts)
        return query.Where(x => x.Value != DirRX.Solution.DocumentRegisterNumberFormatItems.Element.Territory.Value);
      return query;
    }
  }

  partial class DocumentRegisterClientHandlers
  {

  }
}