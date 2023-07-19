using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Pricing;
using Amazon.Pricing.Model;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Util;
using aws_restapi.services;
using CsvHelper;
using Dapper;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using Serilog;
using Z.Dapper.Plus;
using Filter = Amazon.Pricing.Model.Filter;

namespace aws_restapi;

public class AwsMultiClient
{
    public IEnumerable<RegionEndpoint> RegionEndpoints { get; }
    public BasicAWSCredentials Credentials { get; }
    public IEnumerable<AmazonEC2Client> RegionalEc2Clients { get; }
    public IEnumerable<AmazonPricingClient> RegionalPricingClients { get; set; }

    public AwsMultiClient(IEnumerable<RegionEndpoint> regionEndpoints, BasicAWSCredentials credentials)
    {
        RegionEndpoints = regionEndpoints;
        Credentials = credentials;
        RegionalEc2Clients = RegionEndpoints
            .Select(endpoint => new AmazonEC2Client(Credentials, endpoint));
        RegionalPricingClients = RegionEndpoints
            .Where(e => e.SystemName.Equals("us-east-1"))
            .Select(endpoint => new AmazonPricingClient(Credentials, endpoint));
    }

    /// <summary>
    /// sample spot pricing history (since it's too much data to download exhaustively). makes sure to fetch a c
    /// </summary>
    /// <param name="req"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<IEnumerable<SpotPrice>> SampleSpotPricing(DescribeSpotPriceHistoryRequest req)
    {
        var maxQueriesPerEndpoint = 10;
        var instanceTypes = req.Filters
            .Where(filter => filter.Name.Equals("instance-type"))
            .SelectMany(filter => filter.Values);
        var productDescriptions = req.Filters
            .Where(filter => filter.Name.Equals("product-description"))
            .SelectMany(filter => filter.Values);
        var instanceTypesProductDescriptions = instanceTypes
            .SelectMany(instanceType =>
            {
                return productDescriptions
                    .Select(productDescription => new
                    {
                        instanceType,
                        productDescription
                    });
            })
            .ToList();
        var spotPricesTasks = RegionalEc2Clients
            .Select(async client =>
            {
                var resultTask = Enumerable.Range(0, maxQueriesPerEndpoint)
                    .AggregateUntilAsync(
                        new
                        {
                            NextRequest = req,
                            SpotPrices =
                                new List<Amazon.EC2.Model.SpotPrice>() as IEnumerable<Amazon.EC2.Model.SpotPrice>,
                            FetchCount = 0,
                            Audit = new[]
                            {
                                new
                                {
                                    FetchCount = 0,
                                    DistinctCount = 0,
                                }
                            }
                        },
                        async (aggregate, i) =>
                        {
                            var describeSpotPriceHistoryResponse =
                                await client.DescribeSpotPriceHistoryAsync((await aggregate).NextRequest);

                            var nextRequest = new DescribeSpotPriceHistoryRequest
                            {
                                StartTimeUtc = req.StartTimeUtc,
                                EndTimeUtc = req.EndTimeUtc,
                                NextToken = describeSpotPriceHistoryResponse.NextToken,
                                Filters = req.Filters,
                                MaxResults = req.MaxResults,
                            };

                            var accumulation = aggregate.Result.SpotPrices
                                .Concat(describeSpotPriceHistoryResponse.SpotPriceHistory);

                            return new
                            {
                                NextRequest = nextRequest,
                                SpotPrices = accumulation,
                                FetchCount = i + 1,
                                Audit = aggregate.Result.Audit
                                    .Concat(new[]
                                    {
                                        new
                                        {
                                            FetchCount = i + 1,
                                            DistinctCount = accumulation
                                                .DistinctBy(sp => new
                                                {
                                                    sp.InstanceType,
                                                    sp.ProductDescription
                                                })
                                                .Count()
                                        }
                                    })
                                    .OrderBy(fd => fd.FetchCount)
                                    .ToArray()
                            };
                        },
                        aggregate =>
                        {
                            var completed = aggregate.SpotPrices
                                .Where(price => req.StartTimeUtc < price.Timestamp && price.Timestamp < req.EndTimeUtc)
                                .Select(spotPrice => new
                                {
                                    instanceType = spotPrice.InstanceType.Value,
                                    productDescription = spotPrice.ProductDescription.Value
                                })
                                .Distinct()
                                .ToList();
                            var intersection = instanceTypesProductDescriptions
                                .Intersect(completed)
                                .ToList();
                            var missing = instanceTypesProductDescriptions
                                .Except(intersection)
                                .ToList();

                            // distinct combos of instance types + product descriptions
                            return intersection.Count >= instanceTypesProductDescriptions.Count ||
                                   aggregate.NextRequest.NextToken == null;
                        });

                var results = await resultTask;
                return results;
            });

        var spotPricesMany = await Task.WhenAll(spotPricesTasks);
        var awsSpotPrices = spotPricesMany
            .SelectMany(aggregate => aggregate.SpotPrices)
            .DistinctBy(sp => new
            {
                sp.InstanceType,
                sp.ProductDescription
            })
            .ToList();
        var spotPrices = awsSpotPrices
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

    public async Task<GetPriceListFileUrlResponse[]> GetPriceFileDownloadUrlsAsync()
    {
        var client = RegionalPricingClients.First();
        var serviceCode = "AmazonEC2";
        var formatVersion = "aws_v1";
        var regionCodes = new[] { "us-east-1", "us-east-2", "us-west-1", "us-west-2" };
        var currencyCode = "USD";
        var httpClient = new HttpClient();
        var versionIndexFileResponse = await httpClient
            .GetAsync("https://pricing.us-east-1.amazonaws.com/offers/v1.0/aws/AmazonEC2/index.json");
        var versionIndexJson = await versionIndexFileResponse.Content.ReadAsStringAsync();
        var versionIndex = JObject.Parse(versionIndexJson);
        var effectiveDates = versionIndex["versions"]
            .Select(jToken => ((JProperty)jToken).Name)
            .Select(effectiveDate =>
                DateTime.ParseExact(effectiveDate, "yyyyMMddHHmmss", CultureInfo.InvariantCulture).Date)
            .Where(effectiveDate => effectiveDate.Year >= 2023)
            .OrderByDescending(dt => dt)
            .ToList();
        var listPriceListsResponses = effectiveDates
            .Select(async effectiveDate =>
            {
                var priceListsResponse = await client.ListPriceListsAsync(
                    new ListPriceListsRequest()
                    {
                        MaxResults = 100,
                        ServiceCode = serviceCode,
                        CurrencyCode = currencyCode,
                        EffectiveDate = effectiveDate,
                        // RegionCode = regionCodes.First(),
                    });

                return new
                {
                    effectiveDate,
                    priceListsResponse
                };
            });
        var listPriceLists = await Task.WhenAll(listPriceListsResponses);
        var priceListArns = listPriceLists
            .SelectMany(priceList =>
                priceList
                    .priceListsResponse
                    .PriceLists
                    .Where(pl => regionCodes.Contains(pl.RegionCode))
                    .Select(pl => new
                    {
                        priceList.effectiveDate,
                        priceListArn = pl.PriceListArn,
                        regionCode = pl.RegionCode
                    }));
        var downloadUrlsResponse = priceListArns
            .Select(async priceListArn => await client.GetPriceListFileUrlAsync(new GetPriceListFileUrlRequest()
            {
                FileFormat = "csv",
                PriceListArn = priceListArn.priceListArn,
            }))
            .ToList();
        var downloadUrls = await Task.WhenAll(downloadUrlsResponse);
        return downloadUrls;
    }

    public async Task<IEnumerable<PriceSchedule>> PricingApiDemo()
    {
        var client = RegionalPricingClients.First();
        var serviceCode = "AmazonEC2";
        var formatVersion = "aws_v1";
        var regionCode = "us-east-1";
        var currencyCode = "USD";
        try
        {
            var gpuInstanceTypes = AwsParams.GetGpuInstances();
            var p4 = gpuInstanceTypes
                .Single(t => t.Contains("p4"));
            var describeServicesResponse = await client
                .DescribeServicesAsync(new DescribeServicesRequest()
                {
                    MaxResults = 1,
                    FormatVersion = formatVersion,
                    ServiceCode = serviceCode,
                    // NextToken = "",
                });
            var getProductsResponse = await client
                .GetProductsAsync(new GetProductsRequest()
                {
                    MaxResults = 100,
                    Filters = new List<Filter>
                    {
                        new Filter()
                        {
                            Field = "instanceType",
                            Type = FilterType.TERM_MATCH,
                            Value = p4,
                        },
                        new Filter()
                        {
                            Field = "regionCode",
                            Type = FilterType.TERM_MATCH,
                            Value = "us-east-1",
                        },
                    },
                    FormatVersion = formatVersion,
                    ServiceCode = serviceCode,
                    // NextToken = "",
                });
            var priceLists = getProductsResponse.PriceList;
            var getAttributeValuesResponse = await client.GetAttributeValuesAsync(new GetAttributeValuesRequest()
            {
                MaxResults = 1,
                ServiceCode = serviceCode,
                AttributeName = "operatingSystem"
                //NextToken = ""
            });
            var listPriceListsResponse = await client.ListPriceListsAsync(new ListPriceListsRequest()
            {
                MaxResults = 100,
                ServiceCode = serviceCode,
                CurrencyCode = currencyCode,
                EffectiveDate = DateTime.Parse("2023-07-01"),
                RegionCode = regionCode,
                // NextToken = "",
            });
            var getPriceListFileUrlResponse = await client.GetPriceListFileUrlAsync(new GetPriceListFileUrlRequest()
            {
                FileFormat = "csv",
                PriceListArn = listPriceListsResponse.PriceLists.First().PriceListArn,
            });
        }
        catch (Exception ex)
        {
            throw ex;
        }

        throw new NotImplementedException();
    }

    //public async Task<(int rowsInserted, string? csvHeader)> DownloadPriceFileAsync(
    public async Task<DownloadPriceFileResult> DownloadPriceFileAsync(
        GetPriceListFileUrlResponse priceFileDownloadUrl,
        DbConnectionStringBuilder connectionStringBuilder,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        try
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            await using var connection = new NpgsqlConnection(connectionStringBuilder.ToString());
            await connection.OpenAsync(cancellationToken);

            //start download of file
            var httpClient = new HttpClient();
            var response = await httpClient.GetStreamAsync(priceFileDownloadUrl.Url, cancellationToken);
            var tempFile = Path.GetTempFileName();
            Log.Information($"tempFile: {tempFile}");
            using var awsDownloadStreamReader = new StreamReader(response);

            // write csv to temp file
            await using (var outputFile = new StreamWriter(tempFile))
            {
                while (await awsDownloadStreamReader.ReadLineAsync() is { } line)
                {
                    await outputFile.WriteLineAsync(line);
                }
            }

            awsDownloadStreamReader.Close();

            // open file
            var fileLines = new StreamReader(tempFile);

            var boilerplate = Enumerable.Range(0, 5)
                .Select(line => fileLines.ReadLine())
                .ToList();
            var csvHeader = fileLines.ReadLine();
            var csvRecordsCreatedAt = DateTime.Now;
            var onDemandCsvFile = new OnDemandCsvFile()
            {
                CreatedAt = csvRecordsCreatedAt,
                Url = priceFileDownloadUrl.Url,
                Header = csvHeader,
            };
            connection.SingleInsert(onDemandCsvFile);
            var writer = await connection
                .BeginBinaryImportAsync(
                    @"COPY ""OnDemandCsvRows"" (""OnDemandCsvFilesId"", ""CreatedAt"", ""Row"") FROM STDIN (FORMAT BINARY) ",
                    cancellationToken);
            int i = 0;

            for (; await fileLines.ReadLineAsync() is { } line; i++)
            {
                await writer.StartRowAsync(cancellationToken);
                await writer.WriteAsync(onDemandCsvFile.Id, NpgsqlDbType.Bigint, cancellationToken);
                await writer.WriteAsync(csvRecordsCreatedAt, NpgsqlDbType.Timestamp, cancellationToken);
                await writer.WriteAsync(line, NpgsqlDbType.Text, cancellationToken);
                if (i % 100000 == 0) Console.WriteLine($"{i} rows inserted");
            }

            await writer.CompleteAsync(cancellationToken);
            await connection.CloseAsync();
            fileLines.Close();
            File.Delete(tempFile);

            Log.Information($"{i} rows bulk copied");

            stopWatch.Stop();
            return new DownloadPriceFileResult()
            {
                OnDemandCsvFile = onDemandCsvFile,
                RowsUploaded = i,
                TimeElapsed = stopWatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "error downloading price file from {Url}", priceFileDownloadUrl.Url);
            throw new NotImplementedException();
        }
    }

