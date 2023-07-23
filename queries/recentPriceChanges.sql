with grouped as (select "Instance Type",
                        min("PricePerUnit") minprice,
                        max("PricePerUnit") maxprice,
                        count(*) as         setCount,
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
                 order by max("PricePerUnit") - min("PricePerUnit") desc),

     recent_price_changes as (select "Instance Type",
                                     "EffectiveDate",
                                     "PricePerUnit",
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
                              from "OnDemandPricing"
                              where "EffectiveDate" > '2023-01-01'),
     report as (select g."Instance Type",
                       minprice,
                       maxprice,
                       setCount,
                       p."EffectiveDate",
                       p."PricePerUnit",
                       g."Unit",
                       g."PriceDescription",
                       g."TermType",
                       g."LeaseContractLength",
                       g."PurchaseOption",
                       g."OfferingClass",
                       g."Current Generation",
                       g."Tenancy",
                       g."Operating System",
                       g."License Model"
                from grouped g
                         join "OnDemandPricing" p on
                            p."Instance Type" = g."Instance Type" and
                            p."Unit" = g."Unit" and
                            p."PriceDescription" = g."PriceDescription" and
                            p."TermType" = g."TermType" and
                            p."LeaseContractLength" = g."LeaseContractLength" and
                            p."PurchaseOption" = g."PurchaseOption" and
                            p."OfferingClass" = g."OfferingClass" and
                            p."Current Generation" = g."Current Generation" and
                            p."Tenancy" = g."Tenancy" and
                            p."Operating System" = g."Operating System" and
                            p."License Model" = g."License Model"
                where g."Instance Type" like 'g4%'
                  and g."Operating System" = 'RHEL'
                  and g."PurchaseOption" = 'No Upfront'
                  and g."Unit" = 'Hrs'
                  and (g."LeaseContractLength" = '1yr' or g."LeaseContractLength" = '1 yr'))
select *
from report r
where exists (select *
              from recent_price_changes rpc
              where r."Instance Type" = rpc."Instance Type"
                and r."Unit" = rpc."Unit"
                and r."PriceDescription" = rpc."PriceDescription"
                and r."TermType" = rpc."TermType"
                and r."LeaseContractLength" = rpc."LeaseContractLength"
                and r."PurchaseOption" = rpc."PurchaseOption"
                and r."OfferingClass" = rpc."OfferingClass"
                and r."Current Generation" = rpc."Current Generation"
                and r."Tenancy" = rpc."Tenancy"
                and r."Operating System" = rpc."Operating System"
                and r."License Model" = rpc."License Model"
                and r."EffectiveDate" = rpc."EffectiveDate")
order by abs(maxprice::decimal - minprice::decimal) desc,
         "Instance Type",
         "Unit",
         "PriceDescription",
         "TermType",
         "LeaseContractLength",
         "PurchaseOption",
         "OfferingClass",
         "Current Generation",
         "Tenancy",
         "Operating System",
         "License Model",
         "EffectiveDate"
