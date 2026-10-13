namespace LaCentral.UseCases.Comun
{
    public class Result<T> : Result
    {
        public T Value { get; }

        private Result(bool isSuccess, string error, T value, TipoError tipo = TipoError.Ninguno)
            : base(isSuccess, error, tipo)
        {
            Value = value;
        }

        public static Result<T> Success(T value) =>
            new Result<T>(true, string.Empty, value, TipoError.Ninguno);

        // 'new' oculta el estático homónimo del padre: mantiene la corrección del #65.
        /// <summary>Sobrecarga heredada: clasifica como Invalido (400 en la API).</summary>
        public static new Result<T> Failure(string error) =>
            new Result<T>(false, error, default!, TipoError.Invalido);

        /// <summary>Forma preferida: declara explícitamente qué tipo de fallo fue.</summary>
        public static new Result<T> Failure(TipoError tipo, string error) =>
            new Result<T>(false, error, default!, tipo);
    }
}
