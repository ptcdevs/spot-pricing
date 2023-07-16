-- rtime: Sat, 15 Jul 2023 23:50:04 GMT
-- utime: 2023-07-15 23:50:04Z
select *
from "SpotPrices"
where "Timestamp"::date = '2023-07-15'
  and "AvailabilityZone" = 'us-east-1a'
  and "InstanceType" = 'g4dn.metal'
  and "ProductDescription" = 'Windows'
order by "Timestamp" desc;
