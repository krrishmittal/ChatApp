using ChatApp.Application.Interfaces.Services;
using ChatApp.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ChatApp.Infrastructure.Services;
public class TokenService : ITokenService
{
    private readonly  IConfiguration _config;
    public TokenService(IConfiguration config)
    {
        _config = config;
    }
    public string GenerateAccessToken(User user)
    {
        var jwtsettings=_config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtsettings["SecretKey"]!));
        var creds= new SigningCredentials(key, SecurityAlgorithms.HmacSha512);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
            new Claim(ClaimTypes.Email,user.Email!),
            new Claim(ClaimTypes.Name,user.FullName!)
        };

        var token = new JwtSecurityToken(
            issuer: jwtsettings["Issuer"],
            audience: jwtsettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtsettings["AccessTokenExpiryMinutes"]!)),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
