with grouped as (select ODP."Instance Type",
                        min(ODP."PricePerUnit") minprice,
                        max(ODP."PricePerUnit") maxprice,
                        count(*) as             setCount,
                        ODP."Unit",
                        ODP."TermType",
                        ODP."LeaseContractLength",
                        ODP."PurchaseOption",
                        ODP."OfferingClass",
                        ODP."Current Generation",
                        ODP."Tenancy",
                        ODP."Operating System",
                        ODP."License Model"
                 from "OnDemandPricing" ODP
                 where ODP."Instance Family" = 'GPU instance'
                 group by ODP."Instance Type",
                          ODP."Unit",
                          ODP."TermType",
                          ODP."LeaseContractLength",
                          ODP."PurchaseOption",
                          ODP."OfferingClass",
                          ODP."Current Generation",
                          ODP."Tenancy",
                          ODP."Operating System",
                          ODP."License Model"),

     report as (select g."Instance Type",
                       odp."EffectiveDate",
                       odp."PricePerUnit",
                       g.minprice,
                       g.maxprice,
                       g.setCount,
                       g."Unit",
                       g."TermType",
                       g."LeaseContractLength",
                       g."PurchaseOption",
                       g."OfferingClass",
                       g."Current Generation",
                       g."Tenancy",
                       g."Operating System",
                       g."License Model"
                from grouped g
                         join "OnDemandPricing" odp on
                            g."Instance Type" = odp."Instance Type" and
                            g."Unit" = odp."Unit" and
                            g."TermType" = odp."TermType" and
                            g."LeaseContractLength" = odp."LeaseContractLength" and
                            g."PurchaseOption" = odp."PurchaseOption" and
                            g."OfferingClass" = odp."OfferingClass" and
                            g."Current Generation" = odp."Current Generation" and
                            g."Tenancy" = odp."Tenancy" and
                            g."Operating System" = odp."Operating System" and
                            g."License Model" = odp."License Model"
                where g."Unit" = 'Hrs'
--                   and g."Operating System" = 'RHEL'
--                   and g."PurchaseOption" = 'No Upfront'
--                   and g."Tenancy" = 'Dedicated'
--                   and (g."LeaseContractLength" = '1yr' or g."LeaseContractLength" = '1 yr')
                  and g.minprice <> g.maxprice),

     recent_changes as (select "Instance Type",
                               minprice,
                               maxprice,
                               setCount,
                               "Unit",
                               "TermType",
                               "LeaseContractLength",
                               "PurchaseOption",
                               "OfferingClass",
                               "Current Generation",
                               "Tenancy",
                               "Operating System",
                               "License Model"
                        from report
                        where "EffectiveDate" > '2022-01-01')
select *
from recent_changes
-- select "Instance Type",
--        "EffectiveDate",
--        "PricePerUnit",
--        minprice,
--        maxprice,
--        setCount,
--        "Unit",
--        "TermType",
--        "LeaseContractLength",
--        "PurchaseOption",
--        "OfferingClass",
--        "Current Generation",
--        "Tenancy",
--        "Operating System",
--        "License Model"
-- from report r
-- where exists (select true
--               from recent_changes rc
--               where r."Instance Type" = rc."Instance Type"
--                 and r.minprice = rc.minprice
--                 and r.maxprice = rc.maxprice
--                 and r.setCount = rc.setCount
--                 and r."Unit" = rc."Unit"
--                 and r."TermType" = rc."TermType"
--                 and r."LeaseContractLength" = rc."LeaseContractLength"
--                 and r."PurchaseOption" = rc."PurchaseOption"
--                 and r."OfferingClass" = rc."OfferingClass"
--                 and r."Current Generation" = rc."Current Generation"
--                 and r."Tenancy" = rc."Tenancy"
--                 and r."Operating System" = rc."Operating System"
--                 and r."License Model" = rc."License Model")
-- order by "Instance Type",
--          minprice,
--          maxprice,
--          setCount,
--          "Unit",
--          "TermType",
--          "LeaseContractLength",
--          "PurchaseOption",
--          "OfferingClass",
--          "Current Generation",
--          "Tenancy",
--          "Operating System",
--          "License Model",
--          r."EffectiveDate"
--
