using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime;

namespace aws_console;

public class AwsMultiClient
{
    public List<RegionEndpoint> RegionEndpoints { get; }
    public BasicAWSCredentials Credentials { get; }
    public List<AmazonEC2Client> RegionalClients { get; }

    public AwsMultiClient(List<RegionEndpoint> regionEndpoints, BasicAWSCredentials credentials)
    {
        RegionEndpoints = regionEndpoints;
        Credentials = credentials;
        RegionalClients = RegionEndpoints
            .Select(endpoint => new AmazonEC2Client(Credentials, endpoint))
            .ToList();
    }

    public List<AvailabilityZone> GetAvailabilityZonesList(DescribeAvailabilityZonesRequest req)
    {
        var availabilityZones = RegionalClients
            .Select(async client => await client.DescribeAvailabilityZonesAsync(req))
            .SelectMany(response => response.Result.AvailabilityZones)
            .ToList();

        return availabilityZones;
    }
}