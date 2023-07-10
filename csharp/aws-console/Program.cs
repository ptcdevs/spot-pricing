// See https://aka.ms/new-console-template for more information

using System.Text.Json.Nodes;
using Amazon;
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
        // RegionEndpoint.USEast2,
        // RegionEndpoint.USWest1,
        // RegionEndpoint.USWest2,
    },
    new BasicAWSCredentials(
        config["aws:accessKey"],
        config["AWSSECRETKEY"]));

var instanceTypesText = File.ReadAllText("params/gpu-instances.json");
var instanceTypes = JsonNode.Parse(instanceTypesText)?
    .AsArray()
    .Select(node => node?.ToString())
    .Where(instanceType => instanceType != null)
    .ToList();

var dateTimes = Enumerable.Range(0, 90)
    .Select(i => DateTime.Today.AddDays(-1 * i))
    .SelectMany(d =>
    {
        return Enumerable.Range(0, 23)
            .Select(i => d.AddHours(i));
    })
    .OrderBy(d => d)
    .ToList();

var responses = await awsMultiClient
    .SampleSpotPricing(new DescribeSpotPriceHistoryRequest
        {
            StartTimeUtc = DateTime.Today.AddDays(-1),
            EndTimeUtc = DateTime.Today,
            Filters = new List<Filter>
            {
                new Filter("availability-zone",
                    new List<string> { "us-east-1a", "us-east-2a", "us-east-3a", "us-east-4a" }),
                new Filter("instance-type", instanceTypes.ToList()),
                new Filter("product-description",
                    new List<string>
                    {
                        "Linux/UNIX", "Red Hat Enterprise Linux ", "SUSE Linux ", "Windows ",
                        "Linux/UNIX (Amazon VPC) ",
                        "Red Hat Enterprise Linux (Amazon VPC) ", "SUSE Linux (Amazon VPC) ", "Windows (Amazon VPC)",
                    }),
            },
        },
        dateTimes.First(),
        dateTimes.First().AddHours(1)
    );
//TODO: standup database and start pushing results in

Console.WriteLine("fin");