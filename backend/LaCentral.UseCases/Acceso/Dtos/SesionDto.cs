namespace LaCentral.UseCases.Acceso.Dtos;

public record SesionDto(
    string Token,
    string NombreUsuario,
    string Rol,
    short SucursalId);