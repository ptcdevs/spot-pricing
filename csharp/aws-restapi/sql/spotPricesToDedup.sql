with idsNumbered as (select "Id",
                            row_number()
                            over (partition by
                                "Timestamp", "AvailabilityZone", "InstanceType", "ProductDescription", "Price"
                                order by "Id") as rownum
                     from "SpotPrices"),
     dupIds as (select "Id"
                from idsNumbered
                where rownum > 1)

select count(*)
from "SpotPrices"
where "Id" in (select dupIds."Id" from dupIds)