namespace LaCentral.UseCases.Comun
{
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string Error { get; }

        /// <summary>Qué clase de fallo fue. En un Result exitoso es Ninguno.</summary>
        public TipoError Tipo { get; }

        // El parámetro tipo es opcional para no romper las llamadas existentes:
        // Result<T> podía seguir invocando base(isSuccess, error) sin cambios.
        protected Result(bool isSuccess, string error, TipoError tipo = TipoError.Ninguno)
        {
            if (isSuccess && error != string.Empty)
                throw new InvalidOperationException();
            if (!isSuccess && error == string.Empty)
                throw new InvalidOperationException();

            IsSuccess = isSuccess;
            Error = error;
            Tipo = tipo;
        }

        public static Result Success() => new Result(true, string.Empty, TipoError.Ninguno);

        /// <summary>
        /// Sobrecarga heredada. Se mantiene para que los casos de uso ya escritos
        /// sigan compilando; clasifica el fallo como Invalido, que en la API es 400.
        /// </summary>
        public static Result Failure(string error) => new Result(false, error, TipoError.Invalido);

        /// <summary>Forma preferida: declara explícitamente qué tipo de fallo fue.</summary>
        public static Result Failure(TipoError tipo, string error) => new Result(false, error, tipo);
    }
}
