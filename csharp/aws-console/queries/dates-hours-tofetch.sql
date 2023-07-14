with dates as (select date_trunc('day', dd):: date as queryDate
               from generate_series
                        (date(current_date - interval '90' day)
                        , current_date
                        , '1 day'::interval) dd),

     fetched as (select "StartTime"::date as queriedDate
                 from "QueriesRun"
                 where "Search" = 'GpuMlMain')

select d.queryDate 
from dates d
where d.queryDate not in (select queriedDate from fetched)