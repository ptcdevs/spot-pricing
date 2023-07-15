using System.Diagnostics;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Pricing;
using Amazon.Pricing.Model;
using Amazon.Runtime;

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

    public async Task<IEnumerable<PriceSchedule>> GetOnDemandPricing()
    {
        var client = RegionalPricingClients.First();
        var describeServicesResponse = await client
            .DescribeServicesAsync(new DescribeServicesRequest()
            {
                MaxResults = 0,
                NextToken = "",
                FormatVersion = "",
                ServiceCode = "",
            });
        var getProductsResponse = await client
            .GetProductsAsync(new GetProductsRequest()
            {
                MaxResults = 0,
                NextToken = "",
                FormatVersion = "",
                Filters = new List<Amazon.Pricing.Model.Filter>(),
                ServiceCode = "",
            });
        var listPriceListsResponse = await client.ListPriceListsAsync(new ListPriceListsRequest()
        {
            MaxResults = 0,
            NextToken = "",
            ServiceCode = "",
            CurrencyCode = "",
            EffectiveDate = DateTime.Now,
            RegionCode = "",
        });
        var getPriceListFileUrlResponse = await client.GetPriceListFileUrlAsync(new GetPriceListFileUrlRequest()
        {
            FileFormat = "",
            PriceListArn = "",
        });
        throw new NotImplementedException();
    }
}