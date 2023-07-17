using System.Data.Common;
using System.Globalization;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Pricing;
using Amazon.Pricing.Model;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Util;
using aws_restapi.services;
using Dapper;
using Newtonsoft.Json.Linq;
using Npgsql;
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
    public async Task DownloadPriceFileAsync(
        GetPriceListFileUrlResponse priceFileDownloadUrl,
        DbConnectionStringBuilder connectionStringBuilder,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        await using var connection = new NpgsqlConnection(connectionStringBuilder.ToString());
        await connection.OpenAsync(cancellationToken);
        var httpClient = new HttpClient();
        var response = await httpClient.GetStreamAsync(priceFileDownloadUrl.Url, cancellationToken);
        var tempFile = Path.GetTempFileName();
        Log.Information($"tempFile: {tempFile}");
        using var streamRdr = new StreamReader(response);
        // using var outputFile = new StreamWriter(tempFile);
        // while (await streamRdr.ReadLineAsync() is { } line)
        // {
        //     await outputFile.WriteLineAsync(line);
        // }

        var boilerplate = Enumerable.Range(0, 5)
            .Select(i => streamRdr.ReadLine())
            .ToList();
        var csvHeader = await streamRdr.ReadLineAsync();
        var csvRecordsCreatedAt = DateTime.Now;
        var onDemandCsvFile = new OnDemandCsvFile()
        {
            CreatedAt = csvRecordsCreatedAt,
            Url = priceFileDownloadUrl.Url,
            Header = csvHeader,
        };
        connection.SingleInsert(onDemandCsvFile);
        var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var bufferSize = 100000;
        var lines = new string[bufferSize + 1];
        for (var i = 0; await streamRdr.ReadLineAsync() is { } line; i++)
        {
            lines[i] = line;
            if (i == bufferSize)
            {
                // var insertSql = @"insert into ""OnDemandCsvRows""(""CreatedAt"", ""OnDemandCsvFilesId"", ""Row"")" +
                //                 " values (@CreatedAt, @OnDemandCsvFilesId, @Row)";
                // await connection
                //     .ExecuteAsync(insertSql,
                //         new
                //         {
                //             CreatedAt = csvRecordsCreatedAt,
                //             OnDemandCsvFilesId = onDemandCsvFile.Id,
                //             Row = line
                //         },
                //         transaction);
                transaction.BulkInsert(lines.Select(l => new OnDemandCsvRow()
                {
                    CreatedAt = csvRecordsCreatedAt,
                    OnDemandCsvFilesId = onDemandCsvFile.Id,
                    Row = l,
                }));
                Console.WriteLine($"{bufferSize} lines inserted");
                i = 0;
            }
        }
        transaction.BulkInsert(lines.Select(l => new OnDemandCsvRow()
        {
            CreatedAt = csvRecordsCreatedAt,
            OnDemandCsvFilesId = onDemandCsvFile.Id,
            Row = l,
        }));
        Console.WriteLine($"{lines.Count()} lines inserted");

        await transaction.CommitAsync(cancellationToken);
        //     var leftovers = Enumerable.Range(0, int.MaxValue)
        //         .AggregateUntilAsync(
        //             seed: new
        //             {
        //                 nextLine = await streamRdr.ReadLineAsync(),
        //                 uncommittedLines = (IEnumerable<string>)new List<string>() { },
        //                 rowsInserted = 0,
        //             },
        //             // if there are 500 uncommitted lines, push to db and remove uncommitted lines from aggregate
        //             // read new line into aggregate.nextLine
        //             // append old aggregate.nextLine to uncommited lines
        //             func: async (aggregateTask, i) =>
        //             {
        //                 var aggregate = await aggregateTask;
        //                 var linesToUpload = aggregate.uncommittedLines.Count() >= 100
        //                     ? aggregate.uncommittedLines
        //                     : Array.Empty<string>();
        //                 var rowsToUpload = linesToUpload
        //                     .Select(line => new OnDemandCsvRow()
        //                     {
        //                         OnDemandCsvFilesId = onDemandCsvFileId,
        //                         CreatedAt = csvRecordsCreatedAt,
        //                         Row = line
        //                     });
        //                 var task = connection
        //                     .BulkActionAsync(x => x.BulkInsert(rowsToUpload), cancellationToken);
        //                 await task.WaitAsync(cancellationToken);
        //                 Log.Debug($"{linesToUpload.Count()} rows bulk inserted");
        //
        //                 return new
        //                 {
        //                     nextLine = await streamRdr.ReadLineAsync(),
        //                     uncommittedLines = aggregate.uncommittedLines
        //                         .Except(linesToUpload)
        //                         .Append(aggregate.nextLine),
        //                 rowsInserted = aggregate.rowsInserted + rowsToUpload.Count(),
        //                 };
        //             },
        //             untilFunc: aggregate => aggregate.nextLine == null);
        //
        //     //upload remaining rows
        //     var rowsToUpload = (await leftovers).uncommittedLines
        //         .Select(line => new OnDemandCsvRow()
        //         {
        //             OnDemandCsvFilesId = onDemandCsvFileId,
        //             CreatedAt = csvRecordsCreatedAt,
        //             Row = line
        //         });
        //     connection.BulkInsert(rowsToUpload);
        //     
        //     return ((await leftovers).rowsInserted, csvHeader);
    }
}