// See https://aka.ms/new-console-template for more information

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


var connection = new NpgsqlConnection(npgsqlConnectionStringBuilder.ToString());
var effectiveDatesFetchedSql = File.ReadAllText("sql/effectiveDatesFetched.sql");
var effectiveDatesFetchedl = connection.Query(effectiveDatesFetchedSql)
    .ToList();
var priceFileDownloadUrls = await awsMultiClient.GetPriceFileDownloadUrlsAsync();
var downloads = priceFileDownloadUrls
    .Select(async priceFileDownloadUrl => await awsMultiClient.DownloadPriceFileAsync(priceFileDownloadUrl.Url))
    .ToList();

await Task.WhenAll(downloads);
Log.Information("fin");