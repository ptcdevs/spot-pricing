with dates as (select date_trunc('day', dd):: date as day
               from generate_series
                        (date(current_date - interval '90' day)
                        , current_date
                        , '1 day'::interval) dd),
     hours as (select hour
               from generate_series(0, 23) hour),

     fetched as (select "StartTime" as starttime
                 from "QueriesRun"
                 where "Search" = 'GpuMlMain'),

     fetchable as (select d.day + (interval '1' hour * h.hour)                       as starttime
                   from dates d
                            cross join hours h)

select f.starttime
from fetchable f
where f.startTime not in (select startTime from fetched)
order by f.starttime asc
