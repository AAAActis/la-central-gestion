using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Puertos;

namespace LaCentral.Data.Servicios;

public class GeneradorToken : IGeneradorToken
{
    private readonly IConfiguration _config;

    public GeneradorToken(IConfiguration config)
    {
        _config = config;
    }

    public string GenerarToken(Usuario usuario, string nombreRol)
    {
        var jwtKey = _config["Jwt:Key"] ?? throw new ArgumentNullException("Falta Jwt:Key en appsettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreUsuario),
            new Claim(ClaimTypes.Role, nombreRol),
            new Claim("sucursal_id", usuario.SucursalId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}