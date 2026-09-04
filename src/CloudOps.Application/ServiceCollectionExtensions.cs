using CloudOps.Application.AI;
using CloudOps.Application.Dashboard;
using CloudOps.Application.Projects;
using CloudOps.Application.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace CloudOps.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services) => services
        .AddScoped<IProjectService, ProjectService>()
        .AddScoped<ITaskService, TaskService>()
        .AddScoped<IDashboardService, DashboardService>()
        .AddScoped<IAiService, AiService>();
}
