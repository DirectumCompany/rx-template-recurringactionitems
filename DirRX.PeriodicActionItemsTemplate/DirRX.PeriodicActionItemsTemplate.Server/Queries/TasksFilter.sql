i.Status in ({0}) and 
coalesce((select max(Completed)
          from Sungero_WF_Assignment 
			    where Task = i.Id), coalesce(i.Started, i.Created)) < @date