using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ContaCorrente.Domain.Interfaces;
using ContaCorrente.Infrastructure.Data;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ContaCorrente.Infrastructure.Security;

public sealed class JwtTokenService : IAuthTokenService
{
    private readonly JwtOptions _jwtOptions;

    public JwtTokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public string GenerateToken(Guid idContaCorrente)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key)),
            SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, idContaCorrente.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, idContaCorrente.ToString())
        };
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
