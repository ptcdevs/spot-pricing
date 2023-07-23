with grouped as (select "Instance Type",
                        min("PricePerUnit") minprice,
                        max("PricePerUnit") maxprice,
                        "Unit",
                        "PriceDescription",
                        "TermType",
                        "LeaseContractLength",
                        "PurchaseOption",
                        "OfferingClass",
                        "Current Generation",
                        "Tenancy",
                        "Operating System",
                        "License Model"
                 from "OnDemandPricing" ODP
--          join "OnDemandCsvFiles" ODCF on ODP."OnDemandCsvFilesId" = ODCF."Id"
                 where "Instance Family" = 'GPU instance'
                 group by "Instance Type",
                          "Unit",
                          "PriceDescription",
                          "TermType",
                          "LeaseContractLength",
                          "PurchaseOption",
                          "OfferingClass",
                          "Current Generation",
                          "Tenancy",
                          "Operating System",
                          "License Model"
                 order by max("PricePerUnit") - min("PricePerUnit") desc)

select
       g."Instance Type",
       regexp_match(ODCF."Url", '[0-9]{14}') CsvFileDate,
       op."EffectiveDate",
       op."PricePerUnit",
       g.minprice,
       g.maxprice,
       g."Unit",
       --g."PriceDescription",
       g."TermType",
       g."LeaseContractLength",
       g."PurchaseOption",
       g."OfferingClass",
       g."Current Generation",
       g."Tenancy",
       g."Operating System",
       g."License Model"
from grouped g
         join "OnDemandPricing" op on
            g."Instance Type" = op."Instance Type" and
            g."PriceDescription" = op."PriceDescription" and
            g."TermType" = op."TermType" and
            g."LeaseContractLength" = op."LeaseContractLength" and
            g."PurchaseOption" = op."PurchaseOption" and
            g."OfferingClass" = op."OfferingClass" and
            g."Current Generation" = op."Current Generation" and
            g."Tenancy" = op."Tenancy" and
            g."Operating System" = op."Operating System" and
            g."License Model" = op."License Model"
         join "OnDemandCsvFiles" ODCF on ODCF."Id" = op."OnDemandCsvFilesId"
-- where op."LeaseContractLength" = '1yr' and op."OfferingClass" = 'standard' and g."Operating System" = 'Linux' and g."Tenancy" = 'Dedicated'
where op."EffectiveDate" >= '2022-09-01' and g.maxprice <> g.minprice
order by g.maxprice - g.minprice desc,
         g."PriceDescription",
         g."TermType",
         g."LeaseContractLength",
         g."PurchaseOption",
         g."OfferingClass",
         g."Current Generation",
         g."Tenancy",
         g."Operating System",
         g."License Model",
         g."Instance Type",
         op."EffectiveDate"
