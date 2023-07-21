using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace aws_restapi;

public abstract class GithubAuth
{
    /// <summary>
    /// this is a custom github policy, whitelisting specific users; moved here to declutter Program.cs
    /// </summary>
    /// <returns></returns>
    public static Action<AuthorizationOptions> CustomPolicy() =>
        options =>
        {
            options.AddPolicy("ValidGithubUser", new AuthorizationPolicyBuilder()
                .RequireAssertion(context =>
                {
                    var authorizedGithubUsers = new[]
                    {
                        "vector623",
                    };

                    var githubUser = context.User.Claims
                        .Where(claim => claim.Type.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"))
                        .Select(claim => claim.Value)
                        .SingleOrDefault() ?? "";
                    Console.WriteLine($"github user: {githubUser}");

                    return authorizedGithubUsers.Contains(githubUser);
                }).Build());
        };

    /// <summary>
    /// this class decorates the swagger doc (and ui) with oauth labels for methods requiring authorization
    /// </summary>
    public class SecurityFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var validGithubUserAuth = context
                .ApiDescription
                .ActionDescriptor
                .EndpointMetadata
                .OfType<AuthorizeAttribute>()
                .Any(authAttr => authAttr.Policy is "ValidGithubUser");

            var githubCustomRequirement = new List<OpenApiSecurityRequirement>
            {
                new()
                {
                    {
                        new OpenApiSecurityScheme()
                        {
                            Reference = new OpenApiReference()
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "GithubOAuth"
                            }
                        },
                        new List<string>()
                    }
                },
            };
            var noAuth = new List<OpenApiSecurityRequirement>();
            operation.Security = validGithubUserAuth
                ? githubCustomRequirement
                : noAuth;
        }
    }

    public Func<RedirectContext<OAuthOptions>, Task> GithubFixer = context =>
    {
        var redirectUrl = new Uri(context.RedirectUri);
        if (redirectUrl.Host.Equals("github.com"))
        {
            Log.Information("test");
            //TODO: fix proxied http scheme and make https
            var xForwardedHost = context.Request.Headers["X-Forwarded-Host"].ToString();
            var xForwardedProto = context.Request.Headers["X-Forwarded-Proto"].ToString();
            var query = HttpUtility.ParseQueryString(redirectUrl.Query);
            var oauthRedirect = new Uri(query["redirect_uri"]);
            var newOauthRedirect = new UriBuilder(oauthRedirect)
            {
                Scheme = xForwardedProto.Equals("")
                    ? oauthRedirect.Scheme
                    : xForwardedProto,
                Host = xForwardedHost.Equals("")
                    ? oauthRedirect.Host
                    : xForwardedHost,
            };
            var newQuery = query
                .AllKeys
                .Select(k =>
                {
                    // var keyValue = k[0].Equals("redirect_uri")
                    //     ? new[] { "redirect_uri", newOauthRedirect }
                    //     : new[] { k, query[k] };
                    var param = k[0].Equals("redirect_uri")
                        ? $"redirect_uri={HttpUtility.UrlEncode(newOauthRedirect.ToString())}"
                        : $"{k}={query[k]}";
                    return param;
                });
            var newRedirectUrl = new UriBuilder(redirectUrl)
            {
                Query = string.Join("&", newQuery)
            };

            // var headers = context.Request.Headers
            //     .Select(h => $"{h.Key.ToString()}: {h.Value.ToString()} ")
            //     .OrderBy(h => h)
            //     .ToList();
            // X-Forwarded-For: 139.144.30.218
            // X-Forwarded-Host: spot-pricing.dev.xounges.net
            // X-Forwarded-Port: 443
            // X-Forwarded-Proto: https
            // X-Forwarded-Scheme: https
            // Log.Information("headers: {Headers}", string.Join("\n", headers));
            Log.Information("oldRedirectUrl: {OldRedirectUrl}", redirectUrl);
            Log.Information("oldOauthRedirectUrl: {OldOauthRedirectUrl}", oauthRedirect);
            Log.Information("newRedirectUrl: {NewRedirectUrl}", newRedirectUrl);
            Log.Information("newOauthRedirectUrl: {NewRedirectUrl}", newOauthRedirect);
            context.RedirectUri = newRedirectUrl.ToString();
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };

}