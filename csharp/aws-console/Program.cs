// See https://aka.ms/new-console-template for more information

using System.Text.Json.Nodes;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime;
using aws_console;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Serilog;
using Z.Dapper.Plus;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Log.Information("start");

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();
var npgsqlConnectionStringBuilder = new NpgsqlConnectionStringBuilder()
{
    Host = config["postgres:host"],
    Port = int.Parse(config["postgres:port"] ?? string.Empty),
    Database = config["postgres:database"],
    Username = config["postgres:username"],
    Password = config["POSTGRESQL_PASSWORD"],
    SslMode = SslMode.VerifyCA,
    RootCertificate = "data/ptcdevs-psql-ca-certificate.crt",
};
DapperPlusManager.Entity<SpotPrice>()
    .Table("SpotPrices")
    .Identity(x => x.Id);
DapperPlusManager.Entity<QueryRun>()
    .Table("QueriesRun")
    .Identity(x => x.Id);

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

var instanceTypesText = File.ReadAllText("params/gpu-instances.json");
var instanceTypes = JsonNode.Parse(instanceTypesText)?
    .AsArray()
    .Select(node => node?.ToString())
    .Where(instanceType => instanceType != null)
    .ToList();

await using var connection = new NpgsqlConnection(npgsqlConnectionStringBuilder.ToString());
await connection.OpenAsync();

var startEndTimesQuery = File.ReadAllText("queries/dates-hours-tofetch.sql");
var startEndTimes = connection.Query(startEndTimesQuery)
    .OrderBy(ts => ts.starttime)
    .Take(1);
var semaphore = new SemaphoreSlim(4);
var results = startEndTimes
    .Select(async startEndTime =>
    {
        try
        {
            semaphore.Wait();
            var starttime = startEndTime.starttime is DateTime ? (DateTime)startEndTime.starttime : default;
            var endtime = startEndTime.endtime is DateTime ? (DateTime)startEndTime.endtime : default;
            var responses = await awsMultiClient
                .SampleSpotPricing(new DescribeSpotPriceHistoryRequest
                    {
                        MaxResults = 10000,
                        StartTimeUtc = starttime,
                        EndTimeUtc = endtime,
                        Filters = new List<Filter>
                        {
                            new("availability-zone", new List<string> { "us-east-1a", "us-east-2a", "us-west-1a", "us-west-2a" }),
                            new("instance-type", instanceTypes.ToList()),
                            new("product-description",
                                new List<string>
                                {
                                    "Linux/UNIX",
                                    "Red Hat Enterprise Linux",
                                    "SUSE Linux",
                                    "Windows",
                                    "Linux/UNIX (Amazon VPC)",
                                    "Red Hat Enterprise Linux (Amazon VPC)",
                                    "SUSE Linux (Amazon VPC)",
                                    "Windows (Amazon VPC)",
                                }),
                        },
                    }
                );
            var spotPrices = responses
                .Select(response =>
                {
                    var spotPrice = new SpotPrice()
                    {
                        Price = decimal.Parse(response.Price),
                        Timestamp = response.Timestamp,
                        AvailabilityZone = response.AvailabilityZone,
                        InstanceType = response.InstanceType,
                        ProductDescription = response.ProductDescription
                    };
                    return spotPrice;
                });
            return spotPrices;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"error while querying startTime: {startEndTime.starttime}, endTime: {startEndTime.endtime}");
            throw ex;
        }
        finally
        {
            semaphore.Release();
        }
    });

var spotPrices = await Task.WhenAll(results);
var queriesRun = startEndTimes
    .Select(set => new QueryRun()
    {
        Search = "GpuMlMain",
        StartTime = set.starttime,
    });

await using var tx = connection.BeginTransaction();
tx.BulkInsert(queriesRun);
tx.BulkInsert(spotPrices);
await tx.CommitAsync();

//TODO: standup database and start pushing results in

Log.Information($"ran {string.Join(",",queriesRun.Select(q => q.StartTime.ToString("u")))}");
Log.Information("fin");
