using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.PeriodicActionItemsTemplate.Priority;

namespace DirRX.PeriodicActionItemsTemplate
{
	partial class PriorityClientHandlers
	{

		public virtual void PriorityValueValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
		{
			if (e.NewValue.HasValue && e.NewValue.Value <= 0)
			{
				e.AddError(ActionItems.Resources.ValueMustBePositive);
			}
		}

		public virtual void EscalationPeriodWorkDaysValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
		{
			if (e.NewValue.HasValue && e.NewValue.Value <= 0)
			{
				e.AddError(ActionItems.Resources.ValueMustBePositive);
			}
		}

		public virtual void EscalationPeriodPercentValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
		{
			if (e.NewValue.HasValue && (e.NewValue.Value < 0 || e.NewValue.Value >= 100))
			{
				e.AddError(ActionItems.Resources.PercentMustBeInRange);
			}
		}

		public virtual void CompletionDeadlinePercentValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
		{
			if (e.NewValue.HasValue && (e.NewValue.Value <= 0 || e.NewValue.Value >= 100))
			{
				e.AddError(ActionItems.Resources.PercentMustBeInRange);
			}
		}

		public virtual void RejectionDeadlinePercentValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
		{
			if (e.NewValue.HasValue && (e.NewValue.Value <= 0 || e.NewValue.Value >= 100))
			{
				e.AddError(ActionItems.Resources.PercentMustBeInRange);
			}
		}

	}
}