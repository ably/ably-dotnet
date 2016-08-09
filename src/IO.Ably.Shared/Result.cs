using System;

namespace IO.Ably
{
    //https://github.com/vkhorikov/FuntionalPrinciplesCsharp/blob/master/New/CustomerManagement.Logic/Common/Result.cs
    // Slightly modified version of the above
    public class Result
    {
        public bool IsSuccess { get; }
        public ErrorInfo Error { get; }
        public bool IsFailure => !IsSuccess;

        protected Result(bool isSuccess, ErrorInfo error)
        {
            if (isSuccess && error != null)
                throw new InvalidOperationException();
            if (!isSuccess && error == null)
                throw new InvalidOperationException();

            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Fail(string message)
        {
            return new Result(false, new ErrorInfo() {Message = message});
        }

        public static Result Fail(ErrorInfo error)
        {
            return new Result(false, error);
        }

        public static Result<T> Fail<T>(ErrorInfo error)
        {
            return new Result<T>(default(T), false, error);
        }

        public static Result Ok()
        {
            return new Result(true, null);
        }

        public static Result<T> Ok<T>(T value)
        {
            return new Result<T>(value, true, null);
        }

        public static Result Combine(params Result[] results)
        {
            foreach (Result result in results)
            {
                if (result.IsFailure)
                    return result;
            }

            return Ok();
        }
    }


    public class Result<T> : Result
    {
        private readonly T _value;

        public T Value
        {
            get
            {
                if (!IsSuccess)
                    throw new InvalidOperationException();

                return _value;
            }
        }

        protected internal Result(T value, bool isSuccess, ErrorInfo error)
            : base(isSuccess, error)
        {
            _value = value;
        }
    }

    public static class ResultExtensions
    {
        public static Result<K> OnSuccess<T, K>(this Result<T> result, Func<T, K> func)
        {
            if (result.IsFailure)
                return Result.Fail<K>(result.Error);

            return Result.Ok(func(result.Value));
        }

        public static Result OnSuccess<T>(this Result<T> result, Func<T, Result> func)
        {
            if (result.IsFailure)
                return result;

            return func(result.Value);
        }

        public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, ErrorInfo error)
        {
            if (result.IsFailure)
                return result;

            if (!predicate(result.Value))
                return Result.Fail<T>(error);

            return result;
        }

        public static Result<K> Map<T, K>(this Result<T> result, Func<T, K> func)
        {
            if (result.IsFailure)
                return Result.Fail<K>(result.Error);

            return Result.Ok(func(result.Value));
        }

        public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
        {
            if (result.IsSuccess)
            {
                action(result.Value);
            }

            return result;
        }

        public static T OnBoth<T>(this Result result, Func<Result, T> func)
        {
            return func(result);
        }

        public static Result OnSuccess(this Result result, Action action)
        {
            if (result.IsSuccess)
            {
                action();
            }

            return result;
        }
    }
}
