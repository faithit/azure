using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CloudOps.Application.Auth;
using CloudOps.Application.Common;
using CloudOps.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CloudOps.Infrastructure.Services;

public sealed class AuthService(UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwtOptions) : IAuthService
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser { UserName = request.Email.Trim(), Email = request.Email.Trim(), DisplayName = request.DisplayName.Trim() };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded) throw new ConflictException(string.Join(" ", result.Errors.Select(error => error.Description)));
        var roleResult = await userManager.AddToRoleAsync(user, ApplicationRoles.Developer);
        if (!roleResult.Succeeded) throw new ConflictException("The user was created but the default role could not be assigned.");
        return await CreateResponseAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password)) throw new UnauthorizedAccessException("Invalid email or password.");
        return await CreateResponseAsync(user);
    }

    private async Task<AuthResponse> CreateResponseAsync(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(_jwt.Secret) || Encoding.UTF8.GetByteCount(_jwt.Secret) < 32) throw new InvalidOperationException("JWT secret must be configured and at least 32 bytes long.");
        var roles = await userManager.GetRolesAsync(user);
        var expiry = DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes);
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, user.Id), new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty), new(ClaimTypes.Name, user.DisplayName) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, expires: expiry, signingCredentials: credentials);
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expiry, new UserDto(user.Id, user.DisplayName, user.Email ?? string.Empty, roles.ToList()));
    }
}
