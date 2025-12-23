delete from
  Sungero_Core_Link
where
  Id in (select
           link.Id
         from
           Sungero_Core_Link link 
           left join Sungero_Core_Folder folder on folder.Id = link.Folder
           left join {0} i on link.DestinationTypeGuid = i.Discriminator and link.DestinationId = i.Id and link.Folder = folder.Id
         where
           folder.Name = '{1}' and {2})