if not exists(select 1 from [dbo].[Sungero_Docflow_Params] where [Key] = '{0}')
  insert [Sungero_Docflow_Params] ([Key],Value) values ('{0}', '{1}')
else UPDATE Sungero_Docflow_Params SET Value = '{1}' WHERE ([Key] = '{0}')