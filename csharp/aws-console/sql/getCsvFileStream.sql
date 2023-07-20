-- drop table csvFile;
with csvUnion as (select 0             as priority,
                         ODCF."Header" as line
                  from "OnDemandCsvFiles" ODCF
                  where "Id" = 48
                  union
                  select 1          as priority,
                         ODCR."Row" as line
                  from "OnDemandCsvRows" ODCR
                  where "OnDemandCsvFilesId" = 48
                  limit 100)

select line
into temporary table csvFile 
from csvUnion
order by priority
limit 1000;

copy csvFile (line) TO STDOUT (FORMAT TEXT)
