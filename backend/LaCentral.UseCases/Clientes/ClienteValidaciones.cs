using LaCentral.UseCases.Comun;

namespace LaCentral.UseCases.Clientes;

public static class ClienteValidaciones
{
    public static Result ValidarLimites(string codigo, string razonSocial, string? cuit, string condicionFiscal, string condicionPago)
    {
        if (string.IsNullOrWhiteSpace(codigo) || codigo.Length > 20 || 
            string.IsNullOrWhiteSpace(razonSocial) || razonSocial.Length > 120 || 
            string.IsNullOrWhiteSpace(condicionFiscal) || condicionFiscal.Length > 30 || 
            string.IsNullOrWhiteSpace(condicionPago) || condicionPago.Length > 60)
        {
            return Result.Failure(TipoError.Invalido, "Faltan campos obligatorios o superan la longitud máxima permitida en la base de datos.");
        }

        if (!string.IsNullOrWhiteSpace(cuit) && cuit.Length > 13)
        {
            return Result.Failure(TipoError.Invalido, "El CUIT/CUIL no puede superar los 13 caracteres.");
        }

        return Result.Success();
    }
}