using Microsoft.AspNetCore.Authorization;

namespace CloudOps.Infrastructure.Identity;

public static class AuthorizationPolicies
{
    public const string ManageProjects = "ManageProjects";
    public const string ManageTasks = "ManageTasks";

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(ManageProjects, policy => policy.RequireRole(ApplicationRoles.Admin, ApplicationRoles.ProjectManager, ApplicationRoles.Developer));
        options.AddPolicy(ManageTasks, policy => policy.RequireRole(ApplicationRoles.Admin, ApplicationRoles.ProjectManager, ApplicationRoles.Developer));
    }
}
