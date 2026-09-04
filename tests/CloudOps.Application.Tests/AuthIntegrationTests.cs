using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CloudOps.Application.Auth;
using CloudOps.Application.Common;
using CloudOps.Application.Projects;
using CloudOps.Application.Tasks;
using CloudOps.Infrastructure.Identity;
using CloudOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CloudOps.Application.Tests;

public sealed class AuthIntegrationTests(CloudOpsApiFactory factory) : IClassFixture<CloudOpsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    [Fact]
    public async Task ProtectedProjectEndpoint_RejectsAnonymousRequests()
    {
        var response = await _client.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsJwt_AndAllowsAuthenticatedProjectRead()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { DisplayName = "Grace Hopper", Email = "grace@example.test", Password = "A-strong-password-2" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.NotEmpty(auth.AccessToken);
        Assert.Contains(ApplicationRoles.Developer, auth.User.Roles);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var projects = await _client.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.OK, projects.StatusCode);
    }

    [Fact]
    public async Task Viewer_IsForbiddenFromTaskManagementPolicy()
    {
        const string email = "viewer@example.test";
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = email, Email = email, DisplayName = "Read Only" };
            Assert.True((await users.CreateAsync(user, "A-strong-password-3")).Succeeded);
            Assert.True((await users.AddToRoleAsync(user, ApplicationRoles.Viewer)).Succeeded);
        }

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "A-strong-password-3" });
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var response = await _client.PostAsJsonAsync($"/api/projects/{Guid.NewGuid()}/tasks", new { title = "Blocked task", priority = "Medium" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TaskList_AppliesStatusPriorityFiltering_AndReturnsPagingMetadata()
    {
        var auth = await RegisterAndAuthenticateAsync("tasks@example.test", "A-strong-password-4");
        var projectResponse = await _client.PostAsJsonAsync("/api/projects", new CreateProjectRequest { Name = "Filtered work" });
        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);
        var project = await projectResponse.Content.ReadFromJsonAsync<ProjectDto>();
        Assert.NotNull(project);

        var createTask = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks", new { title = "Critical release", priority = "High" });
        var task = await createTask.Content.ReadFromJsonAsync<TaskDto>(JsonOptions);
        Assert.NotNull(task);
        var update = await _client.PutAsJsonAsync($"/api/tasks/{task.Id}", new { title = task.Title, priority = "High", status = "Done" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        await _client.PostAsJsonAsync($"/api/projects/{project.Id}/tasks", new { title = "Low priority", priority = "Low" });

        var response = await _client.GetAsync($"/api/projects/{project.Id}/tasks?status=Done&priority=High&pageNumber=1&pageSize=1");
        var page = await response.Content.ReadFromJsonAsync<PagedResult<TaskDto>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(page);
        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("Critical release", page.Items[0].Title);
    }

    private async Task<AuthResponse> RegisterAndAuthenticateAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest { DisplayName = "Task Manager", Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return auth;
    }
}

public sealed class CloudOpsApiFactory : WebApplicationFactory<global::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=cloudops_tests");
        builder.UseSetting("Jwt:Issuer", "CloudOps.Tests");
        builder.UseSetting("Jwt:Audience", "CloudOps.Tests");
        builder.UseSetting("Jwt:Secret", new string('t', 48));
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=cloudops_tests",
            ["Jwt:Issuer"] = "CloudOps.Tests",
            ["Jwt:Audience"] = "CloudOps.Tests",
            ["Jwt:Secret"] = new string('t', 48)
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("cloudops-integration-tests"));
        });
    }
}
