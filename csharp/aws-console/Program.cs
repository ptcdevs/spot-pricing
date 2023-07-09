// See https://aka.ms/new-console-template for more information

using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime;
using aws_console;
using Microsoft.Extensions.Configuration;

Console.WriteLine("start");

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

using var awsClient = new AmazonEC2Client(
    new BasicAWSCredentials(config["aws:accessKey"], config["AWSSECRETKEY"]), RegionEndpoint.USEast1);
//TODO: create wrapper service that will run same query against separate regional endpoints
var regionsResult = await awsClient.DescribeRegionsAsync();
var regions = regionsResult.Regions
    .Select(r => r.RegionName)
    .OrderBy(s => s)
    .ToList();

var usEast1ZonesAsync = await awsClient
    .DescribeAvailabilityZonesAsync(new DescribeAvailabilityZonesRequest()
    {
        AllAvailabilityZones = true,
        Filters = new List<Filter>
        {
            new Filter("region-name", new[] { "us-east-1" }.ToList())
        }
    });
var usEast1Zones = usEast1ZonesAsync
    .AvailabilityZones
    .Select(availZone => availZone.ZoneName)
    .OrderBy(s => s);
var usEast2ZonesAsync = await awsClient
    .DescribeAvailabilityZonesAsync(new DescribeAvailabilityZonesRequest()
    {
        // AllAvailabilityZones = true,
        Filters = new List<Filter>
        {
            new Filter("region-name", new[] { "us-east-2" }.ToList())
        }
    });
var usEast2Zones = usEast1ZonesAsync
    .AvailabilityZones
    .Select(availZone => availZone.ZoneName)
    .OrderBy(s => s);
var spotPriceHistoryQueryFilter = new List<Filter>
{
    new Filter("instance-type", new[] { "", "" }.ToList()),
    new Filter("", new[] { "", "" }.ToList())
};
var initialSpotPriceHistoryResponse = await awsClient.DescribeSpotPriceHistoryAsync();
var availabilityZones = initialSpotPriceHistoryResponse.SpotPriceHistory
    .Select(sph => sph.AvailabilityZone)
    .Distinct()
    .OrderBy(s => s)
    .ToList();

var spotPriceHistory = Enumerable
    .Range(0, Int32.MaxValue)
    .AggregateUntil(
        new
        {
            entries = initialSpotPriceHistoryResponse.SpotPriceHistory as IEnumerable<SpotPrice>,
            totalEntries = initialSpotPriceHistoryResponse.SpotPriceHistory.Count(),
            nextToken = initialSpotPriceHistoryResponse.NextToken,
        }, (accumulatedPage, pageIndex) =>
        {
            var asdf = awsClient.DescribeSpotPriceHistoryAsync(new DescribeSpotPriceHistoryRequest()
            {
                NextToken = accumulatedPage.nextToken,
            });

            return accumulatedPage;
        },
        (accumulatedPage) => { return true; }
    );

Console.WriteLine("fin");