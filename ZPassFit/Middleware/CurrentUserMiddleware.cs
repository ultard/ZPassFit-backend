using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ZPassFit.Data.Models;

namespace ZPassFit.Middleware;

public static class CurrentUserHttpContextExtensions
{
    public const string ApplicationUserKey = "__ZPassFit_ApplicationUser";

    public static ApplicationUser? GetCurrentApplicationUser(this HttpContext httpContext)
    {
        return httpContext.Items.TryGetValue(ApplicationUserKey, out var value) ? value as ApplicationUser : null;
    }

    public static ApplicationUser GetRequiredCurrentApplicationUser(this HttpContext httpContext)
    {
        return httpContext.GetCurrentApplicationUser()
               ?? throw new InvalidOperationException(
                   "Current user is missing; ensure CurrentUserMiddleware runs after authentication.");
    }
}

public sealed class CurrentUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var user = await userManager.GetUserAsync(context.User);
            context.Items[CurrentUserHttpContextExtensions.ApplicationUserKey] = user;

            if (EndpointRequiresAuthorization(context) && user is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await next(context);
    }

    private static bool EndpointRequiresAuthorization(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null)
            return false;

        if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            return false;

        var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        return authorizeData is { Count: > 0 };
    }
}