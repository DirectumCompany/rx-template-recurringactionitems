using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.OutgoingLetter;

namespace DirRX.Solution.Server
{
  partial class OutgoingLetterFunctions
  {
		public override List<Sungero.Docflow.Structures.SignatureSetting.Signatory> GetSignatories()
		{
			return base.GetSignatories();
		}
  }
}