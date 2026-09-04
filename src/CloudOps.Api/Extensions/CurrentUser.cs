using System.Security.Claims;
using CloudOps.Application.Abstractions;

namespace CloudOps.Api.Extensions;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;
    public string? Id => Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub");
    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}
