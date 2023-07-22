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
                 order by max("PricePerUnit") - min("PricePerUnit") desc)


select g."Instance Type",
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
and (g."LeaseContractLength" = '1yr' or g."LeaseContractLength" = '1 yr')
order by
    abs(g.maxprice::decimal - g.minprice::decimal) desc,
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
         p."EffectiveDate"
