// See https://aka.ms/new-console-template for more information

using System.Globalization;
using Amazon;
using Amazon.Runtime;
using aws_restapi;
using CsvHelper;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;
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
// DapperPlusManager.Entity<SpotPrice>()
//     .Table("SpotPrices")
//     .Identity(x => x.Id);
// DapperPlusManager.Entity<QueryRun>()
//     .Table("QueriesRun")
//     .Identity(x => x.Id);
// DapperPlusManager.Entity<OnDemandCsvFile>()
//     .Table("OnDemandCsvFiles")
//     .Identity(x => x.Id);
// DapperPlusManager.Entity<OnDemandCsvRow>()
//     .Table("OnDemandCsvRows")
//     .Identity(x => x.Id);

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

await using var readConnection = new NpgsqlConnection(npgsqlConnectionStringBuilder.ToString());
await using var writeConnection = new NpgsqlConnection(npgsqlConnectionStringBuilder.ToString());
readConnection.Open();
writeConnection.Open();
var createCsvFileTempTableSql = File.ReadAllText("sql/createCsvFileTempTable.sql");
var csvFileId = 73;
var result = readConnection.Execute(createCsvFileTempTableSql, new { Id = csvFileId });
var pgCsvTextReader = readConnection
    .BeginTextExport("copy csvFile (line) TO STDOUT (FORMAT TEXT)");
var createdAt = DateTime.Now;
var bulkCopySql = File.ReadAllText("sql/onDemandPricingBulkCopy.sql");
var pgPricingBulkCopier = writeConnection.BeginBinaryImport(bulkCopySql);
using (var csv = new CsvReader(pgCsvTextReader, CultureInfo.InvariantCulture))
{
    var records = csv.GetRecords<dynamic>();
    int i = 0;
    foreach (var record in records)
    {
        if(i++ % 1000 == 0) Console.WriteLine($"{i} records copied");
        //convert to poco
        var recordDictionary = ((IEnumerable<KeyValuePair<string, object>>)record)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        var onDemandPrice = OnDemandPrice.Convert(csvFileId, createdAt, recordDictionary);

        //write to bulk copier
        await pgPricingBulkCopier.StartRowAsync();
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.CreatedAt.ToUniversalTime(), NpgsqlDbType.TimestampTz);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.SKU, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.OnDemandCsvFilesId, NpgsqlDbType.Bigint);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.OnDemandCsvRowsId, NpgsqlDbType.Bigint);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.OfferTermCode, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.RateCode, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.TermType, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.PriceDescription, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.EffectiveDate, NpgsqlDbType.Date);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.StartingRange, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.EndingRange, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.Unit, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.PricePerUnit, NpgsqlDbType.Money);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.Currency, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.RelatedTo, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.LeaseContractLength, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.PurchaseOption, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.OfferingClass, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.ProductFamily, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.serviceCode, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.Location, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.LocationType, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.InstanceType, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.CurrentGeneration, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.InstanceFamily, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.vCPU, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.PhysicalProcessor, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.ClockSpeed, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.Memory, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.Storage, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.NetworkPerformance, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.ProcessorArchitecture, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.Tenancy, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.OperatingSystem, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.LicenseModel, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.GPU, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.GpuMemory, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.instanceSKU, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.MarketOption, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.NormalizationSizeFactor, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.PhysicalCores, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.ProcessorFeatures, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.RegionCode, NpgsqlDbType.Text);
        await pgPricingBulkCopier.WriteAsync(onDemandPrice.serviceName, NpgsqlDbType.Text);
    }

    await pgPricingBulkCopier.CompleteAsync();
}

await readConnection.CloseAsync();

Log.Information("fin");