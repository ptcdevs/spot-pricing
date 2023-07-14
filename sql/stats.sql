select count(*) from "SpotPrices";

select "Timestamp"::date,
       count(*)
from "SpotPrices"
group by "Timestamp"::date
order by "Timestamp"::date

select "Timestamp"::date, count(*)
from "SpotPrices"
group by "Timestamp"::date
order by "Timestamp"::date;

select "InstanceType", count(*)
from "SpotPrices"
group by "InstanceType"
order by "InstanceType";

select *
from "SpotPrices"
order by "InstanceType", "ProductDescription", "Timestamp";

select *
from "SpotPrices"
where "InstanceType" = 'p4d.24xlarge'
order by "ProductDescription", "Timestamp";

select *
from "SpotPrices"
where "InstanceType" like 'p3%'
order by "InstanceType", "ProductDescription", "Timestamp";

select *
from "SpotPrices"
where "InstanceType" like 'g5%'
order by "InstanceType", "ProductDescription", "Timestamp";

select *
from "SpotPrices"
-- where "InstanceType" = 'g5.48xlarge'
order by "InstanceType", "ProductDescription", "Timestamp";

-- truncate table "SpotPrices";
-- truncate table "QueriesRun";

with idsNumbered as (select *,
                            row_number()
                            over (partition by "Timestamp", "AvailabilityZone", "InstanceType", "ProductDescription", "Price" order by "Id") as rownum
                     from "SpotPrices"),
     dupIds as (select *
                from idsNumbered
                where rownum > 1)

select *
from dupIds;