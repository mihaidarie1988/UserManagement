namespace DocumentManagement.Authorization;

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Services;

[AttributeUsage(AttributeTargets.Method)]
public sealed class DocumentOwnershipAttribute() : TypeFilterAttribute(typeof(DocumentOwnershipFilter));

public sealed class DocumentOwnershipFilter(DocumentStore store) : IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!context.RouteData.Values.TryGetValue("id", out var idValue) ||
            !int.TryParse(idValue?.ToString(), out var id))
            return Task.CompletedTask;

        var user = context.HttpContext.User;

        if (user.IsInRole(AuthorizationPolicies.AdminRole))
            return Task.CompletedTask;

        var document = store.FindById(id);
        if (document is null)
            return Task.CompletedTask; // let the action return 404

        if (document.CreatedBy != user.FindFirstValue(ClaimTypes.NameIdentifier))
            context.Result = new ForbidResult();

        return Task.CompletedTask;
    }
}
