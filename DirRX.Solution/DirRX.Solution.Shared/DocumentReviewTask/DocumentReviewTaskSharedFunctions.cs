using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.Solution.DocumentReviewTask;

namespace DirRX.Solution.Shared
{
	partial class DocumentReviewTaskFunctions
	{

		/// <summary>
		/// Проверить, стартована ли задача.
		/// </summary>
		///<returns>True, если стартована. False - иначе.</returns>
		public bool IsStarted()
		{
			return _obj.Started.HasValue;
		}
	}
}