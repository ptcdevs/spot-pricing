using System.Diagnostics;
using Amazon;
using Amazon.EC2.Model;
using Amazon.Runtime;
using aws_restapi;
using aws_restapi.services;
using Dapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.OpenApi.Models;
using Npgsql;
using Serilog;
using Serilog.Events;
using Z.Dapper.Plus;

#region config

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(builder.Environment.IsProduction()
        ? LogEventLevel.Information
        : LogEventLevel.Information)
    .CreateLogger();
builder.Logging.AddSerilog(Log.Logger);

//authentication
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "GitHub";
    })
    .AddCookie()
    .AddGitHub(authOptions =>
    {
        authOptions.ClientId = config["GithubOauth:ClientId"];
        authOptions.ClientSecret = config["GITHUB_OAUTH_CLIENT_SECRET"];
        authOptions.CallbackPath = "/callback";
        authOptions.Scope.Add("user:email");
    });

//swagger gen + ui config
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = builder.Environment.ApplicationName,
        Version = "v1"
    });
    options.AddSecurityDefinition("GithubOAuth", new OpenApiSecurityScheme()
    {
        Type = SecuritySchemeType.OAuth2,
        Description = "Github OAuth2 with custom user whitelist",
        In = ParameterLocation.Cookie,
        Flows = new OpenApiOAuthFlows()
        {
            AuthorizationCode = new OpenApiOAuthFlow()
            {
                Scopes = new Dictionary<string, string>()
                {
                    { "user:email", "read user email from github" }
                },
                AuthorizationUrl = new Uri("https://github.com/login/oauth/authorize")
            }
        },
    });
    options.OperationFilter<GithubAuth.SecurityFilter>();
});

// database config
builder.Services
    .AddScoped<NpgsqlConnectionStringBuilder>(provider => new NpgsqlConnectionStringBuilder()
    {
        Host = config["postgres:host"],
        Port = int.Parse(config["postgres:port"] ?? string.Empty),
        Database = config["postgres:database"],
        Username = config["postgres:username"],
        Password = config["POSTGRESQL_PASSWORD"],
        SslMode = SslMode.VerifyCA,
        RootCertificate = "sql/ptcdevs-psql-ca-certificate.crt",
    })
    .AddScoped<NpgsqlConnection>(provider =>
    {
        var npgsqlConnectionStringBuilder = provider.GetService<NpgsqlConnectionStringBuilder>();
        return new NpgsqlConnection(npgsqlConnectionStringBuilder.ToString());
    });
DapperPlusManager.Entity<SpotPrice>()
    .Table("SpotPrices")
    .Identity(x => x.Id);
DapperPlusManager.Entity<QueryRun>()
    .Table("QueriesRun")
    .Identity(x => x.Id);
DapperPlusManager.Entity<OnDemandCsvFile>()
    .Table("OnDemandCsvFiles")
    .Identity(x => x.Id);
DapperPlusManager.Entity<OnDemandCsvRow>()
    .Table("OnDemandCsvRows")
    .Identity(x => x.Id);

//aws config
builder.Services.AddScoped<AwsMultiClient>(provider =>
{
    return new AwsMultiClient(
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
});

#endregion config

builder.Services
    .AddAuthorization(GithubAuth.CustomPolicy());
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.Use((context, next) =>
    {
        context.Request.Scheme = "https";
        return next(context);
    });
}

// app.UseForwardedHeaders();
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", $"{builder.Environment.ApplicationName} v1");
});
app.MapGet("/", () => "Hello World!")
    .RequireAuthorization("ValidGithubUser");
//TODO: confirm service authentication for health check
app.MapGet("/health", () => "healthy");
app.MapGet("login", () => "authorized")
    .RequireAuthorization("ValidGithubUser");
