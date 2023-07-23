select '"OnDemandCsvFilesId",' || '"OnDemandCsvRowsId",' || ODCF."Header" as line
into temporary table csvFile
from "OnDemandCsvFiles" ODCF
where "Id" = @Id;

insert into csvFile
select '"' || ODCR."OnDemandCsvFilesId" || '",' || '"' || ODCR."Id" || '",' || ODCR."Row" as line
from "OnDemandCsvRows" ODCR
where "OnDemandCsvFilesId" = @Id;
