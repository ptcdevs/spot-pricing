with data as (select "Instance Type",
                     "SKU",
                     "EffectiveDate",
                     "PricePerUnit",
                     "OfferTermCode",
                     "RateCode",
                     "OnDemandCsvFilesId",
                     "OnDemandCsvRowsId",
                     "TermType",
                     "PriceDescription",
                     "StartingRange",
                     "EndingRange",
                     "Unit",
                     "Currency",
                     "RelatedTo",
                     "LeaseContractLength",
                     "PurchaseOption",
                     "OfferingClass",
                     "Product Family",
                     "serviceCode",
                     "Location",
                     "Location Type",
                     "Current Generation",
                     "Instance Family",
                     "vCPU",
                     "Physical Processor",
                     "Clock Speed",
                     "Memory",
                     "Storage",
                     "Network Performance",
                     "Processor Architecture",
                     "Tenancy",
                     "Operating System",
                     "License Model",
                     "GPU",
                     "GPU Memory",
                     "instanceSKU",
                     "MarketOption",
                     "Normalization Size Factor",
                     "Physical Cores",
                     "Processor Features",
                     "Region Code",
                     "serviceName",
                     rank() over (partition by
                         "Instance Type",
                         "EffectiveDate",
                         "Tenancy",
                         "Operating System",
                         "License Model",
                         "PurchaseOption",
                         "OfferingClass" ,
                         "PurchaseOption",
                         "LeaseContractLength"
                         order by random()) as duprank,
                     min("PricePerUnit") over (partition by
                         "Instance Type",
                         "Tenancy",
                         "Operating System",
                         "License Model",
                         "PurchaseOption",
                         "OfferingClass" ,
                         "PurchaseOption",
                         "LeaseContractLength"
                         ) as minPrice,
                     max("PricePerUnit") over (partition by
                         "Instance Type",
                         "Tenancy",
                         "Operating System",
                         "License Model",
                         "PurchaseOption",
                         "OfferingClass" ,
                         "PurchaseOption",
                         "LeaseContractLength"
                         ) as maxPrice,
                     md5(ROW(
                         "Instance Type",
                         "Tenancy",
                         "Operating System",
                         "License Model",
                         "PurchaseOption",
                         "OfferingClass" ,
                         "PurchaseOption",
                         "LeaseContractLength"
                         )::TEXT) as hash
              from "OnDemandPricing"
              where "Unit" = 'Hrs'
--                 and "Instance Type" = 'p4d.24xlarge'
                and "Region Code" = 'us-east-1'
                and "PricePerUnit" > '$0.00'
              order by "Instance Type",
                       "EffectiveDate",
                       "Tenancy",
                       "Operating System",
                       "License Model",
                       "PurchaseOption",
                       "OfferingClass",
                       "PurchaseOption",
                       "LeaseContractLength")

select "Instance Type",
       "SKU",
       "instanceSKU",
       hash,
       "EffectiveDate",
       "PricePerUnit",
       minPrice,
       maxPrice,
       "OfferTermCode",
       "RateCode",
       "OnDemandCsvFilesId",
       "OnDemandCsvRowsId",
       "TermType",
       "PriceDescription",
       "StartingRange",
       "EndingRange",
       "Unit",
       "Currency",
       "RelatedTo",
       "LeaseContractLength",
       "PurchaseOption",
       "OfferingClass",
       "Product Family",
       "serviceCode",
       "Location",
       "Location Type",
       "Current Generation",
       "Instance Family",
       "vCPU",
       "Physical Processor",
       "Clock Speed",
       "Memory",
       "Storage",
       "Network Performance",
       "Processor Architecture",
       "Tenancy",
       "Operating System",
       "License Model",
       "GPU",
       "GPU Memory",
       "MarketOption",
       "Normalization Size Factor",
       "Physical Cores",
       "Processor Features",
       "Region Code",
       "serviceName",
       duprank,
       minPrice,
       maxPrice
from data
where duprank = 1
  and minPrice != maxPrice
order by "Instance Type",
         "Tenancy",
         "Operating System",
         "License Model",
         "PurchaseOption",
         "OfferingClass",
         "PurchaseOption",
         "LeaseContractLength",
         "SKU",
         "EffectiveDate"
