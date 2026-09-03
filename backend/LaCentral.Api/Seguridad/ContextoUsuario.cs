using System.Security.Claims;
using LaCentral.UseCases.Puertos;

namespace LaCentral.Api.Seguridad;

public class ContextoUsuario : IContextoUsuario
{
    private readonly IHttpContextAccessor _accessor;

    public ContextoUsuario(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal UsuarioActual => _accessor.HttpContext?.User 
        ?? throw new UnauthorizedAccessException("No hay contexto HTTP o el usuario no está autenticado.");

    public int UsuarioId 
    {
        get
        {
            var claim = UsuarioActual.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }

    public string NombreUsuario => UsuarioActual.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public short SucursalId 
    {
        get
        {
            var claim = UsuarioActual.FindFirstValue("sucursal");
            return short.TryParse(claim, out var sucursalId) ? sucursalId : (short)0;
        }
    }
}