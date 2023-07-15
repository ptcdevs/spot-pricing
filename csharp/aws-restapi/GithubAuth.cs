using Microsoft.AspNetCore.Authorization;

namespace aws_restapi;

public class GithubAuth
{
    // public static AuthorizationPolicyBuilder Policy() =>
    //     new AuthorizationPolicyBuilder()
    //         .RequireAssertion(context =>
    //         {
    //             var authorizedGithubUsers = new[]
    //             {
    //                 "vector623",
    //             };
    //
    //             var githubUser = context.User.Claims
    //                 .Where(claim => claim.Type.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"))
    //                 .Select(claim => claim.Value)
    //                 .SingleOrDefault() ?? "";
    //             Console.WriteLine($"github user: {githubUser}");
    //
    //             return authorizedGithubUsers.Contains(githubUser);
    //         });

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
}