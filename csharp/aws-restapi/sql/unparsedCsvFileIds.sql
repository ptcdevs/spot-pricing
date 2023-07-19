select "Id" 
from "OnDemandCsvFiles"
where "Id" not in (select "OnDemandCsvFilesId" from "OnDemandPricing")
