using System.Diagnostics;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime;

namespace aws_console;

public class AwsMultiClient
{
    public IEnumerable<RegionEndpoint> RegionEndpoints { get; }
    public BasicAWSCredentials Credentials { get; }
    public IEnumerable<AmazonEC2Client> RegionalClients { get; }

    public AwsMultiClient(IEnumerable<RegionEndpoint> regionEndpoints, BasicAWSCredentials credentials)
    {
        RegionEndpoints = regionEndpoints;
        Credentials = credentials;
        RegionalClients = RegionEndpoints
            .Select(endpoint => new AmazonEC2Client(Credentials, endpoint));
        //TODO: check creds for each client
    }

    public async Task<IEnumerable<AvailabilityZone>> GetAvailabilityZonesList(DescribeAvailabilityZonesRequest req)
    {
        var availabilityZonesResponsesAsync = RegionalClients
            .Select(async client =>
            {
                try
                {
                    DescribeAvailabilityZonesResponse? response = await client.DescribeAvailabilityZonesAsync(req);
                    return new DescribeAvailabilityZonesMultiResponse()
                    {
                        Client = client,
                        Response = response,
                    };
                }
                catch (AmazonEC2Exception ex)
                {
                    return new DescribeAvailabilityZonesMultiResponse()
                    {
                        Client = client,
                        Exception = ex,
                    };
                }
            });
        var zonesResponses = await Task.WhenAll(availabilityZonesResponsesAsync);

        var availabilityZones = zonesResponses
            .SelectMany(response => response.Response?.AvailabilityZones ?? new List<AvailabilityZone>())
            .Where(zone => zone.ZoneType.Equals("availability-zone"));

        return availabilityZones;
    }

    /// <summary>
    /// exhaustive fetch of spot price history; deprecated b/c not feasible to fetch all pricing history
    /// </summary>
    /// <param name="req">request paramaters for spot price history request</param>
    /// <returns>all spot price history</returns>
    public async Task<IEnumerable<SpotPrice>> GetSpotPricing(DescribeSpotPriceHistoryRequest req)
    {
        var initialSpotPriceResponsesAsync = RegionalClients
            .Select(async client =>
            {
                try
                {
                    var response = await client.DescribeSpotPriceHistoryAsync(req);
                    return new DescribeSpotPriceHistoryMultiResponse()
                    {
                        Client = client,
                        Response = response,
                    };
                }
                catch (AmazonEC2Exception ex)
                {
                    return new DescribeSpotPriceHistoryMultiResponse()
                    {
                        Client = client,
                        Exception = ex,
                    };
                }
            });
        var spotPricesTasks = initialSpotPriceResponsesAsync
            .Select(async response =>
            {
                var resultTask = Enumerable.Range(0, 10000)
                    .AggregateUntilAsync(
                        new
                        {
                            response.Result.Response?.NextToken,
                            SpotPrices = response.Result.Response?.SpotPriceHistory ??
                                         new List<SpotPrice>() as IEnumerable<SpotPrice>,
                        }, async (task, i) =>
                        {
                            var completedTask = await task;
                            var windowsPrices = completedTask.SpotPrices
                                .Where(sp => sp.ProductDescription.Value.Equals("Windows"))
                                .OrderBy(sp => sp.Timestamp)
                                .ToList();
                            var windowsDistinctPrices = windowsPrices
                                .Select(sp => sp.Price)
                                .Distinct()
                                .ToList();

                            Console.WriteLine(
                                $"fetch #: {i};\t " +
                                $"distinct spots: {completedTask.SpotPrices.Distinct().Count()};\t " +
                                $"min date: {completedTask.SpotPrices.Min(spotPrice => spotPrice.Timestamp.ToString("u"))};\t " +
                                $"max date: {completedTask.SpotPrices.Max(spotPrice => spotPrice.Timestamp.ToString("u"))};\t" +
                                $"windows prices: {windowsDistinctPrices};");
                            var nextBatch = await response.Result.Client
                                .DescribeSpotPriceHistoryAsync(new DescribeSpotPriceHistoryRequest
                                {
                                    StartTimeUtc = req.StartTimeUtc,
                                    EndTimeUtc = req.EndTimeUtc,
                                    NextToken = completedTask.NextToken,
                                    Filters = req.Filters
                                });

                            return new
                            {
                                nextBatch?.NextToken,
                                SpotPrices =
                                    completedTask.SpotPrices.Concat(
                                        nextBatch?.SpotPriceHistory ?? new List<SpotPrice>())
                            };
                        },
                        arg => { return arg.NextToken == null; });

                var results = (await resultTask).SpotPrices.ToList();
                return results;
            });

        var spotPricesMany = await Task.WhenAll(spotPricesTasks);
        var spotPrices = spotPricesMany
            .SelectMany(c => c)
            .ToList();

        return spotPrices;
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
        var spotPricesTasks = RegionalClients
            .Select(async client =>
            {
                var resultTask = Enumerable.Range(0, 15)
                    .AggregateUntilAsync(
                        new
                        {
                            NextRequest = req,
                            SpotPrices = new List<SpotPrice>() as IEnumerable<SpotPrice>,
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
                                Filters = req.Filters
                            };

                            var accumulation = aggregate.Result.SpotPrices
                                .Concat(describeSpotPriceHistoryResponse.SpotPriceHistory);

                            return new
                            {
                                NextRequest = nextRequest,
                                SpotPrices = accumulation,
                                Audit = aggregate.Result.Audit
                                    .Concat(new[]
                                    {
                                        new
                                        {
                                            FetchCount = i,
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

                            //TODO: probably just replace this with a minimum dataset spanning 25 or so
                            // distinct combos of instance types + product descriptions
                            return intersection.Count >= instanceTypesProductDescriptions.Count ||
                                   aggregate.NextRequest.NextToken == null;
                        });

                var results = await resultTask;
                return results;
            });

        var spotPricesMany = await Task.WhenAll(spotPricesTasks);
        var spotPrices = spotPricesMany
            .SelectMany(aggregate => aggregate.SpotPrices)
            .DistinctBy(sp => new
            {
                sp.InstanceType,
                sp.ProductDescription
            })
            .ToList();

        return spotPrices;
    }
}

public class DescribeAvailabilityZonesMultiResponse
{
    public AmazonEC2Client Client { get; init; }
    public DescribeAvailabilityZonesResponse? Response { get; init; }
    public AmazonEC2Exception? Exception { get; init; }
}

public class DescribeSpotPriceHistoryMultiResponse
{
    public AmazonEC2Client Client { get; init; }
    public DescribeSpotPriceHistoryResponse? Response { get; init; }
    public AmazonEC2Exception? Exception { get; init; }
}