    public async Task<ParseOnDemandPricingResult> ParseOnDemandPricingAsync(long csvFileId, NpgsqlConnectionStringBuilder connectionStringBuilder)
    {
        var stopWatch = new Stopwatch(); 
        stopWatch.Start();
        await using var readConnection = new NpgsqlConnection(connectionStringBuilder.ToString());
        await using var writeConnection = new NpgsqlConnection(connectionStringBuilder.ToString());
        await readConnection.OpenAsync();
        await writeConnection.OpenAsync();
        var createCsvFileTempTableSql = await File.ReadAllTextAsync("sql/createCsvFileTempTable.sql");
        var csvFileTempTableCreationResult = await readConnection
            .ExecuteAsync(createCsvFileTempTableSql, new { Id = csvFileId });
        var pgCsvTextReader = await readConnection
            .BeginTextExportAsync("copy csvFile (line) TO STDOUT (FORMAT TEXT)");
        var createdAt = DateTimeOffset.Now;
        var bulkCopySql = await File.ReadAllTextAsync("sql/onDemandPricingBulkCopy.sql");
        var pgPricingBulkCopier = await writeConnection.BeginBinaryImportAsync(bulkCopySql);
        int recordsCopied = 0;
        using var csv = new CsvReader(pgCsvTextReader, CultureInfo.InvariantCulture);
        var records = csv.GetRecords<dynamic>();
        foreach (var record in records)
        {
            if (recordsCopied % 1000 == 0) Log.Information("{recordsCopied} records copied", recordsCopied);
            //convert to poco
            var recordDictionary = ((IEnumerable<KeyValuePair<string, object>>)record)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            var onDemandPrice = OnDemandPrice.Convert(recordDictionary);
            await OnDemandPrice.BulkCopy(pgPricingBulkCopier, createdAt, onDemandPrice);
            recordsCopied += 1;
            //write to bulk copier
            // await pgPricingBulkCopier.StartRowAsync();
            // await pgPricingBulkCopier.WriteAsync(createdAt.ToUniversalTime(), NpgsqlDbType.TimestampTz);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.SKU, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.OnDemandCsvFilesId, NpgsqlDbType.Bigint);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.OnDemandCsvRowsId, NpgsqlDbType.Bigint);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.OfferTermCode, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.RateCode, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.TermType, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.PriceDescription, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.EffectiveDate, NpgsqlDbType.Date);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.StartingRange, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.EndingRange, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.Unit, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.PricePerUnit, NpgsqlDbType.Money);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.Currency, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.RelatedTo, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.LeaseContractLength, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.PurchaseOption, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.OfferingClass, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.ProductFamily, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.serviceCode, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.Location, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.LocationType, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.InstanceType, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.CurrentGeneration, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.InstanceFamily, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.vCPU, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.PhysicalProcessor, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.ClockSpeed, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.Memory, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.Storage, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.NetworkPerformance, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.ProcessorArchitecture, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.Tenancy, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.OperatingSystem, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.LicenseModel, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.GPU, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.GpuMemory, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.instanceSKU, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.MarketOption, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.NormalizationSizeFactor, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.PhysicalCores, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.ProcessorFeatures, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.RegionCode, NpgsqlDbType.Text);
            // await pgPricingBulkCopier.WriteAsync(onDemandPrice.serviceName, NpgsqlDbType.Text);
        }

        await pgPricingBulkCopier.CompleteAsync();

        await readConnection.CloseAsync();
        await writeConnection.CloseAsync();
        
        stopWatch.Stop();
        return new ParseOnDemandPricingResult()
        {
            OnDemandCsvFileId = csvFileId,
            RecordsCopied = recordsCopied,
            TimeElapsed = stopWatch.Elapsed
        };
    }
}

public class ParseOnDemandPricingResult
{
    public long OnDemandCsvFileId { get; set; }
    public long RecordsCopied { get; init; }
    public TimeSpan TimeElapsed { get; init; }
}

public class DownloadPriceFileResult
{
    public OnDemandCsvFile OnDemandCsvFile { get; set; }
    public int RowsUploaded { get; set; }
    public TimeSpan TimeElapsed { get; set; }

    public string ToString()
    {
        return $"{OnDemandCsvFile.Url} downloaded; {RowsUploaded} uploaded; completed in {TimeElapsed}";
    }
}