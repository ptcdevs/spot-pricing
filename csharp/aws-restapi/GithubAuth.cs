using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace aws_restapi;

public abstract class GithubAuth
{
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

    public class SecurityFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var metaData = context
                .ApiDescription
                .ActionDescriptor
                .EndpointMetadata;
            var authAttributes = metaData
                .OfType<AuthorizeAttribute>();
            var validGithubUserAuth = authAttributes
                .Any(authAttr => authAttr.Policy.Equals("ValidGithubUser"));

            var githubCustomRequirement = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement()
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