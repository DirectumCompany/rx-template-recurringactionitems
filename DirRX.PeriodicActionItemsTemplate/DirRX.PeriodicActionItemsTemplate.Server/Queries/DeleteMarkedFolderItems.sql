delete from
  Sungero_System_MarkedFolderItems
where
  EntityId in (select
           	   	i.Id
         	   from
           	   	Sungero_Core_Link link 
           	   	left join Sungero_Core_Folder folder on folder.Id = link.Folder
           	   	left join {0} i on link.DestinationTypeGuid = i.Discriminator and link.DestinationId = i.Id and link.Folder = folder.Id
         	   where
           	   	folder.Name = '{1}' and {2})
  and FolderId = '{1}'