using aws_restapi;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Events;

var configuration = new ConfigurationBuilder()
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
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "GitHub";
    })
    .AddCookie(cookieOptions =>
    {
        cookieOptions.AccessDeniedPath = "/unauthorized";
    })
    .AddGitHub(authOptions =>
    {
        authOptions.ClientId = configuration["GithubOauth:ClientId"];
        authOptions.ClientSecret = configuration["GITHUB_OAUTH_CLIENT_SECRET"];
        authOptions.CallbackPath = "/callback";
        authOptions.Scope.Add("user:email");
    });

builder.Services
    .AddAuthorization(GithubAuth.CustomPolicy());

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!")
    .RequireAuthorization("ValidGitHubUser");
app.MapGet("/unauthorized", () => new UnauthorizedResult());


app.Run();