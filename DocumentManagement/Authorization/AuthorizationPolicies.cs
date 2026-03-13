namespace DocumentManagement.Authorization;

public static class AuthorizationPolicies
{
    public const string ReadRole = "Read";
    public const string CreateRole = "Create";
    public const string UpdateRole = "Update";
    public const string DeleteRole = "Delete";
    public const string AdminRole = "Admin";

    public const string ReadPolicy = "RequireReadRole";
    public const string CreatePolicy = "RequireCreateRole";
    public const string UpdatePolicy = "RequireUpdateRole";
    public const string DeletePolicy = "RequireDeleteRole";
}
