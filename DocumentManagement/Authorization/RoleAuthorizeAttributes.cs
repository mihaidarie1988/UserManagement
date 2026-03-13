namespace DocumentManagement.Authorization;

using Microsoft.AspNetCore.Authorization;

public sealed class ReadAccessAttribute : AuthorizeAttribute
{
    public ReadAccessAttribute()
    {
        Policy = AuthorizationPolicies.ReadPolicy;
    }
}

public sealed class CreateAccessAttribute : AuthorizeAttribute
{
    public CreateAccessAttribute()
    {
        Policy = AuthorizationPolicies.CreatePolicy;
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
