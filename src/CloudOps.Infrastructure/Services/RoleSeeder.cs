using CloudOps.Domain.Entities;
using CloudOps.Infrastructure.Identity;
using CloudOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CloudOps.Infrastructure.Services;

public static class RoleSeeder
{
    public static async Task SeedAsync(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser>? userManager = null, ApplicationDbContext? dbContext = null)
    {
        foreach (var role in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role)) await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (userManager is null) return;

        var email = Environment.GetEnvironmentVariable("CLOUDOPS_DEFAULT_ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("CLOUDOPS_DEFAULT_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        var adminUser = await userManager.FindByEmailAsync(email);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = "System Administrator"
            };

            var createResult = await userManager.CreateAsync(adminUser, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Unable to create default admin user: {string.Join(", ", createResult.Errors.Select(error => error.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, ApplicationRoles.Admin))
        {
            var addRoleResult = await userManager.AddToRoleAsync(adminUser, ApplicationRoles.Admin);
            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException($"Unable to assign default admin role: {string.Join(", ", addRoleResult.Errors.Select(error => error.Description))}");
            }
        }

        if (dbContext is null) return;

        if (await dbContext.Projects.AnyAsync(project => project.OwnerId == adminUser.Id)) return;

        var demoProject = new Project("CloudOps Demo Launch", "Seeded project for local development and validation.", adminUser.Id);
        await dbContext.Projects.AddAsync(demoProject);
        await dbContext.SaveChangesAsync();
    }
}
