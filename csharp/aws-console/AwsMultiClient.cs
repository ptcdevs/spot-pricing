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
    public async Task<IEnumerable<SpotPrice>> GetSpotPricing(DescribeSpotPriceHistoryRequest req)
    {
        var availabilityZonesResponsesAsync = RegionalClients
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
        var spotPriceResponses = await Task.WhenAll(availabilityZonesResponsesAsync);

        var spotPrices = spotPriceResponses
            .SelectMany(response => response.Response?.SpotPriceHistory ?? new List<SpotPrice>());
        
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
