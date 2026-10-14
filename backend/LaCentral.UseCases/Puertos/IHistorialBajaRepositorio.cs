using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Puertos;

/// <summary>
/// D10: historial de bajas de cliente, proveedor, artículo y usuario. La fecha y el
/// usuario los fija el caso de uso; el repositorio no estampa nada por su cuenta.
/// El índice único parcial (fecha_reactivacion IS NULL) impide dos bajas abiertas.
/// </summary>
public interface IHistorialBajaRepositorio
{
    Task AbrirAsync(EntidadConHistorialBaja entidad, int entidadId, string motivo,
        DateTime fechaBaja, int usuarioBajaId, CancellationToken ct = default);

    /// <summary>
    /// Cierra la baja abierta. Devuelve false si no había ninguna: los datos importados
    /// de Multisoft tienen inactivos sin historial, y reactivarlos no debe fallar por eso.
    /// </summary>
    Task<bool> CerrarAsync(EntidadConHistorialBaja entidad, int entidadId,
        DateTime fechaReactivacion, int usuarioReactivacionId, CancellationToken ct = default);

    /// <summary>Todas las bajas de la entidad, de la más antigua a la más reciente.</summary>
    Task<IReadOnlyList<RegistroBaja>> ObtenerAsync(EntidadConHistorialBaja entidad, int entidadId,
        CancellationToken ct = default);
}
