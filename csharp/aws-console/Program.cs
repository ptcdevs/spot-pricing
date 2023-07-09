// See https://aka.ms/new-console-template for more information

using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime;
using aws_console;
using Microsoft.Extensions.Configuration;

Console.WriteLine("start");

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

using var awsClient = new AmazonEC2Client(
    new BasicAWSCredentials(config["aws:accessKey"], config["AWSSECRETKEY"]),
    RegionEndpoint.USEast1);
var initialSpotPriceHistoryResponse = await awsClient.DescribeSpotPriceHistoryAsync();

var spotPriceHistory = Enumerable
    .Range(0, Int32.MaxValue)
    .AggregateUntil(
        new
        {
            entries = initialSpotPriceHistoryResponse.SpotPriceHistory as IEnumerable<SpotPrice>,
            totalEntries = initialSpotPriceHistoryResponse.SpotPriceHistory.Count(),
            nextToken = initialSpotPriceHistoryResponse.NextToken,
        }, (accumulatedPage, pageIndex) =>
        {
            var asdf = awsClient.DescribeSpotPriceHistoryAsync(new DescribeSpotPriceHistoryRequest()
            {
                NextToken = accumulatedPage.nextToken,
            });
            
            return accumulatedPage;
        },
        (accumulatedPage) =>
        {
            return true;
        }
    );

Console.WriteLine("fin");