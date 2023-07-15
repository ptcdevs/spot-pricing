using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();
var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "GitHub";
    })
    .AddCookie()
    .AddGitHub(gitHubAuthenticationOptions =>
    {
        gitHubAuthenticationOptions.ClientId = configuration["GithubOauth:ClientId"];
        gitHubAuthenticationOptions.ClientSecret = configuration["GITHUB_OAUTH_CLIENT_SECRET"];
        gitHubAuthenticationOptions.CallbackPath = "/callback";
        gitHubAuthenticationOptions.Scope.Add("user:email");
    });
builder.Services
    .AddAuthorization(config =>
    {
        config
            .AddPolicy("ValidGitHubUser", new AuthorizationPolicyBuilder()
                .RequireAssertion(context =>
                {
                    var authorizedGithubUsers = new[]
                    {
                        "vector6234",
                    };
    
                    var githubUser = context.User.Claims
                        .Where(claim => claim.Type.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"))
                        .Select(claim => claim.Value)
                        .SingleOrDefault() ?? "";
                    Console.WriteLine($"github user: {githubUser}");
    
                    return authorizedGithubUsers.Contains(githubUser);
                })
                .Build());
    });

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!")
    .RequireAuthorization("ValidGitHubUser");

app.Run();