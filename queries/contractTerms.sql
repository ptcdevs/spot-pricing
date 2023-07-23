select "TermType",
       "LeaseContractLength",
       "PurchaseOption",
       "OfferingClass",
       "Current Generation",
       "Tenancy",
       "Operating System",
       "License Model"
from "OnDemandPricing"
where "Instance Family" = 'GPU instance'
group by "TermType",
         "LeaseContractLength",
         "PurchaseOption",
         "OfferingClass",
         "Current Generation",
         "Tenancy",
         "Operating System",
         "License Model"
