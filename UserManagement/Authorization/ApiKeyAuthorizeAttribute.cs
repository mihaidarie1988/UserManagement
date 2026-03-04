using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UserManagement.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ApiKeyAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private const string HeaderName = "X-Api-Key";
    private const string ApiKey = "local-secret-key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.Filters.Any(f => f is IAllowAnonymousFilter))
        {
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var key) ||
            !string.Equals(key, ApiKey, StringComparison.Ordinal))
        {
            context.Result = new UnauthorizedObjectResult("Missing or invalid API key.");
        }
    }
}
