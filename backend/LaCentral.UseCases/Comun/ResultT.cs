namespace LaCentral.UseCases.Comun
{
    public class Result<T> : Result
    {
        public T Value { get; }
        
        private Result(bool isSuccess, string error, T value) 
            : base(isSuccess, error)
        {
            Value = value;
        }
        
        public static Result<T> Success(T value) => new Result<T>(true, string.Empty, value);
        
        // Se agrega 'new' y 'default!' para limpiar los warnings
        public static new Result<T> Failure(string error) => new Result<T>(false, error, default!);
    }
}