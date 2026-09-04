using CloudOps.Application.Auth;
using CloudOps.Infrastructure.Identity;
using CloudOps.Infrastructure.Persistence;
using CloudOps.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CloudOps.Application.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_HashesPassword_AssignsDeveloperRole_AndReturnsJwt()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<ApplicationUser>(options => options.User.RequireUniqueEmail = true)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await RoleSeeder.SeedAsync(roleManager);
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var settings = Options.Create(new JwtSettings { Issuer = "CloudOps.Tests", Audience = "CloudOps.Tests", Secret = new string('s', 48), ExpiryMinutes = 30 });
        var service = new AuthService(userManager, settings);

        var result = await service.RegisterAsync(new RegisterRequest { DisplayName = "Ada Lovelace", Email = "ada@example.test", Password = "A-strong-password-1" });

        var user = await userManager.FindByEmailAsync("ada@example.test");
        Assert.NotNull(user);
        Assert.NotEqual("A-strong-password-1", user.PasswordHash);
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ApplicationUser>>();
        Assert.Equal(PasswordVerificationResult.Success, passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, "A-strong-password-1"));
        Assert.Contains(ApplicationRoles.Developer, result.User.Roles);
        Assert.NotEmpty(result.AccessToken);
    }

    [Fact]
    public async Task SeedAsync_CreatesDefaultAdminUser_WhenAdminConfigIsProvided()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<ApplicationUser>(options => options.User.RequireUniqueEmail = true)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var originalEmail = Environment.GetEnvironmentVariable("CLOUDOPS_DEFAULT_ADMIN_EMAIL");
        var originalPassword = Environment.GetEnvironmentVariable("CLOUDOPS_DEFAULT_ADMIN_PASSWORD");
        Environment.SetEnvironmentVariable("CLOUDOPS_DEFAULT_ADMIN_EMAIL", "admin@cloudops.local");
        Environment.SetEnvironmentVariable("CLOUDOPS_DEFAULT_ADMIN_PASSWORD", "AdminPass123!");

        try
        {
            await RoleSeeder.SeedAsync(roleManager, userManager, context);

            var admin = await userManager.FindByEmailAsync("admin@cloudops.local");
            Assert.NotNull(admin);
            Assert.True(await userManager.IsInRoleAsync(admin!, ApplicationRoles.Admin));
            Assert.Contains(context.Projects, project => project.OwnerId == admin!.Id && project.Name == "CloudOps Demo Launch");
        }
        finally
        {
            if (originalEmail is null) Environment.SetEnvironmentVariable("CLOUDOPS_DEFAULT_ADMIN_EMAIL", null);
            else Environment.SetEnvironmentVariable("CLOUDOPS_DEFAULT_ADMIN_EMAIL", originalEmail);

            if (originalPassword is null) Environment.SetEnvironmentVariable("CLOUDOPS_DEFAULT_ADMIN_PASSWORD", null);
            else Environment.SetEnvironmentVariable("CLOUDOPS_DEFAULT_ADMIN_PASSWORD", originalPassword);
        }
    }
}
