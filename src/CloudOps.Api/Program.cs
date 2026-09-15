using CloudOps.Api.Extensions;
using CloudOps.Api.Middleware;
using CloudOps.Application;
using CloudOps.Application.Abstractions;
using CloudOps.Infrastructure;
using CloudOps.Infrastructure.Identity;
using CloudOps.Infrastructure.Persistence;
using CloudOps.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text.Json.Serialization;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", Description = "Enter only the JWT bearer token." });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement { [new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>() });
});
var allowedOrigins = GetAllowedOrigins(builder.Configuration, builder.Environment);
builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enable"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", async (IServiceScopeFactory scopeFactory, CancellationToken cancellationToken) =>
{
    await using var scope = scopeFactory.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database;
    return await database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "healthy" })
        : Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Database unavailable");
}).AllowAnonymous();
app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database;
    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:MigrateOnStartup")) await database.MigrateAsync();
    if (app.Environment.IsEnvironment("Testing")) await database.EnsureCreatedAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await RoleSeeder.SeedAsync(roleManager, userManager, dbContext);
}
app.Run();


static string[] GetAllowedOrigins(IConfiguration configuration, IHostEnvironment environment)
{
    var values = new[]
    {
        configuration["CORS_ALLOWED_ORIGINS"],
        configuration["FrontendUrl"]
    }
    .Where(value => !string.IsNullOrWhiteSpace(value))
    .SelectMany(value => value!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
    .Append(!environment.IsProduction() ? "http://localhost:5173" : null)
    .Where(value => value is not null)
    .Cast<string>()
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

    if (values.Length == 0)
    {
        throw new InvalidOperationException("CORS_ALLOWED_ORIGINS must contain the deployed frontend origin outside Development.");
    }

    return values;
}
