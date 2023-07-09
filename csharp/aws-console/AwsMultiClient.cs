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
    }

    public async Task<IEnumerable<AvailabilityZone>> GetAvailabilityZonesList(DescribeAvailabilityZonesRequest req)
    {
        var availabilityZonesResponsesAsync = RegionalClients
            .Select(async client => await client.DescribeAvailabilityZonesAsync(req));
        var zonesResponses = await Task.WhenAll(availabilityZonesResponsesAsync);

        var availabilityZones = zonesResponses
            .SelectMany(response => response.AvailabilityZones);

        return availabilityZones;
    }
}