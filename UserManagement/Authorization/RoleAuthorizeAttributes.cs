using Microsoft.AspNetCore.Authorization;

namespace UserManagement.Authorization;

public sealed class ReadAccessAttribute : AuthorizeAttribute
{
    public ReadAccessAttribute()
    {
        Policy = AuthorizationPolicies.ReadPolicy;
    }
}

public sealed class UpdateAccessAttribute : AuthorizeAttribute
{
    public UpdateAccessAttribute()
    {
        Policy = AuthorizationPolicies.UpdatePolicy;
    }
}

public sealed class DeleteAccessAttribute : AuthorizeAttribute
{
    public DeleteAccessAttribute()
    {
        Policy = AuthorizationPolicies.DeletePolicy;
    }
}
