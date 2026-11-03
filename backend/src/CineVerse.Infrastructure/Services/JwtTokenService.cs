using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using CineVerse.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CineVerse.Infrastructure.Services;

/// <summary>
/// Видача JWT Access Token (короткоживучий, claims: sub/email/role) та випадкового
/// Refresh Token (довгоживучий, зберігається в БД + HttpOnly Cookie на клієнті).
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly IDateTime _dateTime;

    public JwtTokenService(IConfiguration configuration, IDateTime dateTime)
    {
        _configuration = configuration;
        _dateTime = dateTime;
    }

    public TokenPair GenerateTokenPair(Guid userId, string email, string role)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var secret = jwtSection["Secret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            // Порожній рядок — так само небезпечно, як відсутнє значення: не даємо підписати
            // жоден токен "порожнім" секретом, який хтось міг випадково лишити в конфігу.
            throw new InvalidOperationException(
                "Jwt:Secret не налаштовано (порожнє значення). Задайте його через Jwt__Secret " +
                "у змінних середовища / user-secrets — appsettings.json навмисно не містить робочого значення.");
        }

        var accessMinutes = double.Parse(jwtSection["AccessTokenMinutes"] ?? "15");
        var refreshDays = double.Parse(jwtSection["RefreshTokenDays"] ?? "7");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = _dateTime.UtcNow.AddMinutes(accessMinutes);

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshTokenValue();
        var refreshExpiresAt = _dateTime.UtcNow.AddDays(refreshDays);

        return new TokenPair(accessToken, refreshToken, refreshExpiresAt);
    }

    public string GenerateRefreshTokenValue()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }
}
