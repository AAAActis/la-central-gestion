namespace LaCentral.UseCases.FacturasCompra.Dtos;

/// <summary>
/// HU-FCO-04 (028). Quién anula sale de la sesión y tiene que ser ADMINISTRADOR (CA-006 y CA-007
/// v1.2, D-S7-04, 23/11/2026: sin doble certificación).
/// </summary>
public sealed record AnularFacturaCompraRequest(string? Motivo);

/// <summary>Lo que el caso de uso necesita saber de la factura antes de anularla.</summary>
public sealed record FacturaCompraParaAnular(int Id, string Numero, bool Activa, IReadOnlyList<int> ArticuloIds);

/// <summary>La anulación ya validada, como la escribe el repositorio.</summary>
public sealed record AnulacionFacturaCompra(int FacturaId, string Motivo, int UsuarioId, DateTime Fecha);
