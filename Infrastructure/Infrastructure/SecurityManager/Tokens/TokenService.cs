using Infrastructure.SecurityManager.AspNetIdentity;
using Application.Common.Services;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.SecurityManager.Tokens;

public interface ITokenService
{
    string GenerateToken(ApplicationUser user, List<Claim>? userClaims);
    string GenerateRefreshToken();
}
public class TokenService : ITokenService, ICustomerTokenService
{
    private readonly TokenSettings _tokenSettings;

    public TokenService(
        IOptions<TokenSettings> tokenSettings
        )
    {
        _tokenSettings = tokenSettings.Value;
    }

    private SymmetricSecurityKey GetSymmetricSecurityKey()
    {
        var keyBytes = Encoding.UTF8.GetBytes(_tokenSettings.Key);
        return new SymmetricSecurityKey(keyBytes);
    }

    public string GenerateToken(ApplicationUser user, List<Claim>? userClaims)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("FirstName", user.FirstName ?? ""),
            new Claim("LastName", user.LastName ?? ""),
            new Claim("CompanyName", user.LastName ?? ""),
            new Claim("identityType", "User"),
        };

        if (userClaims != null)
        {

            claims.AddRange(userClaims);

        }

        var key = GetSymmetricSecurityKey();
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _tokenSettings.Issuer,
            audience: _tokenSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_tokenSettings.ExpireInMinute),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateToken(Customer customer)
    {
        var nameParts = (customer.Name ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, customer.Id),
            new Claim(JwtRegisteredClaimNames.Sub, customer.Id),
            new Claim(JwtRegisteredClaimNames.Email, customer.EmailAddress ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("FirstName", nameParts.FirstOrDefault() ?? ""),
            new Claim("LastName", nameParts.Length <= 1 ? "" : string.Join(' ', nameParts.Skip(1))),
            new Claim(ClaimTypes.Role, "Customer"),
            new Claim("identityType", "Customer")
        };

        var key = GetSymmetricSecurityKey();
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _tokenSettings.Issuer,
            audience: _tokenSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_tokenSettings.ExpireInMinute),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}

