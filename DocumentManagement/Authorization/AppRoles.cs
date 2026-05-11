namespace DocumentManagement.Authorization;

public static class AppRoles
{
    public const string Viewer  = "Viewer";
    public const string Editor  = "Editor";
    public const string Manager = "Manager";
    public const string Admin   = "Admin";

    public static IEnumerable<string> GetPermissions(string role) => role switch
    {
        Viewer  => [AppPermissions.Read],
        Editor  => [AppPermissions.Read, AppPermissions.Create, AppPermissions.Update],
        Manager => [AppPermissions.Read, AppPermissions.Create, AppPermissions.Update, AppPermissions.Delete],
        Admin   => [AppPermissions.Read, AppPermissions.Create, AppPermissions.Update, AppPermissions.Delete],
        _       => []
    };
}
