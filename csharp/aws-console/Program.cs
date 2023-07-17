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
var effectiveDatesRegionsFetchedSql = File.ReadAllText("sql/effectiveDatesRegionsFetched.sql");
var effectiveDatesRegionsFetched = await connection.QueryAsync(effectiveDatesRegionsFetchedSql);
    
var priceFileUrlResponses = await awsMultiClient.GetPriceFileDownloadUrlsAsync();
var priceFileUrls = priceFileUrlResponses
    .Select(response =>
    {
        var url = new Uri(response.Url);
        var effectiveDateString = url.Segments[5].Substring(0, 14);
        var effectiveDate = DateTime.ParseExact(effectiveDateString, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var region = url.Segments[6].Substring(0, 9);
        return new
        {
            priceFileDownloadUrlResponse = response,
            region,
            effectiveDate = effectiveDate,
            effectiveDateString = effectiveDateString,
        };
    })
    .ToList();
var priceListFilUrlsToFetch = priceFileUrls
    .Where(pfu =>
    {
        return !effectiveDatesRegionsFetched
            .Select(ed => Tuple.Create(ed.effectivedate, ed.region))
            .Contains(Tuple.Create(pfu.effectiveDate, pfu.region));
    })
    .ToList();
var priceListFilUrlsToFetchSubset = priceListFilUrlsToFetch
    .Take(1)
    .ToList();
connection.BulkInsert<SpotPrice>(Array.Empty<SpotPrice>());
var downloads = priceFileUrlResponses
    .Select(async priceFileDownloadUrl => await awsMultiClient.DownloadPriceFileAsync(priceFileDownloadUrl))
    .ToList();

await Task.WhenAll(downloads);
Log.Information("fin");