using Amazon.Runtime.Internal.Transform;
using aws_restapi;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

#region config

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
    .AddCookie(cookieOptions => { cookieOptions.AccessDeniedPath = "/unauthorized"; })
    .AddGitHub(authOptions =>
    {
        authOptions.ClientId = configuration["GithubOauth:ClientId"];
        authOptions.ClientSecret = configuration["GITHUB_OAUTH_CLIENT_SECRET"];
        authOptions.CallbackPath = "/callback";
        authOptions.Scope.Add("user:email");
    });
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
                    new("user:email","read user email from github")
                },
                AuthorizationUrl = new Uri("https://github.com/login/oauth/authorize")
            }
        },
    });
    options.OperationFilter<GithubAuth.SecurityFilter>();
});

#endregion config

builder.Services
    .AddAuthorization(GithubAuth.CustomPolicy());
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{builder.Environment.ApplicationName} v1"));
app.MapGet("/", () => "Hello World!")
    .RequireAuthorization("ValidGithubUser");
app.MapGet("/authorize", () => "authorized")
    .RequireAuthorization("ValidGithubUser");
app.MapGet("/unauthorized", () => new UnauthorizedResult());

app.Run();