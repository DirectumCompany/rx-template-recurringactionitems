do $$
begin
  if not exists(select 1 from Sungero_Docflow_Params where Key = '{0}')
  then
    insert into Sungero_Docflow_Params (Key, Value) values ('{0}', '{1}');
  else
    UPDATE Sungero_Docflow_Params SET Value = '{1}' WHERE (Key = '{0}');
  end if;
end$$;