i.Discriminator in ({0}) and
i.Status in ({1})
and (select max(HistoryDate) 
		 from Sungero_WF_WorkflowHistory 
		 where EntityId = i.Id and 
		       Operation in ('Abort', 'CompleteAsg', 'CompleteTask')) < @date