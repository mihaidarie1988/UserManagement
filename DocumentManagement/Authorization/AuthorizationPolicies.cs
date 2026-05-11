namespace DocumentManagement.Authorization;

public static class AuthorizationPolicies
{
    public const string ReadPolicy   = "RequireReadPermission";
    public const string CreatePolicy = "RequireCreatePermission";
    public const string UpdatePolicy = "RequireUpdatePermission";
    public const string DeletePolicy = "RequireDeletePermission";
}
