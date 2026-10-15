namespace LaCentral.UseCases.Entidades;

/// <summary>
/// D1: una fila de articulo_precio_modalidad, tal como la guarda la tabla. Precio null es
/// "no se pudo calcular", nunca cero. Base y Porcentaje van juntos: los dos o ninguno.
/// </summary>
public sealed record PrecioModalidad
{
    public Modalidad Modalidad { get; set; }
    public BasePrecio? Base { get; set; }
    public decimal? Porcentaje { get; set; }
    public decimal? Precio { get; set; }

    /// <summary>true = lo fija una persona; el recálculo automático no lo pisa.</summary>
    public bool CalculoManual { get; set; }

    public DateTime? FechaCalculo { get; set; }
    public int? UsuarioId { get; set; }
}