app.MapGet("unauthorized", Results.Unauthorized);
app.MapGet("syncgpuspotpricing", async (NpgsqlConnection connection, AwsMultiClient awsMultiClient) =>
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        await connection.OpenAsync();

        var datesToQuerySql = File.ReadAllText("sql/gpu-spotprice-datehours-tofetch.sql");
        var datesToQuery = connection.Query(datesToQuerySql)
            .ToList();
        var datesToQuerySubset = datesToQuery
            .OrderByDescending(ts => ts.starttime)
            .Take(250)
            .ToList();
        var semaphore = new SemaphoreSlim(10);
        var results = datesToQuerySubset
            .Select(async dateToQuery =>
            {
                try
                {
                    semaphore.Wait();
                    var starttime = (DateTime)dateToQuery.starttime;
                    var endtime = starttime.AddHours(1);
                    var instanceTypes = AwsParams.GetGpuInstances();
                    var spotPrices = await awsMultiClient
                        .SampleSpotPricing(new DescribeSpotPriceHistoryRequest
                            {
                                MaxResults = 10000,
                                StartTimeUtc = starttime,
                                EndTimeUtc = endtime,
                                Filters = new List<Filter>
                                {
                                    new("availability-zone",
                                        new List<string> { "us-east-1a", "us-east-2a", "us-west-1a", "us-west-2a" }),
                                    new("instance-type", instanceTypes.ToList()),
                                },
                            }
                        );
                    Log.Information($"finished {dateToQuery.starttime}, retrieved {spotPrices.Count()} records");
                    return spotPrices;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"error while syncing gpu spot prices for querydate:: {dateToQuery.starttime}");
                    throw ex;
                }
                finally
                {
                    semaphore.Release();
                }
            });

        var spotPricesBatches = await Task.WhenAll(results);
        var spotPrices = spotPricesBatches
            .SelectMany(spotPriceBatch => spotPriceBatch)
            .ToList();
        var queriesRun = datesToQuerySubset
            .Select(dateToQuery => new QueryRun()
            {
                Search = "GpuMlMain",
                StartTime = dateToQuery.starttime,
            });

        if (spotPrices.Any())
            connection.BulkInsert(spotPrices);
        if (queriesRun.Any())
            connection.BulkInsert(queriesRun);
        // var spotPricesToDedupSql = await File.ReadAllTextAsync("sql/spotPricesToDedup.sql");
        var dedupSql = await File.ReadAllTextAsync("sql/dedup.sql");
        // var spotPricesToDedup = await connection.ExecuteAsync(spotPricesToDedupSql);
        var dedupResult = connection.Execute(dedupSql);
        stopWatch.Stop();

        return Results.Json(new
        {
            success = true,
            spotPricesInserted = spotPrices.Count(),
            duplicateRowsDeleted = dedupResult,
            timeToCompletion = stopWatch.Elapsed,
            dateTimesQueried = queriesRun.Select(q => q.StartTime),
        });
    })
    .RequireAuthorization("ValidGithubUser");
app.MapGet("syncondemandpricing", async (
        NpgsqlConnectionStringBuilder connectionStringBuilder,
        NpgsqlConnection connection,
        AwsMultiClient awsMultiClient
        , CancellationToken cancelToken) =>
    {
        var onDemandPriceUrlsFetchedSql = File.ReadAllText("sql/onDemandPriceUrlsFetched.sql");
        var onDemandPriceUrlsFetched = await connection
            .QueryAsync<string>(onDemandPriceUrlsFetchedSql, commandTimeout: 300);

        var priceFileUrlResponses = await awsMultiClient.GetPriceFileDownloadUrlsAsync(cancelToken);
        var batchSize = int.Parse(config["spot-pricing:onDemandDownloadBatchSize"] ?? "1");
        var priceUrlsToFetch = priceFileUrlResponses
            .Where(resp => !onDemandPriceUrlsFetched.Contains(resp.Url))
            .Take(batchSize)
            .ToList();
        var semaphore = new SemaphoreSlim(1);
        var downloads = priceUrlsToFetch
            .Select(async priceFileDownloadUrl =>
            {
                try
                {
                    semaphore.Wait();
                    Log.Information(" downloading url: {priceFileUrl}", priceFileDownloadUrl.Url);
                    return await awsMultiClient.DownloadPriceFileAsync(priceFileDownloadUrl, connectionStringBuilder,
                        cancelToken);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "error with {priceFileDownloadUrl}", priceFileDownloadUrl.Url);
                    throw ex;
                }
                finally
                {
                    semaphore.Release();
                }
            })
            .ToList();

        var downloadPriceFileResults = await Task.WhenAll(downloads);
        
        return new
        {
            priceUrlsToFetch,
            downloadPriceFileResults
        };
    })
    .RequireAuthorization("ValidGithubUser");

app.MapGet("parseondemandpricing", async (
    NpgsqlConnection connection,
    NpgsqlConnectionStringBuilder connectionStringBuilder,
    AwsMultiClient awsMultiClient, 
    CancellationToken cancelToken) =>
{
    var unparsedCsvFileIdsSql = await File.ReadAllTextAsync("sql/unparsedCsvFile.sql", cancelToken);
    var batchSize = int.Parse(config["spot-pricing:onDemandParseBatchSize"] ?? "1");
    var csvFiles = connection
        .Query<OnDemandCsvFile>(unparsedCsvFileIdsSql)
        .Take(batchSize)
        .ToList();
    var semaphore = new SemaphoreSlim(1);
    var resultTasks = csvFiles
        .Select(async csvFile =>
        {
            try
            {
                semaphore.Wait();
                Log.Information("parsing csv file id ({csvFileId}) from url: {csvFileUrl}", csvFile.Id, csvFile.Url);
                return await awsMultiClient.ParseOnDemandPricingAsync(csvFile.Id, connectionStringBuilder, cancelToken);
            }
            catch (Exception ex)
            {
                Log.Error("error parsing csv file id ({csvFileId}) from url: {csvFileUrl}", csvFile.Id, csvFile.Url);
                throw ex;
            }
            finally
            {
                semaphore.Release();
            }
        });

    var results = await Task.WhenAll(resultTasks);
    return new
    {
        results
    };
}).RequireAuthorization("ValidGithubUser");

app.Run();