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
                    // Console.WriteLine($"github user: {githubUser}");

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
}