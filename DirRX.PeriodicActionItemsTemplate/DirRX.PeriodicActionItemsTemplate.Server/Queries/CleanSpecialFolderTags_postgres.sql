delete from
  Sungero_System_FolderTag
where
  Id in (select
           ft.Id
         from
           Sungero_System_FolderTag ft 
           left join {0} items on ft.EntityId = items.Id
         where
           ft.FolderId in ({1})
         except
         select
           ft.Id
         from
           Sungero_System_FolderTag ft
           left join {0} items on ft.EntityId = items.Id
           left join Sungero_Core_Folder folder on folder.Name = '{2}' and folder.Author = ft.Owner and folder.IsSpecial = True
           left join Sungero_Core_Link link on link.DestinationTypeGuid = items.Discriminator and link.DestinationId = items.Id and link.Folder = folder.Id
         where
           ft.FolderId in ({1}) and link.Id is not null)