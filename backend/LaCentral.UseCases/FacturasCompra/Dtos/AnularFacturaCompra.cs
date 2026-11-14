namespace LaCentral.UseCases.FacturasCompra.Dtos;

/// <summary>
/// HU-FCO-04 (028). Quién anula sale de la sesión. La doble certificación (B.4 del 07,
/// decisión del 14/11/2026) pide que un ADMINISTRADOR distinto la autorice con su usuario
/// y su contraseña: su identidad se prueba con la clave, nunca con un Id del cuerpo.
/// </summary>
public sealed record AnularFacturaCompraRequest(string? Motivo, string? UsuarioAutoriza, string? ContrasenaAutoriza);

/// <summary>Lo que el caso de uso necesita saber de la factura antes de anularla.</summary>
public sealed record FacturaCompraParaAnular(int Id, string Numero, bool Activa, IReadOnlyList<int> ArticuloIds);

/// <summary>La anulación ya validada, como la escribe el repositorio.</summary>
public sealed record AnulacionFacturaCompra(int FacturaId, string Motivo, int UsuarioId, int UsuarioAutorizacionId, DateTime Fecha);
