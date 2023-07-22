with csv as (select '"OnDemandCsvFilesId",' || '"OnDemandCsvRowsId",' || ODCF."Header" as line,
                    0                                                                  as rownum
             from "OnDemandCsvFiles" ODCF
             where "Id" = @Id
             union
             select '"' || ODCR."OnDemandCsvFilesId" || '",' || '"' || ODCR."Id" || '",' || ODCR."Row" as line,
                    1                                                                                  as rownum

             from "OnDemandCsvRows" ODCR
             where "OnDemandCsvFilesId" = @Id)

select line
from csv
order by rownum
