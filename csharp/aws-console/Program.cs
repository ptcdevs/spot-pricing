// See https://aka.ms/new-console-template for more information

using Amazon;
using Amazon.EC2;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;

Console.WriteLine("start");

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

using var awsClient = new AmazonEC2Client(
    new BasicAWSCredentials("AKIAXULOAMFSPPU3LNWM", "RGnVXtRv+U0essdPi1quU0HRvoCkgxE2t76iFU/V"),
    RegionEndpoint.USEast1);
var spotPriceHistoryAsync = await awsClient.DescribeSpotPriceHistoryAsync();

Console.WriteLine("fin");