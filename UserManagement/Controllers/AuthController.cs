using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserManagement.Authorization;

namespace UserManagement.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(JwtTokenOptions jwt) : ControllerBase
{
    private static readonly Dictionary<string, (string Password, string[] Roles)> LocalUsersWithRoles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["reader"] = ("reader123!", [AuthorizationPolicies.ReadRole]),
            ["creator"] = ("creator123!", [AuthorizationPolicies.CreateRole]),
            ["editor"] = ("editor123!", [AuthorizationPolicies.UpdateRole]),
            ["deleter"] = ("deleter123!", [AuthorizationPolicies.DeleteRole]),
            ["admin"] = ("admin123!", [AuthorizationPolicies.ReadRole, AuthorizationPolicies.CreateRole, AuthorizationPolicies.UpdateRole, AuthorizationPolicies.DeleteRole, AuthorizationPolicies.AdminRole])
        };

    public record TokenRequest(string Username, string Password);

    public record TokenResponse(string AccessToken, DateTime ExpiresAtUtc, string[] Roles);

    /// <summary>
    /// Authenticates a local user and returns a JWT token.
    /// </summary>
    /// <response code="200">Token issued successfully.</response>
    /// <response code="401">Invalid credentials.</response>
    [AllowAnonymous]
    [HttpPost("token")]
    public IActionResult CreateToken([FromBody] TokenRequest request)
    {
        if (!LocalUsersWithRoles.TryGetValue(request.Username, out var account) ||
            !string.Equals(account.Password, request.Password, StringComparison.Ordinal))
        {
            return Unauthorized("Invalid username or password.");
        }

        var expires = DateTime.UtcNow.AddMinutes(60);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(account.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new TokenResponse(tokenValue, expires, account.Roles));
    }
}
