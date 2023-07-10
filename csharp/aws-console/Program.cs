// See https://aka.ms/new-console-template for more information

using System.Text.Json.Nodes;
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
var awsMultiClient = new AwsMultiClient(
    new[]
    {
        RegionEndpoint.USEast1,
        RegionEndpoint.USEast2,
        RegionEndpoint.USWest1,
        RegionEndpoint.USWest2,
    },
    new BasicAWSCredentials(
        config["aws:accessKey"],
        config["AWSSECRETKEY"]));

var availabilityZones = await awsMultiClient
    .GetAvailabilityZonesList(
    new DescribeAvailabilityZonesRequest()
    {
        AllAvailabilityZones = true,
    });
var instanceTypesText = File.ReadAllText("params/gpu-instances.json");
var instanceTypes = JsonNode.Parse(instanceTypesText)?
    .AsArray()
    .Select(node => node?.ToString())
    .Where(instanceType => instanceType != null)
    .ToList();
var spotPriceHistoryQueryFilter = new List<Filter>
{
    new Filter("instance-type", instanceTypes),
};
var responses = await awsMultiClient.GetAvailabilityZonesList()
// var availabilityZones = initialSpotPriceHistoryResponse.SpotPriceHistory
//     .Select(sph => sph.AvailabilityZone)
//     .Distinct()
//     .OrderBy(s => s)
//     .ToList();
//
// var spotPriceHistory = Enumerable
//     .Range(0, Int32.MaxValue)
//     .AggregateUntil(
//         new
//         {
//             entries = initialSpotPriceHistoryResponse.SpotPriceHistory as IEnumerable<SpotPrice>,
//             totalEntries = initialSpotPriceHistoryResponse.SpotPriceHistory.Count(),
//             nextToken = initialSpotPriceHistoryResponse.NextToken,
//         }, (accumulatedPage, pageIndex) =>
//         {
//             var asdf = awsClient.DescribeSpotPriceHistoryAsync(new DescribeSpotPriceHistoryRequest()
//             {
//                 NextToken = accumulatedPage.nextToken,
//             });
//
//             return accumulatedPage;
//         },
//         (accumulatedPage) => { return true; }
//     );

Console.WriteLine("fin");