namespace CloudOps.Infrastructure.Identity;

public static class ApplicationRoles
{
    public const string Admin = "Admin";
    public const string ProjectManager = "ProjectManager";
    public const string Developer = "Developer";
    public const string Viewer = "Viewer";
    public static readonly string[] All = [Admin, ProjectManager, Developer, Viewer];
}
