i.Discriminator in ({0}) and 
i.Status not in ({1}) and 
case when datepart(hour, i.Deadline) = 0 and datepart(minute, i.Deadline) = 0 and datepart(second, i.Deadline) = 0
  then dateadd(day, 1, i.Deadline)
	else i.Deadline
end < @date