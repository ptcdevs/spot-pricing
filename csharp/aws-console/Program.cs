// See https://aka.ms/new-console-template for more information

using System.Globalization;
using Amazon;
using Amazon.Runtime;
using aws_restapi;
using CsvHelper;
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

await using var connection = new NpgsqlConnection(npgsqlConnectionStringBuilder.ToString());
connection.Open();
var createCsvFileTempTableSql = File.ReadAllText("sql/createCsvFileTempTable.sql");
var csvFileId = 73;
var result = connection.Execute(createCsvFileTempTableSql, new { Id = csvFileId });
var pgCsvTextReader = connection
    .BeginTextExport("copy csvFile (line) TO STDOUT (FORMAT TEXT)");
// var pgPricingBulkCopier = connection.BeginBinaryImport("");
using (var csv = new CsvReader(pgCsvTextReader, CultureInfo.InvariantCulture))
{
    var records = csv.GetRecords<dynamic>();
    foreach (var record in records)
    {
        //convert to poco
        var recordDictionary = ((IEnumerable<KeyValuePair<string, object>>)record)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        var onDemandPrice = new OnDemandPrice()
        {
            SKU = recordDictionary["SKU"].ToString(),
            CreatedAt = DateTime.Now,
            OnDemandCsvRowsId = csvFileId,
            OfferTermCode = recordDictionary["OfferTermCode"].ToString(),
            RateCode = recordDictionary["RateCode"].ToString(),
            TermType = recordDictionary["TermType"].ToString(),
            PriceDescription = recordDictionary["PriceDescription"].ToString(),
            EffectiveDate = recordDictionary["EffectiveDate"].ToString(),
            StartingRange = recordDictionary["StartingRange"].ToString(),
            EndingRange = recordDictionary["EndingRange"].ToString(),
            Unit = recordDictionary["Unit"].ToString(),
            PricePerUnit = decimal.Parse(recordDictionary["PricePerUnit"].ToString()),
            Currency = recordDictionary["Currency"].ToString(),
            RelatedTo = recordDictionary["RelatedTo"].ToString(),
            LeaseContractLength = recordDictionary["LeaseContractLength"].ToString(),
            PurchaseOption = recordDictionary["PurchaseOption"].ToString(),
            OfferingClass = recordDictionary["OfferingClass"].ToString(),
            ProductFamily = recordDictionary["Product Family"].ToString(),
            serviceCode = recordDictionary["serviceCode"].ToString(),
            Location = recordDictionary["Location"].ToString(),
            LocationType = recordDictionary["Location Type"].ToString(),
            InstanceType = recordDictionary["Instance Type"].ToString(),
            CurrentGeneration = recordDictionary["Current Generation"].ToString(),
            InstanceFamily = recordDictionary["Instance Family"].ToString(),
            vCPU = recordDictionary["vCPU"].ToString(),
            PhysicalProcessor = recordDictionary["Physical Processor"].ToString(),
            ClockSpeed = recordDictionary["Clock Speed"].ToString(),
            Memory = recordDictionary["Memory"].ToString(),
            Storage = recordDictionary["Storage"].ToString(),
            NetworkPerformance = recordDictionary["Network Performance"].ToString(),
            ProcessorArchitecture = recordDictionary["Processor Architecture"].ToString(),
            Tenancy = recordDictionary["Tenancy"].ToString(),
            OperatingSystem = recordDictionary["Operating System"].ToString(),
            LicenseModel = recordDictionary["License Model"].ToString(),
            GPU = recordDictionary["GPU"].ToString(),
            GpuMemory = recordDictionary["GPU Memory"].ToString(),
            instanceSKU = recordDictionary["instanceSKU"].ToString(),
            MarketOption = recordDictionary["MarketOption"].ToString(),
            NormalizationSizeFactor = recordDictionary["Normalization Size Factor"].ToString(),
            PhysicalCores = recordDictionary["Physical Cores"].ToString(),
            ProcessorFeatures = recordDictionary["Processor Features"].ToString(),
            RegionCode = recordDictionary["Region Code"].ToString(),
            serviceName = recordDictionary["serviceName"].ToString(),
        };
        
        //write to bulk copier
        Log.Information("here");
    }
}

Log.Information("fin");