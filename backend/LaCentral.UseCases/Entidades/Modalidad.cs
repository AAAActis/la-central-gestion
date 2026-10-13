namespace LaCentral.UseCases.Entidades;

/// <summary>
/// D1: modalidades de venta CON NOMBRE. Reemplazan a las listas numeradas 1/2/3
/// del sistema anterior; no se identifican por número.
/// </summary>
public enum Modalidad
{
    Lista,
    Tarjeta3,
    Tarjeta6,
    EfectivoTransferencia,
    Costo,
    CasoEspecial
}
