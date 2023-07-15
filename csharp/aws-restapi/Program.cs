using Amazon;
using Amazon.EC2.Model;
using Amazon.Runtime;
using aws_console;
using aws_restapi;
using aws_restapi.services;
using Dapper;
using Microsoft.AspNetCore.Authentication.Cookies;
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
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(builder.Environment.IsProduction()
        ? LogEventLevel.Error
        : LogEventLevel.Information)
    .CreateLogger();

//authentication
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "GitHub";
    })
    .AddCookie(cookieOptions => { cookieOptions.AccessDeniedPath = "/unauthorized"; })
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
    // options.SwaggerDoc("v1", new()
    // {
    //     Title = builder.Environment.ApplicationName,
    //     Version = "v1"
    // });
    // options.AddSecurityDefinition("GithubOAuth", new OpenApiSecurityScheme()
    // {
    //     Type = SecuritySchemeType.OAuth2,
    //     Description = "Github OAuth2 with custom user whitelist",
    //     In = ParameterLocation.Cookie,
    //     Flows = new OpenApiOAuthFlows()
    //     {
    //         AuthorizationCode = new OpenApiOAuthFlow()
    //         {
    //             Scopes = new Dictionary<string, string>()
    //             {
    //                 { "user:email", "read user email from github" }
    //             },
    //             AuthorizationUrl = new Uri("https://github.com/login/oauth/authorize")
    //         }
    //     },
    // });
    // options.OperationFilter<GithubAuth.SecurityFilter>();
});

// database config
var npgsqlConnectionStringBuilder = new NpgsqlConnectionStringBuilder()
{
    Host = config["postgres:host"],
    Port = int.Parse(config["postgres:port"] ?? string.Empty),
    Database = config["postgres:database"],
    Username = config["postgres:username"],
    Password = config["POSTGRESQL_PASSWORD"],
    SslMode = SslMode.VerifyCA,
    RootCertificate = "sql/ptcdevs-psql-ca-certificate.crt",
};
builder.Services.AddScoped<NpgsqlConnection>(provider =>
    new NpgsqlConnection(npgsqlConnectionStringBuilder.ToString()));

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
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => "Hello World!")
    .RequireAuthorization("ValidGithubUser");
app.MapGet("authorize", () => "authorized")
    .RequireAuthorization("ValidGithubUser");
app.MapGet("unauthorized", () => Results.Unauthorized());
app.MapGet("syncpricing", async (NpgsqlConnection connection, AwsMultiClient awsMultiClient) =>
    {
        await connection.OpenAsync();

        var datesToQuerySql = File.ReadAllText("sql/dates-hours-tofetch.sql");
        var datesToQuery = connection.Query(datesToQuerySql)
            .ToList();
        var datesToQuerySubset = datesToQuery
            .OrderByDescending(ts => ts.querydate)
            .Take(1)
            .ToList();
        var semaphore = new SemaphoreSlim(10);
        var results = datesToQuerySubset
            .Select(async dateToQuery =>
            {
                try
                {
                    semaphore.Wait();
                    var starttime = (DateTime)dateToQuery.querydate;
                    var endtime = starttime.AddDays(1);
                    var instanceTypes = AwsParams.GetGpuInstances();
                    var responses = await awsMultiClient
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
                    var spotPrices = responses
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
                    Console.WriteLine($"finished {dateToQuery.querydate}, retrieved {spotPrices.Count()} records");
                    return spotPrices;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"error while querying date: {dateToQuery.querydate}");
                    throw ex;
                }
                finally
                {
                    semaphore.Release();
                }
            });

        var spotPrices = await Task.WhenAll(results);
        var queriesRun = datesToQuerySubset
            .Select(dateToQuery => new QueryRun()
            {
                Search = "GpuMlMain",
                StartTime = dateToQuery.querydate,
            });

        if (spotPrices.Any())
            connection.BulkInsert(spotPrices);
        if (queriesRun.Any())
            connection.BulkInsert(queriesRun);
        return Results.Json(new
        {
            success = true,
            datesQueried = queriesRun.Select(q => q.StartTime)
        });
    })
    .WithName("SyncPricing")
    .WithDisplayName("SyncPricing")
    .RequireAuthorization("ValidGithubUser");
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", $"{builder.Environment.ApplicationName} v1");
});

app.Run();