namespace LaCentral.UseCases.Puertos
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
        public static Result<T> Failure(string error) => new Result<T>(false, error, default);
    }
}