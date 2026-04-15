using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ZPassFit.Data.Models;

namespace ZPassFit.Middleware;

/// <summary>
/// Ключ элемента <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/> с загруженным пользователем Identity.
/// </summary>
public static class CurrentUserHttpContextExtensions
{
    public const string ApplicationUserKey = "__ZPassFit_ApplicationUser";

    /// <summary>
    /// Пользователь, загруженный <see cref="CurrentUserMiddleware"/> после успешной аутентификации.
    /// </summary>
    public static ApplicationUser? GetCurrentApplicationUser(this HttpContext httpContext) =>
        httpContext.Items.TryGetValue(ApplicationUserKey, out var value) ? value as ApplicationUser : null;

    /// <summary>
    /// Текущий пользователь для эндпоинтов с <see cref="AuthorizeAttribute"/>; после middleware не null.
    /// </summary>
    public static ApplicationUser GetRequiredCurrentApplicationUser(this HttpContext httpContext) =>
        httpContext.GetCurrentApplicationUser()
        ?? throw new InvalidOperationException("Current user is missing; ensure CurrentUserMiddleware runs after authentication.");
}

/// <summary>
/// После JWT-аутентификации подгружает <see cref="ApplicationUser"/> в контекст запроса.
/// Для эндпоинтов, требующих авторизацию, при отсутствии пользователя в БД отвечает 401.
/// </summary>
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
