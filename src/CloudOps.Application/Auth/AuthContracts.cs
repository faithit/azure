using System.ComponentModel.DataAnnotations;

namespace CloudOps.Application.Auth;

public sealed class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)] public string DisplayName { get; init; } = string.Empty;
    [Required, EmailAddress] public string Email { get; init; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required, EmailAddress] public string Email { get; init; } = string.Empty;
    [Required] public string Password { get; init; } = string.Empty;
}

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, UserDto User);
public sealed record UserDto(string Id, string DisplayName, string Email, IReadOnlyList<string> Roles);

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
