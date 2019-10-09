using System;

namespace IO.Ably
{
    // https://github.com/vkhorikov/FuntionalPrinciplesCsharp/blob/master/New/CustomerManagement.Logic/Common/Result.cs
    // Slightly modified version of the above
    public class Result
    {
        public bool IsSuccess { get; }

        public ErrorInfo Error { get; }

        public bool IsFailure => !IsSuccess;

        protected Result(bool isSuccess, ErrorInfo error)
        {
            if (isSuccess && error != null)
            {
                throw new InvalidOperationException();
            }

            if (!isSuccess && error == null)
            {
                throw new InvalidOperationException();
            }

            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Fail(string message)
        {
            return new Result(false, new ErrorInfo() { Message = message });
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
                {
                    return result;
                }
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
                {
                    throw new InvalidOperationException();
                }

                return _value;
            }
        }

        protected internal Result(T value, bool isSuccess, ErrorInfo error)
            : base(isSuccess, error)
        {
            _value = value;
        }
    }
}
