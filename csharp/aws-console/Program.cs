// See https://aka.ms/new-console-template for more information

using System.Globalization;
using System.Text.RegularExpressions;
using Amazon;
using Amazon.Runtime;
using aws_restapi;
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
DapperPlusManager.Entity<OnDemandCsvFile>()
    .Table("OnDemandCsvFiles")
    .Identity(x => x.Id);
DapperPlusManager.Entity<OnDemandCsvRow>()
    .Table("OnDemandCsvRows")
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
// await awsMultiClient.PricingApiDemo();

await using var connection = new NpgsqlConnection(npgsqlConnectionStringBuilder.ToString());
var onDemandPriceUrlsFetchedSql = File.ReadAllText("sql/onDemandPriceUrlsFetched.sql");
var onDemandPriceUrlsFetched = await connection.QueryAsync<string>(onDemandPriceUrlsFetchedSql);
    
var priceFileUrlResponses = await awsMultiClient.GetPriceFileDownloadUrlsAsync();
var priceUrlsToFetch = priceFileUrlResponses
    .Where(resp => !onDemandPriceUrlsFetched.Contains(resp.Url))
    .ToList();
var priceUrlsToFetchSubset = priceUrlsToFetch
    .Where(puf => puf.Url.Contains("20230713184719"))
    .Take(1)
    .ToList();
Log.Information($"url: {priceUrlsToFetchSubset.Single().Url}");
var downloads = priceUrlsToFetchSubset
    .Select(async priceFileDownloadUrl => await awsMultiClient.DownloadPriceFileAsync(priceFileDownloadUrl, npgsqlConnectionStringBuilder))
    .ToList();

var downloadPriceFileResults = await Task.WhenAll(downloads);

Log.Information(downloadPriceFileResults.ToString());
Log.Information("fin");
